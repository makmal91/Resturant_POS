using POSSystem.Application.Ledger.DTOs;

namespace POSSystem.Application.Ledger.Interfaces;

public interface IPartyLedgerService
{
    Task RecordCreditSaleAsync(
        int businessId, int branchId, int customerId, int saleInvoiceId,
        string invoiceNo, decimal amount, DateTime? transactionDate = null);

    Task ReverseCreditSaleAsync(
        int businessId, int branchId, int customerId, int saleInvoiceId,
        string invoiceNo, decimal amount, DateTime? transactionDate = null, string? reason = null);

    Task RecordCreditPurchaseAsync(
        int businessId, int branchId, int supplierId, int purchaseId,
        string invoiceNo, decimal amount, DateTime? transactionDate = null);

    Task ReverseCreditPurchaseAsync(
        int businessId, int branchId, int supplierId, int purchaseId,
        string invoiceNo, decimal amount, DateTime? transactionDate = null, string? reason = null);

    Task<PartyLedgerEntryDto> ReceiveCustomerPaymentAsync(ReceiveCustomerPaymentDto dto);
    Task<PartyLedgerEntryDto> PaySupplierAsync(PaySupplierDto dto);
    Task<PartyBalanceDto> GetCustomerBalanceAsync(int customerId, int businessId, int branchId);
    Task<PartyBalanceDto> GetSupplierBalanceAsync(int supplierId, int businessId, int branchId);
    Task<PartyLedgerPageDto> GetCustomerLedgerAsync(PartyLedgerFilterDto filter);
    Task<PartyLedgerPageDto> GetSupplierLedgerAsync(PartyLedgerFilterDto filter);
}
