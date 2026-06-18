using POSSystem.Application.Payments.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Payments.Interfaces;

public interface IInvoicePaymentService
{
    Task<InvoicePaymentDto> RecordCustomerPaymentAsync(RecordCustomerPaymentDto dto);
    Task<InvoicePaymentDto> RecordSupplierPaymentAsync(RecordSupplierPaymentDto dto);

    Task RecordPosSalePaymentsAsync(SaleInvoice invoice, int? createdBy = null);

    Task<decimal> GetTotalPaidForSaleInvoiceAsync(int saleInvoiceId, int businessId, int branchId);
    Task<decimal> GetTotalPaidForPurchaseAsync(int purchaseId, int businessId, int branchId);
    Task<Dictionary<int, decimal>> GetPaidTotalsForSaleInvoicesAsync(IEnumerable<int> saleInvoiceIds, int businessId, int branchId);
    Task<Dictionary<int, decimal>> GetPaidTotalsForPurchasesAsync(IEnumerable<int> purchaseIds, int businessId, int branchId);

    Task<List<InvoicePaymentDto>> GetPaymentsForSaleInvoiceAsync(int saleInvoiceId, int businessId, int branchId);
    Task<List<InvoicePaymentDto>> GetPaymentsForPurchaseAsync(int purchaseId, int businessId, int branchId);

    Task<InvoiceBalanceDto> GetSaleInvoiceBalanceAsync(int saleInvoiceId, int businessId, int branchId);
    Task<InvoiceBalanceDto> GetPurchaseBalanceAsync(int purchaseId, int businessId, int branchId);

    Task<List<OutstandingInvoiceOptionDto>> GetOutstandingSaleInvoicesAsync(int customerId, int businessId, int branchId);
    Task<List<OutstandingInvoiceOptionDto>> GetOutstandingPurchaseInvoicesAsync(int supplierId, int businessId, int branchId);
}
