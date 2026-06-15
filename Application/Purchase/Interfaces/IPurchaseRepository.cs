using POSSystem.Application.Common.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Purchase.Interfaces;

public interface IPurchaseRepository
{
    Task<PagedResultDto<Domain.Purchase>> GetPagedAsync(int businessId, int branchId, int page, int pageSize, string? search = null, PurchaseStatus? status = null);
    Task<Domain.Purchase?> GetByIdAsync(int id, int businessId, int branchId);
    Task<Domain.Purchase?> GetByIdWithItemsAsync(int id, int businessId, int branchId);
    Task<bool> InvoiceExistsAsync(string invoiceNo, int businessId, int branchId, int? excludeId = null);
    Task AddAsync(Domain.Purchase purchase);
    Task SaveChangesAsync();
}
