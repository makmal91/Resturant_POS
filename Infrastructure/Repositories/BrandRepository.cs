using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Brand.DTOs;
using POSSystem.Application.Brand.Interfaces;
using POSSystem.Application.Common.DTOs;
using POSSystem.Infrastructure.Data;
using BrandEntity = POSSystem.Domain.Brand;

namespace POSSystem.Infrastructure.Repositories;

public class BrandRepository : IBrandRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public BrandRepository(POSDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<BrandEntity>> GetPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null,
        bool? status = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _context.Brands
            .IgnoreQueryFilters()
            .Where(b => !b.IsDeleted && b.BusinessId == businessId);

        if (branchId > 0)
        {
            query = query.Where(b => b.BranchId == branchId);
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(b =>
                b.Name.ToLower().Contains(term) ||
                b.Description.ToLower().Contains(term));
        }

        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
            page = totalPages;

        var orderedQuery = branchId == 0
            ? query.OrderBy(b => b.Branch!.Name).ThenBy(b => b.Name)
            : query.OrderBy(b => b.Name);

        var data = await orderedQuery
            .Include(b => b.Branch)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<BrandEntity>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page
        };
    }

    public async Task<BrandEntity?> GetByIdAsync(int id, int businessId, int branchId)
    {
        var query = _context.Brands
            .IgnoreQueryFilters()
            .Where(b => b.Id == id && !b.IsDeleted && b.BusinessId == businessId);

        if (branchId > 0)
        {
            query = query.Where(b => b.BranchId == branchId);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<BrandEntity?> GetByNameAsync(string name, int businessId, int branchId, int? excludeBrandId = null)
    {
        var normalizedName = name.Trim().ToLower();

        return await _context.Brands
            .IgnoreQueryFilters()
            .Where(b =>
                !b.IsDeleted &&
                b.BusinessId == businessId &&
                b.BranchId == branchId &&
                b.Name.ToLower() == normalizedName &&
                (!excludeBrandId.HasValue || b.Id != excludeBrandId.Value))
            .FirstOrDefaultAsync();
    }

    public async Task<BrandDetailDto?> GetDetailByIdAsync(int id, int businessId, int branchId)
    {
        var query = _context.Brands
            .IgnoreQueryFilters()
            .Where(b => b.Id == id && !b.IsDeleted && b.BusinessId == businessId);

        if (branchId > 0)
        {
            query = query.Where(b => b.BranchId == branchId);
        }

        return await query
            .Select(b => new BrandDetailDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                Status = b.Status,
                HasImage = b.ImageData != null && b.ImageData.Length > 0,
                ImageContentType = b.ImageContentType,
                ImageFileName = b.ImageFileName,
                BranchId = b.BranchId,
                BranchName = b.Branch != null ? b.Branch.Name : string.Empty,
                CreatedDate = b.CreatedDate,
                UpdatedDate = b.UpdatedDate,
                CreatedById = b.CreatedById,
                CreatedByName = b.CreatedByName,
                ModifiedById = b.ModifiedById,
                ModifiedByName = b.ModifiedByName
            })
            .FirstOrDefaultAsync();
    }

    public async Task<BrandImageDto?> GetImageByIdAsync(int id, int businessId, int branchId)
    {
        var query = _context.Brands
            .IgnoreQueryFilters()
            .Where(b => b.Id == id && !b.IsDeleted && b.BusinessId == businessId && b.ImageData != null);

        if (branchId > 0)
        {
            query = query.Where(b => b.BranchId == branchId);
        }

        return await query
            .Select(b => new BrandImageDto
            {
                ImageData = b.ImageData!,
                ImageContentType = b.ImageContentType,
                ImageFileName = b.ImageFileName
            })
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(BrandEntity brand)
    {
        await _context.Brands.AddAsync(brand);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
