using POSSystem.Application.Brand.DTOs;
using POSSystem.Application.Brand.Interfaces;
using POSSystem.Application.Common.DTOs;
using BrandEntity = POSSystem.Domain.Brand;

namespace POSSystem.Application.Brand.Services;

public class BrandService : IBrandService
{
    private readonly IBrandRepository _repository;

    public BrandService(IBrandRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<BrandDto>> GetBrandsPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null,
        bool? status = null)
    {
        var result = await _repository.GetPagedAsync(businessId, branchId, page, pageSize, search, status);

        return new PagedResultDto<BrandDto>
        {
            Data = result.Data.Select(MapBrandDto).ToList(),
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage
        };
    }

    public Task<BrandDetailDto?> GetBrandByIdAsync(int id, int businessId, int branchId)
    {
        return _repository.GetDetailByIdAsync(id, businessId, branchId);
    }

    public Task<BrandImageDto?> GetBrandImageAsync(int id, int businessId, int branchId)
    {
        return _repository.GetImageByIdAsync(id, businessId, branchId);
    }

    public async Task<BrandEntity> AddBrandAsync(
        CreateBrandDto dto,
        byte[]? imageBytes = null,
        string? imageFileName = null,
        string? imageContentType = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Brand name is required.");

        if (dto.BranchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        var duplicate = await _repository.GetByNameAsync(dto.Name, dto.BusinessId, dto.BranchId);
        if (duplicate != null)
            throw new InvalidOperationException("Brand name must be unique within the selected branch.");

        var brand = new BrandEntity
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            Status = dto.Status,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId
        };

        ApplyBrandImage(brand, imageBytes, imageFileName, imageContentType, imageBytes != null && imageBytes.Length > 0, false);

        await _repository.AddAsync(brand);
        await _repository.SaveChangesAsync();

        return brand;
    }

    public async Task<BrandDetailDto?> UpdateBrandAsync(
        int id,
        UpdateBrandDto dto,
        byte[]? imageBytes = null,
        string? imageFileName = null,
        string? imageContentType = null,
        bool replaceImage = false)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Brand name is required.");

        var brand = await _repository.GetByIdAsync(id, dto.BusinessId, dto.BranchId);
        if (brand == null)
            throw new InvalidOperationException("Brand not found.");

        if (brand.BranchId != dto.BranchId)
            throw new InvalidOperationException("Brand branch mismatch.");

        var duplicate = await _repository.GetByNameAsync(dto.Name, dto.BusinessId, dto.BranchId, id);
        if (duplicate != null)
            throw new InvalidOperationException("Brand name must be unique within the selected branch.");

        brand.Name = dto.Name.Trim();
        brand.Description = dto.Description?.Trim() ?? string.Empty;
        brand.Status = dto.Status;

        ApplyBrandImage(
            brand,
            imageBytes,
            imageFileName,
            imageContentType,
            replaceImage,
            replaceImage && (imageBytes == null || imageBytes.Length == 0));

        await _repository.SaveChangesAsync();

        return await _repository.GetDetailByIdAsync(id, dto.BusinessId, dto.BranchId);
    }

    public async Task PatchBrandStatusAsync(BrandStatusPatchDto dto)
    {
        if (dto.Items.Count == 0)
            throw new InvalidOperationException("At least one brand status update is required.");

        if (dto.BranchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        foreach (var item in dto.Items)
        {
            var brand = await _repository.GetByIdAsync(item.Id, dto.BusinessId, dto.BranchId);
            if (brand == null)
                throw new InvalidOperationException($"Brand {item.Id} not found.");

            brand.Status = item.Status;
        }

        await _repository.SaveChangesAsync();
    }

    public async Task DeleteBrandAsync(int id, int businessId, int branchId)
    {
        var brand = await _repository.GetByIdAsync(id, businessId, branchId);
        if (brand == null || brand.BranchId != branchId)
            throw new InvalidOperationException("Brand not found.");

        brand.IsDeleted = true;
        await _repository.SaveChangesAsync();
    }

    private static BrandDto MapBrandDto(BrandEntity brand)
    {
        return new BrandDto
        {
            Id = brand.Id,
            Name = brand.Name,
            Description = brand.Description,
            Status = brand.Status,
            HasImage = brand.ImageData != null && brand.ImageData.Length > 0,
            BranchId = brand.BranchId,
            BranchName = brand.Branch?.Name ?? string.Empty,
            CreatedAt = brand.CreatedAt,
            ModifiedAt = brand.ModifiedAt,
            CreatedBy = brand.CreatedBy,
            ModifiedBy = brand.ModifiedBy
        };
    }

    private static void ApplyBrandImage(
        BrandEntity brand,
        byte[]? imageBytes,
        string? imageFileName,
        string? imageContentType,
        bool replaceImage,
        bool removeImage)
    {
        if (!replaceImage)
            return;

        if (removeImage || imageBytes == null || imageBytes.Length == 0)
        {
            brand.ImageData = null;
            brand.ImageContentType = null;
            brand.ImageFileName = null;
            return;
        }

        brand.ImageData = imageBytes;
        brand.ImageContentType = imageContentType;
        brand.ImageFileName = imageFileName;
    }
}
