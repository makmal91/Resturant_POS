using POSSystem.Application.Brand.DTOs;
using POSSystem.Application.Common.DTOs;
using BrandEntity = POSSystem.Domain.Brand;

namespace POSSystem.Application.Brand.Interfaces;

public interface IBrandRepository
{
    Task<PagedResultDto<BrandEntity>> GetPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null,
        bool? status = null);

    Task<BrandEntity?> GetByIdAsync(int id, int businessId, int branchId);
    Task<BrandEntity?> GetByNameAsync(string name, int businessId, int branchId, int? excludeBrandId = null);
    Task<BrandDetailDto?> GetDetailByIdAsync(int id, int businessId, int branchId);
    Task<BrandImageDto?> GetImageByIdAsync(int id, int businessId, int branchId);
    Task AddAsync(BrandEntity brand);
    Task SaveChangesAsync();
}
