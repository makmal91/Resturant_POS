using POSSystem.Application.Ledger.DTOs;
using POSSystem.Application.Ledger.Interfaces;
using POSSystem.Application.Payments.DTOs;
using POSSystem.Application.Payments.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Ledger.Services;

public class PartyLedgerService : IPartyLedgerService
{
    private readonly IPartyLedgerRepository _repository;
    private readonly IInvoicePaymentService _invoicePaymentService;

    public PartyLedgerService(
        IPartyLedgerRepository repository,
        IInvoicePaymentService invoicePaymentService)
    {
        _repository = repository;
        _invoicePaymentService = invoicePaymentService;
    }

    public async Task RecordCreditSaleAsync(
        int businessId, int branchId, int customerId, int saleInvoiceId,
        string invoiceNo, decimal amount, DateTime? transactionDate = null)
    {
        if (amount <= 0) return;

        var previousBalance = await _repository.GetCustomerRunningBalanceAsync(customerId, businessId, branchId);

        await _repository.AddCustomerEntryAsync(new CustomerLedgerTransaction
        {
            CustomerId = customerId,
            ReferenceId = saleInvoiceId,
            Type = CustomerLedgerTransactionType.CreditSale,
            Debit = amount,
            Credit = 0,
            Date = transactionDate ?? DateTime.UtcNow,
            RunningBalance = previousBalance + amount,
            Remarks = $"Credit Sale — Invoice: {invoiceNo}",
            BusinessId = businessId,
            BranchId = branchId
        });

        await _repository.SaveChangesAsync();
    }

    public async Task ReverseCreditSaleAsync(
        int businessId, int branchId, int customerId, int saleInvoiceId,
        string invoiceNo, decimal amount, DateTime? transactionDate = null, string? reason = null)
    {
        if (amount <= 0) return;

        var previousBalance = await _repository.GetCustomerRunningBalanceAsync(customerId, businessId, branchId);
        var remarks = $"Void of Credit Sale — Invoice: {invoiceNo}";
        if (!string.IsNullOrWhiteSpace(reason))
            remarks += $" | Reason: {reason}";

        await _repository.AddCustomerEntryAsync(new CustomerLedgerTransaction
        {
            CustomerId = customerId,
            ReferenceId = saleInvoiceId,
            Type = CustomerLedgerTransactionType.Reversal,
            Debit = 0,
            Credit = amount,
            Date = transactionDate ?? DateTime.UtcNow,
            RunningBalance = previousBalance - amount,
            Remarks = remarks,
            BusinessId = businessId,
            BranchId = branchId
        });

        await _repository.SaveChangesAsync();
    }

    public async Task RecordCreditPurchaseAsync(
        int businessId, int branchId, int supplierId, int purchaseId,
        string invoiceNo, decimal amount, DateTime? transactionDate = null)
    {
        if (amount <= 0) return;

        var previousBalance = await _repository.GetSupplierRunningBalanceAsync(supplierId, businessId, branchId);

        await _repository.AddSupplierEntryAsync(new SupplierLedgerTransaction
        {
            SupplierId = supplierId,
            ReferenceId = purchaseId,
            Type = SupplierLedgerTransactionType.CreditPurchase,
            Debit = 0,
            Credit = amount,
            Date = transactionDate ?? DateTime.UtcNow,
            RunningBalance = previousBalance + amount,
            Remarks = $"Credit Purchase — Invoice: {invoiceNo}",
            BusinessId = businessId,
            BranchId = branchId
        });

        await _repository.SaveChangesAsync();
    }

    public async Task ReverseCreditPurchaseAsync(
        int businessId, int branchId, int supplierId, int purchaseId,
        string invoiceNo, decimal amount, DateTime? transactionDate = null, string? reason = null)
    {
        if (amount <= 0) return;

        var previousBalance = await _repository.GetSupplierRunningBalanceAsync(supplierId, businessId, branchId);
        var remarks = $"Void of Credit Purchase — Invoice: {invoiceNo}";
        if (!string.IsNullOrWhiteSpace(reason))
            remarks += $" | Reason: {reason}";

        await _repository.AddSupplierEntryAsync(new SupplierLedgerTransaction
        {
            SupplierId = supplierId,
            ReferenceId = purchaseId,
            Type = SupplierLedgerTransactionType.Reversal,
            Debit = amount,
            Credit = 0,
            Date = transactionDate ?? DateTime.UtcNow,
            RunningBalance = previousBalance - amount,
            Remarks = remarks,
            BusinessId = businessId,
            BranchId = branchId
        });

        await _repository.SaveChangesAsync();
    }

    public async Task<PartyLedgerEntryDto> ReceiveCustomerPaymentAsync(ReceiveCustomerPaymentDto dto)
    {
        var payment = await _invoicePaymentService.RecordCustomerPaymentAsync(new RecordCustomerPaymentDto
        {
            CustomerId = dto.CustomerId,
            SaleInvoiceId = dto.SaleInvoiceId,
            PaymentType = dto.PaymentType,
            Amount = dto.Amount,
            PaymentDate = dto.PaymentDate,
            ReferenceNo = dto.ReferenceNo,
            Notes = dto.Notes,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId
        });

        return MapPaymentToLedgerEntry(payment, isCustomer: true);
    }

    public async Task<PartyLedgerEntryDto> PaySupplierAsync(PaySupplierDto dto)
    {
        var payment = await _invoicePaymentService.RecordSupplierPaymentAsync(new RecordSupplierPaymentDto
        {
            SupplierId = dto.SupplierId,
            PurchaseId = dto.PurchaseId,
            PaymentType = dto.PaymentType,
            Amount = dto.Amount,
            PaymentDate = dto.PaymentDate,
            ReferenceNo = dto.ReferenceNo,
            Notes = dto.Notes,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId
        });

        return MapPaymentToLedgerEntry(payment, isCustomer: false);
    }

    public async Task<PartyBalanceDto> GetCustomerBalanceAsync(int customerId, int businessId, int branchId)
    {
        var customer = await _repository.GetCustomerAsync(customerId, businessId, branchId)
            ?? throw new InvalidOperationException("Customer not found.");

        var balance = await _repository.GetCustomerRunningBalanceAsync(customerId, businessId, branchId);
        return new PartyBalanceDto
        {
            PartyId = customerId,
            PartyName = customer.Name,
            Balance = balance
        };
    }

    public async Task<PartyBalanceDto> GetSupplierBalanceAsync(int supplierId, int businessId, int branchId)
    {
        var supplier = await _repository.GetSupplierAsync(supplierId, businessId, branchId)
            ?? throw new InvalidOperationException("Supplier not found.");

        var balance = await _repository.GetSupplierRunningBalanceAsync(supplierId, businessId, branchId);
        return new PartyBalanceDto
        {
            PartyId = supplierId,
            PartyName = supplier.Name,
            Balance = balance
        };
    }

    public Task<PartyLedgerPageDto> GetCustomerLedgerAsync(PartyLedgerFilterDto filter)
        => _repository.GetCustomerLedgerPagedAsync(filter);

    public Task<PartyLedgerPageDto> GetSupplierLedgerAsync(PartyLedgerFilterDto filter)
        => _repository.GetSupplierLedgerPagedAsync(filter);

    private static PartyLedgerEntryDto MapPaymentToLedgerEntry(InvoicePaymentDto payment, bool isCustomer)
    {
        var description = payment.Notes;
        if (payment.InvoiceId.HasValue && !string.IsNullOrWhiteSpace(payment.InvoiceNo))
            description = $"Invoice: {payment.InvoiceNo}" + (string.IsNullOrWhiteSpace(payment.Notes) ? "" : $" | {payment.Notes}");

        return new PartyLedgerEntryDto
        {
            Id = payment.Id,
            Date = payment.PaymentDate,
            Type = isCustomer ? CustomerLedgerTransactionType.PaymentReceived.ToString() : SupplierLedgerTransactionType.PaymentMade.ToString(),
            Description = description,
            Debit = isCustomer ? 0 : payment.Amount,
            Credit = isCustomer ? payment.Amount : 0,
            RunningBalance = 0,
            ReferenceId = payment.InvoiceId ?? payment.Id
        };
    }

    private static PartyLedgerEntryDto MapCustomerEntry(CustomerLedgerTransaction entry) => new()
    {
        Id = entry.Id,
        Date = entry.Date,
        Type = entry.Type.ToString(),
        Description = entry.Remarks,
        Debit = entry.Debit,
        Credit = entry.Credit,
        RunningBalance = entry.RunningBalance,
        ReferenceId = entry.ReferenceId
    };

    private static PartyLedgerEntryDto MapSupplierEntry(SupplierLedgerTransaction entry) => new()
    {
        Id = entry.Id,
        Date = entry.Date,
        Type = entry.Type.ToString(),
        Description = entry.Remarks,
        Debit = entry.Debit,
        Credit = entry.Credit,
        RunningBalance = entry.RunningBalance,
        ReferenceId = entry.ReferenceId
    };
}
