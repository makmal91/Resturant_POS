using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Purchase.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Purchase.Interfaces;

public interface IPurchaseService
{
    Task<PagedResultDto<PurchaseDto>> GetPurchasesPagedAsync(int businessId, int branchId, int page, int pageSize, string? search = null, PurchaseStatus? status = null);
    Task<PurchaseDetailDto?> GetPurchaseByIdAsync(int id, int businessId, int branchId);
    Task<PurchaseDetailDto> CreatePurchaseAsync(CreatePurchaseDto dto);
    Task<PurchaseDetailDto?> UpdatePurchaseAsync(int id, UpdatePurchaseDto dto);
    Task<PurchaseDetailDto> PostPurchaseAsync(int id, PostPurchaseDto dto);
    Task DeletePurchaseAsync(int id, int businessId, int branchId);
}
