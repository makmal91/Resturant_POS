using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Barcode.DTOs;
using POSSystem.Application.Barcode.Interfaces;
using POSSystem.Application.Common.DTOs;
using POSSystem.Infrastructure.Data;
using ProductEntity = POSSystem.Domain.Product;

namespace POSSystem.Infrastructure.Repositories;

public class BarcodePrintRepository : IBarcodePrintRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public BarcodePrintRepository(POSDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<ProductEntity>> SearchProductsAsync(BarcodePrintSearchRequestDto request)
    {
        request.Page = Math.Max(1, request.Page);
        request.PageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.SubCategory)
            .Include(p => p.Brand)
            .Include(p => p.Units.Where(u => !u.IsDeleted))
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Include(p => p.Barcodes.Where(b => !b.IsDeleted))
            .Where(p => !p.IsDeleted && p.BusinessId == request.BusinessId);

        if (request.BranchId > 0)
            query = query.Where(p => p.BranchId == request.BranchId);

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        if (request.SubCategoryId.HasValue)
            query = query.Where(p => p.SubCategoryId == request.SubCategoryId.Value);

        if (request.BrandId.HasValue)
            query = query.Where(p => p.BrandId == request.BrandId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.ProductName.ToLower().Contains(term) ||
                p.Barcodes.Any(b => !b.IsDeleted && b.BarcodeValue.ToLower().Contains(term)));
        }

        if (request.InStockOnly)
        {
            var inStockProductIds = _context.StockLedgerEntries
                .IgnoreQueryFilters()
                .Where(e => !e.IsDeleted
                            && e.BusinessId == request.BusinessId
                            && e.BranchId == request.BranchId)
                .GroupBy(e => e.ProductId)
                .Where(g => g.Sum(e => e.QuantityInBaseUnit) > 0)
                .Select(g => g.Key);

            query = query.Where(p => inStockProductIds.Contains(p.Id));
        }

        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)request.PageSize);
        if (totalPages > 0 && request.Page > totalPages)
            request.Page = totalPages;

        var data = await query
            .OrderByDescending(p => p.Id)
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

    public async Task<Dictionary<int, decimal>> GetProductStockTotalsAsync(
        int businessId, int branchId, IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, decimal>();

        var rows = await _context.StockLedgerEntries
            .IgnoreQueryFilters()
            .Where(e => !e.IsDeleted
                        && e.BusinessId == businessId
                        && e.BranchId == branchId
                        && ids.Contains(e.ProductId))
            .GroupBy(e => e.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(e => e.QuantityInBaseUnit) })
            .ToListAsync();

        return rows.ToDictionary(r => r.ProductId, r => r.Total);
    }
}
