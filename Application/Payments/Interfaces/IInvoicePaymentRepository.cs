using POSSystem.Application.Payments.DTOs;
using POSSystem.Domain;
using CustomerEntity = POSSystem.Domain.Customer;
using SupplierEntity = POSSystem.Domain.Supplier;
using PurchaseEntity = POSSystem.Domain.Purchase;

namespace POSSystem.Application.Payments.Interfaces;

public interface IInvoicePaymentRepository
{
    Task<InvoicePayment> AddAsync(InvoicePayment payment);
    Task<PaymentAllocation> AddAllocationAsync(PaymentAllocation allocation);
    Task SaveChangesAsync();

    Task<InvoicePayment?> GetByIdAsync(int paymentId, int businessId, int branchId);
    Task<List<PaymentAllocation>> GetAllocationsByPaymentIdAsync(int paymentId, int businessId, int branchId);

    Task<decimal> GetTotalPaidForSaleInvoiceAsync(int saleInvoiceId, int businessId, int branchId);
    Task<decimal> GetTotalPaidForPurchaseAsync(int purchaseId, int businessId, int branchId);
    Task<Dictionary<int, decimal>> GetPaidTotalsForSaleInvoicesAsync(IEnumerable<int> saleInvoiceIds, int businessId, int branchId);
    Task<Dictionary<int, decimal>> GetPaidTotalsForPurchasesAsync(IEnumerable<int> purchaseIds, int businessId, int branchId);
    Task<Dictionary<int, decimal>> GetPaidTotalsForSaleInvoicesAsOfAsync(
        IEnumerable<int> saleInvoiceIds, int businessId, int branchId, DateTime asOfDate);
    Task<Dictionary<int, decimal>> GetPaidTotalsForPurchasesAsOfAsync(
        IEnumerable<int> purchaseIds, int businessId, int branchId, DateTime asOfDate);

    Task<List<InvoicePayment>> GetBySaleInvoiceIdAsync(int saleInvoiceId, int businessId, int branchId);
    Task<List<InvoicePayment>> GetByPurchaseIdAsync(int purchaseId, int businessId, int branchId);
    Task<List<InvoicePayment>> GetFilteredAsync(InvoicePaymentFilterDto filter);

    Task<SaleInvoice?> GetSaleInvoiceAsync(int saleInvoiceId, int businessId, int branchId);
    Task<PurchaseEntity?> GetPurchaseAsync(int purchaseId, int businessId, int branchId);
    Task<CustomerEntity?> GetCustomerAsync(int customerId, int businessId, int branchId);
    Task<SupplierEntity?> GetSupplierAsync(int supplierId, int businessId, int branchId);

    Task SyncSaleInvoicePaidCacheAsync(int saleInvoiceId, int businessId, int branchId, decimal paidTotal, InvoiceSettlementStatus status);
    Task SyncPurchasePaidCacheAsync(int purchaseId, int businessId, int branchId, decimal paidTotal, InvoiceSettlementStatus status);

    Task<List<OutstandingInvoiceOptionDto>> GetOutstandingSaleInvoicesAsync(
        int customerId, int businessId, int branchId, int? excludePaymentId = null);
    Task<List<OutstandingInvoiceOptionDto>> GetOutstandingPurchaseInvoicesAsync(
        int supplierId, int businessId, int branchId, int? excludePaymentId = null);

    Task<decimal> GetSupplierOutstandingPayableAsync(int supplierId, int businessId, int branchId, DateTime? asOfDate = null);

    Task SoftDeletePaymentAsync(InvoicePayment payment, int? deletedBy);
    Task SoftDeleteAllocationsAsync(IEnumerable<PaymentAllocation> allocations, int? deletedBy);
}
