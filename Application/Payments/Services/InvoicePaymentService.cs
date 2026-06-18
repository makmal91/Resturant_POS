using POSSystem.Application.CashFlow.Interfaces;
using POSSystem.Application.Ledger.Interfaces;
using POSSystem.Application.Payments.DTOs;
using POSSystem.Application.Payments.Interfaces;
using POSSystem.Domain;
using PurchaseEntity = POSSystem.Domain.Purchase;

namespace POSSystem.Application.Payments.Services;

public class InvoicePaymentService : IInvoicePaymentService
{
    private readonly IInvoicePaymentRepository _repository;
    private readonly IPartyLedgerRepository _partyLedgerRepository;
    private readonly ICashFlowService _cashFlowService;

    public InvoicePaymentService(
        IInvoicePaymentRepository repository,
        IPartyLedgerRepository partyLedgerRepository,
        ICashFlowService cashFlowService)
    {
        _repository = repository;
        _partyLedgerRepository = partyLedgerRepository;
        _cashFlowService = cashFlowService;
    }

    public async Task<InvoicePaymentDto> RecordCustomerPaymentAsync(RecordCustomerPaymentDto dto)
    {
        ValidateAmount(dto.Amount);

        _ = await _repository.GetCustomerAsync(dto.CustomerId, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Customer not found.");

        SaleInvoice? invoice = null;
        if (dto.SaleInvoiceId.HasValue)
        {
            invoice = await _repository.GetSaleInvoiceAsync(dto.SaleInvoiceId.Value, dto.BusinessId, dto.BranchId)
                ?? throw new InvalidOperationException("Sale invoice not found.");

            if (!invoice.CustomerId.HasValue || invoice.CustomerId.Value != dto.CustomerId)
                throw new InvalidOperationException("Customer does not match the selected invoice.");

            if (invoice.Status != SaleInvoiceStatus.Completed)
                throw new InvalidOperationException("Payments can only be recorded against completed sale invoices.");

            var paid = await _repository.GetTotalPaidForSaleInvoiceAsync(invoice.Id, dto.BusinessId, dto.BranchId);
            var due = invoice.GrandTotal - paid;
            if (dto.Amount > due)
                throw new InvalidOperationException($"Payment exceeds invoice balance due of {due:N2}.");
        }

        var paymentDate = NormalizeLedgerPaymentDate(dto.PaymentDate, invoice?.SaleDate);
        var payment = new InvoicePayment
        {
            Module = InvoicePaymentModule.Sale,
            SaleInvoiceId = dto.SaleInvoiceId,
            CustomerId = dto.CustomerId,
            PaymentType = dto.PaymentType,
            Amount = dto.Amount,
            PaymentDate = paymentDate,
            ReferenceNo = dto.ReferenceNo?.Trim() ?? string.Empty,
            Notes = dto.Notes?.Trim() ?? string.Empty,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId,
            CreatedBy = dto.CreatedBy
        };

        await _repository.AddAsync(payment);
        await _repository.SaveChangesAsync();

        var remarks = BuildCustomerPaymentRemarks(invoice, dto.Notes);

        await RecordCustomerLedgerPaymentAsync(
            dto.CustomerId, dto.BusinessId, dto.BranchId, dto.Amount, paymentDate,
            invoice?.Id ?? payment.Id, remarks);

        if (ShouldRecordCustomerPaymentCashFlow(invoice))
        {
            await _cashFlowService.RecordCustomerPaymentAsync(
                dto.BusinessId,
                dto.BranchId,
                payment.Id,
                string.IsNullOrWhiteSpace(dto.ReferenceNo) ? invoice?.InvoiceNo : dto.ReferenceNo.Trim(),
                remarks,
                dto.Amount,
                dto.PaymentType,
                paymentDate);
        }

        if (invoice != null)
            await SyncSaleInvoicePaidCacheAsync(invoice.Id, dto.BusinessId, dto.BranchId);

        payment.SaleInvoice = invoice;
        payment.Customer = await _repository.GetCustomerAsync(dto.CustomerId, dto.BusinessId, dto.BranchId);
        return MapPayment(payment);
    }

    public async Task<InvoicePaymentDto> RecordSupplierPaymentAsync(RecordSupplierPaymentDto dto)
    {
        ValidateAmount(dto.Amount);

        _ = await _repository.GetSupplierAsync(dto.SupplierId, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Supplier not found.");

        PurchaseEntity? purchase = null;
        if (dto.PurchaseId.HasValue)
        {
            purchase = await _repository.GetPurchaseAsync(dto.PurchaseId.Value, dto.BusinessId, dto.BranchId)
                ?? throw new InvalidOperationException("Purchase invoice not found.");

            if (purchase.SupplierId != dto.SupplierId)
                throw new InvalidOperationException("Supplier does not match the selected purchase invoice.");

            if (purchase.Status != PurchaseStatus.Posted)
                throw new InvalidOperationException("Payments can only be recorded against posted purchase invoices.");

            var paid = await _repository.GetTotalPaidForPurchaseAsync(purchase.Id, dto.BusinessId, dto.BranchId);
            var due = purchase.TotalAmount - paid;
            if (dto.Amount > due)
                throw new InvalidOperationException($"Payment exceeds invoice balance due of {due:N2}.");
        }

        var paymentDate = NormalizeLedgerPaymentDate(dto.PaymentDate, purchase?.PurchaseDate);
        var payment = new InvoicePayment
        {
            Module = InvoicePaymentModule.Purchase,
            PurchaseId = dto.PurchaseId,
            SupplierId = dto.SupplierId,
            PaymentType = dto.PaymentType,
            Amount = dto.Amount,
            PaymentDate = paymentDate,
            ReferenceNo = dto.ReferenceNo?.Trim() ?? string.Empty,
            Notes = dto.Notes?.Trim() ?? string.Empty,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId,
            CreatedBy = dto.CreatedBy
        };

        await _repository.AddAsync(payment);
        await _repository.SaveChangesAsync();

        var remarks = BuildSupplierPaymentRemarks(purchase, dto.Notes);

        await RecordSupplierLedgerPaymentAsync(
            dto.SupplierId, dto.BusinessId, dto.BranchId, dto.Amount, paymentDate,
            purchase?.Id ?? payment.Id, remarks);

        await _cashFlowService.RecordSupplierPaymentAsync(
            dto.BusinessId,
            dto.BranchId,
            payment.Id,
            string.IsNullOrWhiteSpace(dto.ReferenceNo) ? purchase?.InvoiceNo : dto.ReferenceNo.Trim(),
            remarks,
            dto.Amount,
            dto.PaymentType,
            paymentDate);

        payment.Purchase = purchase;
        payment.Supplier = await _repository.GetSupplierAsync(dto.SupplierId, dto.BusinessId, dto.BranchId);
        return MapPayment(payment);
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
            BalanceDue = invoice.GrandTotal - paid
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
            BalanceDue = purchase.TotalAmount - paid
        };
    }

    public Task<List<OutstandingInvoiceOptionDto>> GetOutstandingSaleInvoicesAsync(
        int customerId, int businessId, int branchId)
        => _repository.GetOutstandingSaleInvoicesAsync(customerId, businessId, branchId);

    public Task<List<OutstandingInvoiceOptionDto>> GetOutstandingPurchaseInvoicesAsync(
        int supplierId, int businessId, int branchId)
        => _repository.GetOutstandingPurchaseInvoicesAsync(supplierId, businessId, branchId);

    private async Task SyncSaleInvoicePaidCacheAsync(int saleInvoiceId, int businessId, int branchId)
    {
        var total = await _repository.GetTotalPaidForSaleInvoiceAsync(saleInvoiceId, businessId, branchId);
        await _repository.SyncSaleInvoicePaidCacheAsync(saleInvoiceId, businessId, branchId, total);
    }

    private async Task RecordCustomerLedgerPaymentAsync(
        int customerId, int businessId, int branchId, decimal amount, DateTime paymentDate,
        int referenceId, string remarks)
    {
        var previousBalance = await _partyLedgerRepository.GetCustomerRunningBalanceAsync(customerId, businessId, branchId);

        await _partyLedgerRepository.AddCustomerEntryAsync(new CustomerLedgerTransaction
        {
            CustomerId = customerId,
            ReferenceId = referenceId,
            Type = CustomerLedgerTransactionType.PaymentReceived,
            Debit = 0,
            Credit = amount,
            Date = paymentDate,
            RunningBalance = previousBalance - amount,
            Remarks = remarks,
            BusinessId = businessId,
            BranchId = branchId
        });

        await _partyLedgerRepository.SaveChangesAsync();
    }

    private async Task RecordSupplierLedgerPaymentAsync(
        int supplierId, int businessId, int branchId, decimal amount, DateTime paymentDate,
        int referenceId, string remarks)
    {
        var previousBalance = await _partyLedgerRepository.GetSupplierRunningBalanceAsync(supplierId, businessId, branchId);

        await _partyLedgerRepository.AddSupplierEntryAsync(new SupplierLedgerTransaction
        {
            SupplierId = supplierId,
            ReferenceId = referenceId,
            Type = SupplierLedgerTransactionType.PaymentMade,
            Debit = amount,
            Credit = 0,
            Date = paymentDate,
            RunningBalance = previousBalance - amount,
            Remarks = remarks,
            BusinessId = businessId,
            BranchId = branchId
        });

        await _partyLedgerRepository.SaveChangesAsync();
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");
    }

    /// <summary>
    /// Cash sales already post to cash flow as Sale entries; only credit/advance receipts are CashIn.
    /// </summary>
    private static bool ShouldRecordCustomerPaymentCashFlow(SaleInvoice? invoice)
        => invoice == null || invoice.IsCreditSale;

    private static string BuildCustomerPaymentRemarks(SaleInvoice? invoice, string? notes)
    {
        if (invoice != null)
        {
            var baseText = $"Payment Received — Invoice: {invoice.InvoiceNo}";
            return string.IsNullOrWhiteSpace(notes) ? baseText : $"{baseText} | {notes.Trim()}";
        }

        return string.IsNullOrWhiteSpace(notes) ? "Advance Payment Received" : notes.Trim();
    }

    private static string BuildSupplierPaymentRemarks(PurchaseEntity? purchase, string? notes)
    {
        if (purchase != null)
        {
            var baseText = $"Payment Made — Invoice: {purchase.InvoiceNo}";
            return string.IsNullOrWhiteSpace(notes) ? baseText : $"{baseText} | {notes.Trim()}";
        }

        return string.IsNullOrWhiteSpace(notes) ? "Advance Payment Made" : notes.Trim();
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

    /// <summary>
    /// Date-only payment dates sort before same-day invoices at midnight; use end-of-day
    /// and never before the linked invoice so ledger order matches business flow.
    /// </summary>
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

    private static InvoicePaymentDto MapPayment(InvoicePayment payment)
    {
        var invoiceId = payment.SaleInvoiceId ?? payment.PurchaseId;
        return new InvoicePaymentDto
        {
            Id = payment.Id,
            Module = payment.Module,
            InvoiceId = invoiceId,
            InvoiceNo = payment.SaleInvoice?.InvoiceNo ?? payment.Purchase?.InvoiceNo,
            CustomerId = payment.CustomerId,
            CustomerName = payment.Customer?.Name,
            SupplierId = payment.SupplierId,
            SupplierName = payment.Supplier?.Name,
            PaymentType = payment.PaymentType,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            ReferenceNo = payment.ReferenceNo,
            Notes = payment.Notes,
            CreatedBy = payment.CreatedBy,
            CreatedAt = payment.CreatedAt
        };
    }
}
