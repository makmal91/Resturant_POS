using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Product.DTOs;
using POSSystem.Application.Product.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;
using ProductEntity = POSSystem.Domain.Product;

namespace POSSystem.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public ProductRepository(POSDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<ProductEntity>> SearchAsync(ProductSearchRequestDto request)
    {
        request.Page = Math.Max(1, request.Page);
        request.PageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.Products
            .IgnoreQueryFilters()
            .Include(p => p.Category)
            .Include(p => p.SubCategory)
            .Include(p => p.Brand)
            .Include(p => p.Branch)
            .Include(p => p.Barcodes)
            .Include(p => p.Images)
            .Where(p => !p.IsDeleted && p.BusinessId == request.BusinessId);

        if (request.BranchId > 0)
            query = query.Where(p => p.BranchId == request.BranchId);

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        if (request.SubCategoryId.HasValue)
            query = query.Where(p => p.SubCategoryId == request.SubCategoryId.Value);

        if (request.BrandId.HasValue)
            query = query.Where(p => p.BrandId == request.BrandId.Value);

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.ProductName.ToLower().Contains(term) ||
                p.ProductCode.ToLower().Contains(term) ||
                p.SKU.ToLower().Contains(term) ||
                p.Barcodes.Any(b => !b.IsDeleted && b.BarcodeValue.ToLower().Contains(term)));
        }

        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)request.PageSize);
        if (totalPages > 0 && request.Page > totalPages)
            request.Page = totalPages;

        var data = await query
            .OrderBy(p => p.ProductName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResultDto<ProductEntity>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = request.Page
        };
    }

    public Task<ProductEntity?> GetByIdAsync(int id, int businessId, int branchId)
    {
        var query = _context.Products
            .IgnoreQueryFilters()
            .Include(p => p.Category)
            .Include(p => p.SubCategory)
            .Include(p => p.Brand)
            .Include(p => p.Branch)
            .Include(p => p.Units.Where(u => !u.IsDeleted))
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Include(p => p.Barcodes.Where(b => !b.IsDeleted))
            .Include(p => p.Images.Where(i => !i.IsDeleted))
            .Where(p => p.Id == id && !p.IsDeleted && p.BusinessId == businessId);

        if (branchId > 0)
            query = query.Where(p => p.BranchId == branchId);

        return query.FirstOrDefaultAsync();
    }

    public Task<bool> ProductCodeExistsAsync(string productCode, int businessId, int branchId, int? excludeProductId = null)
    {
        var normalized = productCode.Trim().ToLower();
        return _context.Products
            .IgnoreQueryFilters()
            .AnyAsync(p =>
                !p.IsDeleted &&
                p.BusinessId == businessId &&
                p.BranchId == branchId &&
                p.ProductCode.ToLower() == normalized &&
                (!excludeProductId.HasValue || p.Id != excludeProductId.Value));
    }

    public Task<bool> BarcodeExistsAsync(string barcodeValue, int? excludeBarcodeId = null)
    {
        var normalized = barcodeValue.Trim().ToLower();
        return _context.ProductBarcodes
            .IgnoreQueryFilters()
            .AnyAsync(b =>
                !b.IsDeleted &&
                b.BarcodeValue.ToLower() == normalized &&
                (!excludeBarcodeId.HasValue || b.Id != excludeBarcodeId.Value));
    }

    public Task<bool> CategoryExistsAsync(int categoryId, int businessId, int branchId)
    {
        return _context.MenuCategories
            .IgnoreQueryFilters()
            .AnyAsync(c => !c.IsDeleted && c.Id == categoryId && c.BusinessId == businessId && c.BranchId == branchId);
    }

    public Task<bool> SubCategoryBelongsToCategoryAsync(int subCategoryId, int categoryId, int businessId, int branchId)
    {
        return _context.SubCategories
            .IgnoreQueryFilters()
            .AnyAsync(s =>
                !s.IsDeleted &&
                s.Id == subCategoryId &&
                s.CategoryId == categoryId &&
                s.BusinessId == businessId &&
                s.BranchId == branchId);
    }

    public Task<bool> BrandExistsAsync(int brandId, int businessId, int branchId)
    {
        return _context.Brands
            .IgnoreQueryFilters()
            .AnyAsync(b => !b.IsDeleted && b.Id == brandId && b.BusinessId == businessId && b.BranchId == branchId);
    }

    public async Task AddAsync(ProductEntity product)
    {
        await _context.Products.AddAsync(product);
    }

    public Task<ProductImage?> GetImageByIdAsync(int productId, int imageId, int businessId, int branchId)
    {
        return _context.ProductImages
            .IgnoreQueryFilters()
            .Where(i =>
                i.Id == imageId &&
                i.ProductId == productId &&
                !i.IsDeleted &&
                i.BusinessId == businessId &&
                i.BranchId == branchId)
            .FirstOrDefaultAsync();
    }

    public void RemoveImage(ProductImage image)
    {
        _context.ProductImages.Remove(image);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
