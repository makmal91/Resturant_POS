using POSSystem.Application.Payments.DTOs;
using POSSystem.Domain;
using CustomerEntity = POSSystem.Domain.Customer;
using SupplierEntity = POSSystem.Domain.Supplier;
using PurchaseEntity = POSSystem.Domain.Purchase;

namespace POSSystem.Application.Payments.Interfaces;

public interface IInvoicePaymentRepository
{
    Task<InvoicePayment> AddAsync(InvoicePayment payment);
    Task SaveChangesAsync();

    Task<decimal> GetTotalPaidForSaleInvoiceAsync(int saleInvoiceId, int businessId, int branchId);
    Task<decimal> GetTotalPaidForPurchaseAsync(int purchaseId, int businessId, int branchId);
    Task<Dictionary<int, decimal>> GetPaidTotalsForSaleInvoicesAsync(IEnumerable<int> saleInvoiceIds, int businessId, int branchId);
    Task<Dictionary<int, decimal>> GetPaidTotalsForPurchasesAsync(IEnumerable<int> purchaseIds, int businessId, int branchId);

    Task<List<InvoicePayment>> GetBySaleInvoiceIdAsync(int saleInvoiceId, int businessId, int branchId);
    Task<List<InvoicePayment>> GetByPurchaseIdAsync(int purchaseId, int businessId, int branchId);
    Task<List<InvoicePayment>> GetFilteredAsync(InvoicePaymentFilterDto filter);

    Task<SaleInvoice?> GetSaleInvoiceAsync(int saleInvoiceId, int businessId, int branchId);
    Task<PurchaseEntity?> GetPurchaseAsync(int purchaseId, int businessId, int branchId);
    Task<CustomerEntity?> GetCustomerAsync(int customerId, int businessId, int branchId);
    Task<SupplierEntity?> GetSupplierAsync(int supplierId, int businessId, int branchId);

    Task SyncSaleInvoicePaidCacheAsync(int saleInvoiceId, int businessId, int branchId, decimal paidTotal);

    Task<List<OutstandingInvoiceOptionDto>> GetOutstandingSaleInvoicesAsync(int customerId, int businessId, int branchId);
    Task<List<OutstandingInvoiceOptionDto>> GetOutstandingPurchaseInvoicesAsync(int supplierId, int businessId, int branchId);
}
