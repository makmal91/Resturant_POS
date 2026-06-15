using POSSystem.Application.Brand.DTOs;
using POSSystem.Application.Common.DTOs;
using BrandEntity = POSSystem.Domain.Brand;

namespace POSSystem.Application.Brand.Interfaces;

public interface IBrandService
{
    Task<PagedResultDto<BrandDto>> GetBrandsPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null,
        bool? status = null);

    Task<BrandDetailDto?> GetBrandByIdAsync(int id, int businessId, int branchId);
    Task<BrandImageDto?> GetBrandImageAsync(int id, int businessId, int branchId);
    Task<BrandEntity> AddBrandAsync(CreateBrandDto dto, byte[]? imageBytes = null, string? imageFileName = null, string? imageContentType = null);
    Task<BrandDetailDto?> UpdateBrandAsync(
        int id,
        UpdateBrandDto dto,
        byte[]? imageBytes = null,
        string? imageFileName = null,
        string? imageContentType = null,
        bool replaceImage = false);
    Task PatchBrandStatusAsync(BrandStatusPatchDto dto);
    Task DeleteBrandAsync(int id, int businessId, int branchId);
}
