using POSSystem.Application.Common.DTOs;
using POSSystem.Domain;
using ProductEntity = POSSystem.Domain.Product;
using CustomerEntity = POSSystem.Domain.Customer;

namespace POSSystem.Application.Sales.Interfaces;

public interface ISalesRepository
{
    Task<ProductEntity?> GetProductByBarcodeAsync(string barcode, int businessId, int branchId);
    Task<List<ProductEntity>> SearchProductsAsync(string query, int businessId, int branchId, int take = 20);
    Task<List<CustomerEntity>> SearchCustomersAsync(string query, int businessId, int branchId, int take = 10);
    Task<SaleInvoice?> GetByIdAsync(int id, int businessId, int branchId);
    Task<PagedResultDto<SaleInvoice>> GetPagedAsync(
        int businessId, int branchId, int page, int pageSize,
        string? search, SaleInvoiceStatus? status, DateTime? dateFrom, DateTime? dateTo);
    Task<List<SaleInvoice>> GetHeldBillsAsync(int businessId, int branchId);
    Task AddAsync(SaleInvoice invoice);
    Task SaveChangesAsync();
}
