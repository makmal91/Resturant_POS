using POSSystem.Application.Sales.DTOs;

namespace POSSystem.Application.Sales.Interfaces;

public interface ISalesService
{
    Task<PosProductLookupDto?> GetProductByBarcodeAsync(string barcode, int businessId, int branchId);
    Task<List<PosProductLookupDto>> SearchProductsAsync(string query, int businessId, int branchId);
    Task<List<PosSearchGroupDto>> SearchProductsGroupedAsync(string query, int businessId, int branchId, int? warehouseId);
    Task<List<PosCustomerDto>> SearchCustomersAsync(string query, int businessId, int branchId);
    Task<SaleInvoiceDto> CreateSaleInvoiceAsync(CreateSaleInvoiceDto dto);
    Task<SaleInvoiceDto> HoldBillAsync(HoldBillDto dto);
    Task<List<SaleInvoiceDto>> GetHeldBillsAsync(int businessId, int branchId);
    Task<SaleInvoiceDto?> GetInvoiceByIdAsync(int id, int businessId, int branchId);
    Task CancelHeldBillAsync(int id, int businessId, int branchId);

    // ─── Transaction correction ───────────────────────────────────────────────
    Task<SaleInvoiceDto> UpdateSaleInvoiceAsync(int id, UpdateSaleInvoiceDto dto);
    Task<SaleInvoiceDto> VoidSaleInvoiceAsync(int id, VoidSaleInvoiceDto dto);
    Task<List<SaleLedgerEntryDto>> GetSaleLedgerHistoryAsync(int invoiceId, int businessId, int branchId);
}
