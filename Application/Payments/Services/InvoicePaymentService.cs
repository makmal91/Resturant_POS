using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Application.Payments.DTOs;
using POSSystem.Application.Payments.Interfaces;
using POSSystem.Domain;
using PurchaseEntity = POSSystem.Domain.Purchase;

namespace POSSystem.Application.Payments.Services;

public class PaymentService : IInvoicePaymentService
{
    private readonly IInvoicePaymentRepository _repository;
    private readonly IAccountingIntegrationService _accountingIntegration;
    private readonly IAccountingRepository _accountingRepository;
    private readonly ICodeGeneratorService _codeGenerator;

    public PaymentService(
        IInvoicePaymentRepository repository,
        IAccountingIntegrationService accountingIntegration,
        IAccountingRepository accountingRepository,
        ICodeGeneratorService codeGenerator)
    {
        _repository = repository;
        _accountingIntegration = accountingIntegration;
        _accountingRepository = accountingRepository;
        _codeGenerator = codeGenerator;
    }

    public async Task<InvoicePaymentDto> RecordCustomerPaymentAsync(RecordCustomerPaymentDto dto)
    {
        ValidateAmount(dto.Amount);

        _ = await _repository.GetCustomerAsync(dto.CustomerId, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Customer not found.");

        var outstanding = await _repository.GetOutstandingSaleInvoicesAsync(
            dto.CustomerId, dto.BusinessId, dto.BranchId);

        var plan = ResolveAllocationPlan(
            dto.Amount,
            dto.SaleInvoiceId,
            dto.AutoAllocate,
            dto.Allocations,
            outstanding);

        ValidateTotalApplied(plan, dto.Amount);
        await ValidateSaleAllocationsAsync(plan, dto.CustomerId, dto.BusinessId, dto.BranchId);

        var primaryInvoice = plan.Count == 1
            ? await _repository.GetSaleInvoiceAsync(plan[0].InvoiceId, dto.BusinessId, dto.BranchId)
            : null;

        var paymentDate = NormalizeLedgerPaymentDate(dto.PaymentDate, primaryInvoice?.SaleDate);
        var useLegacyDirectLink = plan.Count == 1 && dto.SaleInvoiceId.HasValue && (dto.Allocations == null || dto.Allocations.Count == 0);

        var payment = new InvoicePayment
        {
            Module = InvoicePaymentModule.Sale,
            SaleInvoiceId = useLegacyDirectLink ? plan[0].InvoiceId : null,
            CustomerId = dto.CustomerId,
            PaymentType = dto.PaymentType,
            Amount = dto.Amount,
            PaymentDate = paymentDate,
            Notes = dto.Notes?.Trim() ?? string.Empty,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId,
            CreatedBy = dto.CreatedBy
        };

        payment.ReferenceNo = await _codeGenerator.GenerateAsync(
            CodeModuleNames.CustomerReceipt, dto.BranchId);

        await _repository.AddAsync(payment);
        await _repository.SaveChangesAsync();

        if (!useLegacyDirectLink)
        {
            foreach (var item in plan)
            {
                await _repository.AddAllocationAsync(new PaymentAllocation
                {
                    InvoicePaymentId = payment.Id,
                    SaleInvoiceId = item.InvoiceId,
                    AppliedAmount = item.AppliedAmount,
                    BusinessId = dto.BusinessId,
                    BranchId = dto.BranchId,
                    CreatedBy = dto.CreatedBy
                });
            }

            await _repository.SaveChangesAsync();
        }

        await EnrichAllocationPlanAsync(plan, isPurchase: false, dto.BusinessId, dto.BranchId);

        await _accountingIntegration.PostPaymentReceivedAsync(payment);

        foreach (var item in plan)
            await SyncSaleInvoicePaidCacheAsync(item.InvoiceId, dto.BusinessId, dto.BranchId);

        if (useLegacyDirectLink && primaryInvoice != null)
            payment.SaleInvoice = primaryInvoice;

        payment.Customer = await _repository.GetCustomerAsync(dto.CustomerId, dto.BusinessId, dto.BranchId);
        payment.Allocations = plan.Select(p => new PaymentAllocation
        {
            SaleInvoiceId = p.InvoiceId,
            AppliedAmount = p.AppliedAmount
        }).ToList();

        return MapPayment(payment);
    }

    public async Task<InvoicePaymentDto> RecordSupplierPaymentAsync(RecordSupplierPaymentDto dto)
    {
        ValidateAmount(dto.Amount);

        _ = await _repository.GetSupplierAsync(dto.SupplierId, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Supplier not found.");

        var category = dto.Category;
        var autoAllocate = category == InvoicePaymentCategory.Advance ? false : dto.AutoAllocate;
        var manualAllocations = category == InvoicePaymentCategory.Advance ? null : dto.Allocations;
        var legacyPurchaseId = category == InvoicePaymentCategory.Advance ? null : dto.PurchaseId;

        var outstanding = category == InvoicePaymentCategory.Advance
            ? []
            : await _repository.GetOutstandingPurchaseInvoicesAsync(
                dto.SupplierId, dto.BusinessId, dto.BranchId);

        var plan = category == InvoicePaymentCategory.Advance
            ? []
            : ResolveAllocationPlan(
                dto.Amount,
                legacyPurchaseId,
                autoAllocate,
                manualAllocations,
                outstanding);

        ValidateTotalApplied(plan, dto.Amount);
        if (category == InvoicePaymentCategory.Adjustment)
            await ValidatePurchaseAdjustmentAllocationsAsync(plan, dto.SupplierId, dto.BusinessId, dto.BranchId);
        else if (category != InvoicePaymentCategory.Advance)
            await ValidatePurchaseAllocationsAsync(plan, dto.SupplierId, dto.BusinessId, dto.BranchId);

        var primaryPurchase = plan.Count == 1
            ? await _repository.GetPurchaseAsync(plan[0].InvoiceId, dto.BusinessId, dto.BranchId)
            : null;

        var paymentDate = NormalizeLedgerPaymentDate(dto.PaymentDate, primaryPurchase?.PurchaseDate);
        var useLegacyDirectLink = plan.Count == 1 && legacyPurchaseId.HasValue && (manualAllocations == null || manualAllocations.Count == 0);

        var payment = new InvoicePayment
        {
            Module = InvoicePaymentModule.Purchase,
            PurchaseId = useLegacyDirectLink ? plan[0].InvoiceId : null,
            SupplierId = dto.SupplierId,
            PaymentType = dto.PaymentType,
            Category = category,
            Amount = dto.Amount,
            PaymentDate = paymentDate,
            Notes = dto.Notes?.Trim() ?? string.Empty,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId,
            CreatedBy = dto.CreatedBy
        };

        payment.ReferenceNo = await _codeGenerator.GenerateAsync(
            CodeModuleNames.SupplierPayment, dto.BranchId);

        await _repository.AddAsync(payment);
        await _repository.SaveChangesAsync();

        if (!useLegacyDirectLink)
        {
            foreach (var item in plan)
            {
                await _repository.AddAllocationAsync(new PaymentAllocation
                {
                    InvoicePaymentId = payment.Id,
                    PurchaseId = item.InvoiceId,
                    AppliedAmount = item.AppliedAmount,
                    BusinessId = dto.BusinessId,
                    BranchId = dto.BranchId,
                    CreatedBy = dto.CreatedBy
                });
            }

            await _repository.SaveChangesAsync();
        }

        await EnrichAllocationPlanAsync(plan, isPurchase: true, dto.BusinessId, dto.BranchId);

        await _accountingIntegration.PostPaymentPaidAsync(payment);

        foreach (var item in plan)
            await SyncPurchasePaidCacheAsync(item.InvoiceId, dto.BusinessId, dto.BranchId);

        if (useLegacyDirectLink && primaryPurchase != null)
            payment.Purchase = primaryPurchase;

        payment.Supplier = await _repository.GetSupplierAsync(dto.SupplierId, dto.BusinessId, dto.BranchId);
        payment.Allocations = plan.Select(p => new PaymentAllocation
        {
            PurchaseId = p.InvoiceId,
            AppliedAmount = p.AppliedAmount
        }).ToList();

        return MapPayment(payment);
    }

    public async Task<InvoicePaymentDto> ReversePaymentAsync(ReversePaymentDto dto)
    {
        var original = await _repository.GetByIdAsync(dto.PaymentId, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Payment not found.");

        if (original.IsReversed)
            throw new InvalidOperationException("This payment has already been reversed.");

        if (await HasExistingReversalAsync(original.Id, dto.BusinessId, dto.BranchId))
            throw new InvalidOperationException("A reversal entry already exists for this payment.");

        var glType = original.Module == InvoicePaymentModule.Sale
            ? GlTransactionType.Receipt
            : GlTransactionType.Payment;
        await _accountingIntegration.ReverseTransactionAsync(
            original.Id, glType, $"Reversal — payment {original.ReferenceNo}".Trim());

        var allocations = await _repository.GetAllocationsByPaymentIdAsync(
            original.Id, dto.BusinessId, dto.BranchId);

        var reversal = new InvoicePayment
        {
            Module = original.Module,
            CustomerId = original.CustomerId,
            SupplierId = original.SupplierId,
            PaymentType = original.PaymentType,
            Amount = original.Amount,
            PaymentDate = DateTime.UtcNow,
            ReferenceNo = original.ReferenceNo,
            Notes = BuildReversalNotes(original, dto.Reason),
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId,
            CreatedBy = dto.ReversedBy,
            IsReversed = true,
            OriginalPaymentId = original.Id,
            ReversedBy = dto.ReversedBy,
            ReversedAt = DateTime.UtcNow,
            Category = original.Category
        };

        await _repository.AddAsync(reversal);
        await _repository.SaveChangesAsync();

        await _repository.SoftDeleteAllocationsAsync(allocations, dto.ReversedBy);
        await _repository.SoftDeletePaymentAsync(original, dto.ReversedBy);
        await _repository.SaveChangesAsync();

        var affectedInvoiceIds = CollectAffectedInvoiceIds(original, allocations);
        if (original.Module == InvoicePaymentModule.Sale)
        {
            foreach (var invoiceId in affectedInvoiceIds)
                await SyncSaleInvoicePaidCacheAsync(invoiceId, dto.BusinessId, dto.BranchId);
        }
        else
        {
            foreach (var invoiceId in affectedInvoiceIds)
                await SyncPurchasePaidCacheAsync(invoiceId, dto.BusinessId, dto.BranchId);
        }

        reversal.Customer = original.Customer;
        reversal.Supplier = original.Supplier;
        return MapPayment(reversal);
    }

    public async Task<InvoicePaymentDto> UpdatePaymentAsync(int paymentId, UpdatePaymentDto dto)
    {
        ValidateAmount(dto.Amount);

        var payment = await _repository.GetByIdAsync(paymentId, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Payment not found.");

        EnsurePaymentEditable(payment);

        var oldAllocations = await _repository.GetAllocationsByPaymentIdAsync(
            paymentId, dto.BusinessId, dto.BranchId);
        var affectedInvoiceIds = CollectAffectedInvoiceIds(payment, oldAllocations);

        var glType = payment.Module == InvoicePaymentModule.Sale
            ? GlTransactionType.Receipt
            : GlTransactionType.Payment;

        await _accountingRepository.RunInTransactionAsync(async () =>
        {
            await _accountingIntegration.ReverseTransactionAsync(
                paymentId, glType, $"Edit — payment {payment.ReferenceNo}".Trim());

            payment.SaleInvoiceId = null;
            payment.PurchaseId = null;
            await _repository.SoftDeleteAllocationsAsync(oldAllocations, dto.ModifiedBy);
            await _repository.SaveChangesAsync();

            List<AllocationPlanItem> plan;
            if (payment.Module == InvoicePaymentModule.Sale)
                plan = await BuildCustomerUpdatePlanAsync(payment, dto);
            else
                plan = await BuildSupplierUpdatePlanAsync(payment, dto);

            DateTime? relatedInvoiceDate = null;
            if (plan.Count == 1)
            {
                if (payment.Module == InvoicePaymentModule.Sale)
                    relatedInvoiceDate = (await _repository.GetSaleInvoiceAsync(plan[0].InvoiceId, dto.BusinessId, dto.BranchId))?.SaleDate;
                else
                    relatedInvoiceDate = (await _repository.GetPurchaseAsync(plan[0].InvoiceId, dto.BusinessId, dto.BranchId))?.PurchaseDate;
            }

            var legacyInvoiceId = payment.Module == InvoicePaymentModule.Sale ? dto.SaleInvoiceId : dto.PurchaseId;
            var useLegacyDirectLink = plan.Count == 1
                && legacyInvoiceId.HasValue
                && (dto.Allocations == null || dto.Allocations.Count == 0);

            var paymentDate = NormalizeLedgerPaymentDate(dto.PaymentDate, relatedInvoiceDate);

            payment.PaymentType = dto.PaymentType;
            payment.Amount = dto.Amount;
            payment.PaymentDate = paymentDate;
            payment.Notes = dto.Notes?.Trim() ?? string.Empty;
            payment.ModifiedAt = DateTime.UtcNow;
            payment.ModifiedBy = dto.ModifiedBy;

            if (payment.Module == InvoicePaymentModule.Purchase && dto.Category.HasValue)
                payment.Category = dto.Category.Value;

            if (useLegacyDirectLink)
            {
                if (payment.Module == InvoicePaymentModule.Sale)
                    payment.SaleInvoiceId = plan[0].InvoiceId;
                else
                    payment.PurchaseId = plan[0].InvoiceId;
            }

            if (!useLegacyDirectLink)
            {
                foreach (var item in plan)
                {
                    await _repository.AddAllocationAsync(new PaymentAllocation
                    {
                        InvoicePaymentId = payment.Id,
                        SaleInvoiceId = payment.Module == InvoicePaymentModule.Sale ? item.InvoiceId : null,
                        PurchaseId = payment.Module == InvoicePaymentModule.Purchase ? item.InvoiceId : null,
                        AppliedAmount = item.AppliedAmount,
                        BusinessId = dto.BusinessId,
                        BranchId = dto.BranchId,
                        CreatedBy = dto.ModifiedBy
                    });
                }
            }

            await _repository.SaveChangesAsync();

            if (payment.Module == InvoicePaymentModule.Sale)
                await _accountingIntegration.PostPaymentReceivedAsync(payment);
            else
                await _accountingIntegration.PostPaymentPaidAsync(payment);

            foreach (var item in plan)
                affectedInvoiceIds.Add(item.InvoiceId);
        });

        if (payment.Module == InvoicePaymentModule.Sale)
        {
            foreach (var invoiceId in affectedInvoiceIds)
                await SyncSaleInvoicePaidCacheAsync(invoiceId, dto.BusinessId, dto.BranchId);
        }
        else
        {
            foreach (var invoiceId in affectedInvoiceIds)
                await SyncPurchasePaidCacheAsync(invoiceId, dto.BusinessId, dto.BranchId);
        }

        var updated = await _repository.GetByIdAsync(paymentId, dto.BusinessId, dto.BranchId);
        return MapPayment(updated!);
    }

    private async Task<List<AllocationPlanItem>> BuildCustomerUpdatePlanAsync(
        InvoicePayment payment, UpdatePaymentDto dto)
    {
        if (!payment.CustomerId.HasValue)
            throw new InvalidOperationException("Customer payment requires CustomerId.");

        var outstanding = await _repository.GetOutstandingSaleInvoicesAsync(
            payment.CustomerId.Value, dto.BusinessId, dto.BranchId);

        var plan = ResolveAllocationPlan(
            dto.Amount,
            dto.SaleInvoiceId,
            dto.AutoAllocate,
            dto.Allocations,
            outstanding);

        ValidateTotalApplied(plan, dto.Amount);
        await ValidateSaleAllocationsAsync(plan, payment.CustomerId.Value, dto.BusinessId, dto.BranchId);
        await EnrichAllocationPlanAsync(plan, isPurchase: false, dto.BusinessId, dto.BranchId);
        return plan;
    }

    private async Task<List<AllocationPlanItem>> BuildSupplierUpdatePlanAsync(
        InvoicePayment payment, UpdatePaymentDto dto)
    {
        if (!payment.SupplierId.HasValue)
            throw new InvalidOperationException("Supplier payment requires SupplierId.");

        var category = dto.Category ?? payment.Category;
        var autoAllocate = category == InvoicePaymentCategory.Advance ? false : dto.AutoAllocate;
        var manualAllocations = category == InvoicePaymentCategory.Advance ? null : dto.Allocations;
        var legacyPurchaseId = category == InvoicePaymentCategory.Advance ? null : dto.PurchaseId;

        var outstanding = category == InvoicePaymentCategory.Advance
            ? []
            : await _repository.GetOutstandingPurchaseInvoicesAsync(
                payment.SupplierId.Value, dto.BusinessId, dto.BranchId);

        var plan = category == InvoicePaymentCategory.Advance
            ? []
            : ResolveAllocationPlan(
                dto.Amount,
                legacyPurchaseId,
                autoAllocate,
                manualAllocations,
                outstanding);

        ValidateTotalApplied(plan, dto.Amount);
        if (category == InvoicePaymentCategory.Adjustment)
            await ValidatePurchaseAdjustmentAllocationsAsync(plan, payment.SupplierId.Value, dto.BusinessId, dto.BranchId);
        else if (category != InvoicePaymentCategory.Advance)
            await ValidatePurchaseAllocationsAsync(plan, payment.SupplierId.Value, dto.BusinessId, dto.BranchId);

        await EnrichAllocationPlanAsync(plan, isPurchase: true, dto.BusinessId, dto.BranchId);
        return plan;
    }

    private static void EnsurePaymentEditable(InvoicePayment payment)
    {
        if (payment.IsDeleted)
            throw new InvalidOperationException("Payment not found.");

        if (payment.IsReversed)
            throw new InvalidOperationException("Reversed payments cannot be edited.");

        if (payment.OriginalPaymentId.HasValue)
            throw new InvalidOperationException("Reversal entries cannot be edited.");
    }

    public async Task RecordPosSalePaymentsAsync(SaleInvoice invoice, int? createdBy = null)
    {
        if (invoice.IsCreditSale || invoice.GrandTotal <= 0)
            return;

        var (cash, card) = ResolvePosPaymentAmounts(invoice);
        var paymentDate = invoice.SaleDate;
        var payments = new List<InvoicePayment>();

        if (cash > 0)
        {
            payments.Add(new InvoicePayment
            {
                Module = InvoicePaymentModule.Sale,
                SaleInvoiceId = invoice.Id,
                CustomerId = invoice.CustomerId,
                PaymentType = PartyPaymentType.Cash,
                Category = InvoicePaymentCategory.PosSale,
                Amount = cash,
                PaymentDate = paymentDate,
                Notes = $"POS Sale — Invoice: {invoice.InvoiceNo}",
                BusinessId = invoice.BusinessId,
                BranchId = invoice.BranchId,
                CreatedBy = createdBy
            });
        }

        if (card > 0)
        {
            payments.Add(new InvoicePayment
            {
                Module = InvoicePaymentModule.Sale,
                SaleInvoiceId = invoice.Id,
                CustomerId = invoice.CustomerId,
                PaymentType = PartyPaymentType.Bank,
                Category = InvoicePaymentCategory.PosSale,
                Amount = card,
                PaymentDate = paymentDate,
                Notes = $"POS Card — Invoice: {invoice.InvoiceNo}",
                BusinessId = invoice.BusinessId,
                BranchId = invoice.BranchId,
                CreatedBy = createdBy
            });
        }

        if (payments.Count == 0)
        {
            payments.Add(new InvoicePayment
            {
                Module = InvoicePaymentModule.Sale,
                SaleInvoiceId = invoice.Id,
                CustomerId = invoice.CustomerId,
                PaymentType = MapPosPaymentType(invoice.PaymentMethod),
                Category = InvoicePaymentCategory.PosSale,
                Amount = invoice.GrandTotal,
                PaymentDate = paymentDate,
                Notes = $"POS Sale — Invoice: {invoice.InvoiceNo}",
                BusinessId = invoice.BusinessId,
                BranchId = invoice.BranchId,
                CreatedBy = createdBy
            });
        }

        foreach (var payment in payments)
            await _repository.AddAsync(payment);

        await _repository.SaveChangesAsync();
        await SyncSaleInvoicePaidCacheAsync(invoice.Id, invoice.BusinessId, invoice.BranchId);
    }

    public Task<decimal> GetTotalPaidForSaleInvoiceAsync(int saleInvoiceId, int businessId, int branchId)
        => _repository.GetTotalPaidForSaleInvoiceAsync(saleInvoiceId, businessId, branchId);

    public Task<decimal> GetTotalPaidForPurchaseAsync(int purchaseId, int businessId, int branchId)
        => _repository.GetTotalPaidForPurchaseAsync(purchaseId, businessId, branchId);

    public Task<Dictionary<int, decimal>> GetPaidTotalsForSaleInvoicesAsync(
        IEnumerable<int> saleInvoiceIds, int businessId, int branchId)
        => _repository.GetPaidTotalsForSaleInvoicesAsync(saleInvoiceIds, businessId, branchId);

    public Task<Dictionary<int, decimal>> GetPaidTotalsForPurchasesAsync(
        IEnumerable<int> purchaseIds, int businessId, int branchId)
        => _repository.GetPaidTotalsForPurchasesAsync(purchaseIds, businessId, branchId);

    public async Task<List<InvoicePaymentDto>> GetPaymentsForSaleInvoiceAsync(
        int saleInvoiceId, int businessId, int branchId)
    {
        var payments = await _repository.GetBySaleInvoiceIdAsync(saleInvoiceId, businessId, branchId);
        return payments.Select(MapPayment).ToList();
    }

    public async Task<List<InvoicePaymentDto>> GetPaymentsForPurchaseAsync(
        int purchaseId, int businessId, int branchId)
    {
        var payments = await _repository.GetByPurchaseIdAsync(purchaseId, businessId, branchId);
        return payments.Select(MapPayment).ToList();
    }

    public async Task<InvoicePaymentDto?> GetPaymentByIdAsync(int paymentId, int businessId, int branchId)
    {
        var payment = await _repository.GetByIdAsync(paymentId, businessId, branchId);
        return payment == null ? null : MapPayment(payment);
    }

    public async Task<InvoiceBalanceDto> GetSaleInvoiceBalanceAsync(int saleInvoiceId, int businessId, int branchId)
    {
        var invoice = await _repository.GetSaleInvoiceAsync(saleInvoiceId, businessId, branchId)
            ?? throw new InvalidOperationException("Sale invoice not found.");

        var paid = await _repository.GetTotalPaidForSaleInvoiceAsync(saleInvoiceId, businessId, branchId);
        return new InvoiceBalanceDto
        {
            InvoiceId = invoice.Id,
            InvoiceNo = invoice.InvoiceNo,
            InvoiceTotal = invoice.GrandTotal,
            PaidAmount = paid,
            BalanceDue = invoice.GrandTotal - paid,
            SettlementStatus = ResolveSettlementStatus(paid, invoice.GrandTotal).ToString()
        };
    }

    public async Task<InvoiceBalanceDto> GetPurchaseBalanceAsync(int purchaseId, int businessId, int branchId)
    {
        var purchase = await _repository.GetPurchaseAsync(purchaseId, businessId, branchId)
            ?? throw new InvalidOperationException("Purchase invoice not found.");

        var paid = await _repository.GetTotalPaidForPurchaseAsync(purchaseId, businessId, branchId);
        return new InvoiceBalanceDto
        {
            InvoiceId = purchase.Id,
            InvoiceNo = purchase.InvoiceNo,
            InvoiceTotal = purchase.TotalAmount,
            PaidAmount = paid,
            BalanceDue = purchase.TotalAmount - paid,
            SettlementStatus = ResolveSettlementStatus(paid, purchase.TotalAmount).ToString()
        };
    }

    public Task<List<OutstandingInvoiceOptionDto>> GetOutstandingSaleInvoicesAsync(
        int customerId, int businessId, int branchId, int? excludePaymentId = null)
        => _repository.GetOutstandingSaleInvoicesAsync(customerId, businessId, branchId, excludePaymentId);

    public Task<List<OutstandingInvoiceOptionDto>> GetOutstandingPurchaseInvoicesAsync(
        int supplierId, int businessId, int branchId, int? excludePaymentId = null)
        => _repository.GetOutstandingPurchaseInvoicesAsync(supplierId, businessId, branchId, excludePaymentId);

    public async Task<PagedResultDto<InvoicePaymentDto>> ListPaymentsAsync(PaymentListFilterDto filter)
    {
        var payments = await _repository.GetFilteredAsync(new InvoicePaymentFilterDto
        {
            BusinessId = filter.BusinessId,
            BranchId = filter.BranchId,
            Module = filter.Module,
            CustomerId = filter.CustomerId,
            SupplierId = filter.SupplierId,
        });

        IEnumerable<InvoicePayment> query = payments.Where(p => !p.IsDeleted);
        if (!filter.IncludeReversed)
            query = query.Where(p => !p.IsReversed);

        if (filter.FromDate.HasValue)
            query = query.Where(p => p.PaymentDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(p => p.PaymentDate <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));

        var ordered = query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .ToList();

        var page = filter.Page > 0 ? filter.Page : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 25;
        var totalRecords = ordered.Count;
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);
        var offset = (page - 1) * pageSize;

        var pageData = ordered
            .Skip(offset)
            .Take(pageSize)
            .Select(MapPayment)
            .ToList();

        return new PagedResultDto<InvoicePaymentDto>
        {
            Data = pageData,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page,
        };
    }

    private async Task SyncSaleInvoicePaidCacheAsync(int saleInvoiceId, int businessId, int branchId)
    {
        var invoice = await _repository.GetSaleInvoiceAsync(saleInvoiceId, businessId, branchId);
        if (invoice == null) return;

        var total = await _repository.GetTotalPaidForSaleInvoiceAsync(saleInvoiceId, businessId, branchId);
        var status = ResolveSettlementStatus(total, invoice.GrandTotal);
        await _repository.SyncSaleInvoicePaidCacheAsync(saleInvoiceId, businessId, branchId, total, status);
    }

    private async Task SyncPurchasePaidCacheAsync(int purchaseId, int businessId, int branchId)
    {
        var purchase = await _repository.GetPurchaseAsync(purchaseId, businessId, branchId);
        if (purchase == null) return;

        var total = await _repository.GetTotalPaidForPurchaseAsync(purchaseId, businessId, branchId);
        var status = ResolveSettlementStatus(total, purchase.TotalAmount);
        await _repository.SyncPurchasePaidCacheAsync(purchaseId, businessId, branchId, total, status);
    }

    private static List<AllocationPlanItem> ResolveAllocationPlan(
        decimal paymentAmount,
        int? legacyInvoiceId,
        bool autoAllocate,
        List<PaymentAllocationItemDto>? manualAllocations,
        List<OutstandingInvoiceOptionDto> outstanding)
    {
        if (manualAllocations is { Count: > 0 })
        {
            return manualAllocations
                .Where(a => a.AppliedAmount > 0)
                .GroupBy(a => a.InvoiceId)
                .Select(g => new AllocationPlanItem
                {
                    InvoiceId = g.Key,
                    AppliedAmount = g.Sum(x => x.AppliedAmount)
                })
                .ToList();
        }

        if (legacyInvoiceId.HasValue)
        {
            return
            [
                new AllocationPlanItem
                {
                    InvoiceId = legacyInvoiceId.Value,
                    AppliedAmount = paymentAmount
                }
            ];
        }

        if (!autoAllocate)
            return [];

        var plan = new List<AllocationPlanItem>();
        var remaining = paymentAmount;

        foreach (var invoice in outstanding)
        {
            if (remaining <= 0.005m) break;

            var applied = Math.Min(invoice.BalanceDue, remaining);
            if (applied <= 0) continue;

            plan.Add(new AllocationPlanItem
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNo = invoice.InvoiceNo,
                AppliedAmount = applied
            });
            remaining -= applied;
        }

        return plan;
    }

    private async Task EnrichAllocationPlanAsync(
        List<AllocationPlanItem> plan, bool isPurchase, int businessId, int branchId)
    {
        foreach (var item in plan.Where(p => string.IsNullOrWhiteSpace(p.InvoiceNo)))
        {
            if (isPurchase)
            {
                var purchase = await _repository.GetPurchaseAsync(item.InvoiceId, businessId, branchId);
                item.InvoiceNo = purchase?.InvoiceNo ?? $"#{item.InvoiceId}";
            }
            else
            {
                var invoice = await _repository.GetSaleInvoiceAsync(item.InvoiceId, businessId, branchId);
                item.InvoiceNo = invoice?.InvoiceNo ?? $"#{item.InvoiceId}";
            }
        }
    }

    private static void ValidateTotalApplied(List<AllocationPlanItem> plan, decimal paymentAmount)
    {
        var totalApplied = plan.Sum(p => p.AppliedAmount);
        if (totalApplied > paymentAmount + 0.005m)
            throw new InvalidOperationException("Total applied amount cannot exceed payment amount.");
    }

    private async Task ValidateSaleAllocationsAsync(
        List<AllocationPlanItem> plan, int customerId, int businessId, int branchId)
    {
        if (plan.Count == 0) return;

        var duplicateCheck = plan.GroupBy(p => p.InvoiceId).Any(g => g.Count() > 1);
        if (duplicateCheck)
            throw new InvalidOperationException("Duplicate invoice allocations are not allowed.");

        decimal totalApplied = 0;
        foreach (var item in plan)
        {
            if (item.AppliedAmount <= 0)
                throw new InvalidOperationException("Applied amount must be greater than zero.");

            var invoice = await _repository.GetSaleInvoiceAsync(item.InvoiceId, businessId, branchId)
                ?? throw new InvalidOperationException("Sale invoice not found.");

            item.InvoiceNo ??= invoice.InvoiceNo;

            if (!invoice.CustomerId.HasValue || invoice.CustomerId.Value != customerId)
                throw new InvalidOperationException("Customer does not match the selected invoice.");

            if (invoice.Status != SaleInvoiceStatus.Completed)
                throw new InvalidOperationException("Payments can only be recorded against completed sale invoices.");

            if (!invoice.IsCreditSale)
                throw new InvalidOperationException(
                    $"Sale invoice {invoice.InvoiceNo} is not a credit sale and cannot be paid through the customer ledger.");

            var paid = await _repository.GetTotalPaidForSaleInvoiceAsync(item.InvoiceId, businessId, branchId);
            var due = invoice.GrandTotal - paid;
            if (item.AppliedAmount > due + 0.005m)
                throw new InvalidOperationException($"Applied amount exceeds invoice balance due of {due:N2} for {invoice.InvoiceNo}.");

            totalApplied += item.AppliedAmount;
        }
    }

    private async Task ValidatePurchaseAllocationsAsync(
        List<AllocationPlanItem> plan, int supplierId, int businessId, int branchId)
    {
        if (plan.Count == 0) return;

        if (plan.GroupBy(p => p.InvoiceId).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Duplicate invoice allocations are not allowed.");

        foreach (var item in plan)
        {
            if (item.AppliedAmount <= 0)
                throw new InvalidOperationException("Applied amount must be greater than zero.");

            var purchase = await _repository.GetPurchaseAsync(item.InvoiceId, businessId, branchId)
                ?? throw new InvalidOperationException("Purchase invoice not found.");

            item.InvoiceNo ??= purchase.InvoiceNo;

            if (purchase.SupplierId != supplierId)
                throw new InvalidOperationException("Supplier does not match the selected purchase invoice.");

            if (purchase.Status != PurchaseStatus.Posted)
                throw new InvalidOperationException("Payments can only be recorded against posted purchase invoices.");

            var paid = await _repository.GetTotalPaidForPurchaseAsync(item.InvoiceId, businessId, branchId);
            var due = purchase.TotalAmount - paid;
            if (item.AppliedAmount > due + 0.005m)
                throw new InvalidOperationException($"Applied amount exceeds invoice balance due of {due:N2} for {purchase.InvoiceNo}.");
        }
    }

    private async Task ValidatePurchaseAdjustmentAllocationsAsync(
        List<AllocationPlanItem> plan, int supplierId, int businessId, int branchId)
    {
        if (plan.Count == 0) return;

        if (plan.GroupBy(p => p.InvoiceId).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Duplicate invoice allocations are not allowed.");

        foreach (var item in plan)
        {
            if (item.AppliedAmount <= 0)
                throw new InvalidOperationException("Applied amount must be greater than zero.");

            var purchase = await _repository.GetPurchaseAsync(item.InvoiceId, businessId, branchId)
                ?? throw new InvalidOperationException("Purchase invoice not found.");

            item.InvoiceNo ??= purchase.InvoiceNo;

            if (purchase.SupplierId != supplierId)
                throw new InvalidOperationException("Supplier does not match the selected purchase invoice.");

            if (purchase.Status != PurchaseStatus.Posted)
                throw new InvalidOperationException("Adjustments can only be linked to posted purchase invoices.");
        }
    }

    private async Task<bool> HasExistingReversalAsync(int paymentId, int businessId, int branchId)
    {
        var payments = await _repository.GetFilteredAsync(new InvoicePaymentFilterDto
        {
            BusinessId = businessId,
            BranchId = branchId
        });

        return payments.Any(p => p.OriginalPaymentId == paymentId && !p.IsDeleted);
    }

    private static HashSet<int> CollectAffectedInvoiceIds(InvoicePayment payment, List<PaymentAllocation> allocations)
    {
        var ids = new HashSet<int>();
        if (payment.SaleInvoiceId.HasValue) ids.Add(payment.SaleInvoiceId.Value);
        if (payment.PurchaseId.HasValue) ids.Add(payment.PurchaseId.Value);

        foreach (var allocation in allocations)
        {
            if (allocation.SaleInvoiceId.HasValue) ids.Add(allocation.SaleInvoiceId.Value);
            if (allocation.PurchaseId.HasValue) ids.Add(allocation.PurchaseId.Value);
        }

        return ids;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");
    }

    private static string BuildReversalNotes(InvoicePayment original, string? reason)
    {
        var text = $"Reversal of payment #{original.Id}";
        return string.IsNullOrWhiteSpace(reason) ? text : $"{text} | {reason.Trim()}";
    }

    private static (decimal cash, decimal card) ResolvePosPaymentAmounts(SaleInvoice inv)
    {
        var cash = inv.CashAmount;
        var card = inv.CardAmount;

        if (cash <= 0 && card <= 0)
        {
            switch (inv.PaymentMethod)
            {
                case SalePaymentMethod.Cash:
                    cash = inv.GrandTotal;
                    break;
                case SalePaymentMethod.Card:
                    card = inv.GrandTotal;
                    break;
                case SalePaymentMethod.Mixed:
                    cash = inv.GrandTotal;
                    break;
            }
        }

        return (cash, card);
    }

    private static DateTime NormalizeLedgerPaymentDate(DateTime? paymentDate, DateTime? relatedInvoiceDate)
    {
        if (!paymentDate.HasValue)
            return DateTime.UtcNow;

        var normalized = paymentDate.Value.TimeOfDay == TimeSpan.Zero
            ? paymentDate.Value.Date.AddDays(1).AddTicks(-1)
            : paymentDate.Value;

        if (relatedInvoiceDate.HasValue && normalized < relatedInvoiceDate.Value)
            normalized = relatedInvoiceDate.Value;

        return normalized;
    }

    private static PartyPaymentType MapPosPaymentType(SalePaymentMethod method) => method switch
    {
        SalePaymentMethod.Card => PartyPaymentType.Bank,
        SalePaymentMethod.Mixed => PartyPaymentType.Cash,
        _ => PartyPaymentType.Cash
    };

    private static InvoiceSettlementStatus ResolveSettlementStatus(decimal paid, decimal total)
    {
        if (paid <= 0.005m) return InvoiceSettlementStatus.Pending;
        if (paid >= total - 0.005m) return InvoiceSettlementStatus.Paid;
        return InvoiceSettlementStatus.Partial;
    }

    private static InvoicePaymentDto MapPayment(InvoicePayment payment)
    {
        var allocations = payment.Allocations?
            .Where(a => !a.IsDeleted)
            .Select(a => new PaymentAllocationDto
            {
                Id = a.Id,
                InvoiceId = a.SaleInvoiceId ?? a.PurchaseId ?? 0,
                InvoiceNo = a.SaleInvoice?.InvoiceNo ?? a.Purchase?.InvoiceNo,
                AppliedAmount = a.AppliedAmount
            })
            .ToList() ?? [];

        var invoiceId = payment.SaleInvoiceId ?? payment.PurchaseId
            ?? allocations.FirstOrDefault()?.InvoiceId;

        return new InvoicePaymentDto
        {
            Id = payment.Id,
            Module = payment.Module,
            InvoiceId = invoiceId,
            InvoiceNo = payment.SaleInvoice?.InvoiceNo ?? payment.Purchase?.InvoiceNo
                ?? allocations.FirstOrDefault()?.InvoiceNo,
            CustomerId = payment.CustomerId,
            CustomerName = payment.Customer?.Name,
            SupplierId = payment.SupplierId,
            SupplierName = payment.Supplier?.Name,
            PaymentType = payment.PaymentType,
            Category = payment.Category,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            ReferenceNo = payment.ReferenceNo,
            Notes = payment.Notes,
            CreatedBy = payment.CreatedBy,
            CreatedAt = payment.CreatedAt,
            IsReversed = payment.IsReversed,
            Allocations = allocations
        };
    }

    private sealed class AllocationPlanItem
    {
        public int InvoiceId { get; set; }
        public string? InvoiceNo { get; set; }
        public decimal AppliedAmount { get; set; }
    }
}
