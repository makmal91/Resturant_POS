using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Purchase.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class PurchaseRepository : IPurchaseRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public PurchaseRepository(POSDbContext context) => _context = context;

    public async Task<PagedResultDto<Purchase>> GetPagedAsync(
        int businessId, int branchId, int page, int pageSize, string? search = null, PurchaseStatus? status = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _context.Purchases
            .IgnoreQueryFilters()
            .Where(p => !p.IsDeleted && p.BusinessId == businessId);

        if (branchId > 0) query = query.Where(p => p.BranchId == branchId);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.InvoiceNo.ToLower().Contains(term)
                || p.Supplier.Name.ToLower().Contains(term));
        }

        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);
        if (totalPages > 0 && page > totalPages) page = totalPages;

        var data = await query
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.Branch)
            .Include(p => p.Items.Where(i => !i.IsDeleted))
            .OrderByDescending(p => p.PurchaseDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<Purchase> { Data = data, TotalRecords = totalRecords, TotalPages = totalPages, CurrentPage = page };
    }

    public async Task<Purchase?> GetByIdAsync(int id, int businessId, int branchId)
    {
        var query = _context.Purchases
            .IgnoreQueryFilters()
            .Where(p => p.Id == id && !p.IsDeleted && p.BusinessId == businessId);

        if (branchId > 0) query = query.Where(p => p.BranchId == branchId);
        return await query.FirstOrDefaultAsync();
    }

    public async Task<Purchase?> GetByIdWithItemsAsync(int id, int businessId, int branchId)
    {
        var query = _context.Purchases
            .IgnoreQueryFilters()
            .Where(p => p.Id == id && !p.IsDeleted && p.BusinessId == businessId);

        if (branchId > 0) query = query.Where(p => p.BranchId == branchId);

        return await query
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.Branch)
            .Include(p => p.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Product)
            .Include(p => p.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Variant)
            .Include(p => p.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Unit)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> InvoiceExistsAsync(string invoiceNo, int businessId, int branchId, int? excludeId = null)
    {
        var normalized = invoiceNo.Trim().ToLower();
        return await _context.Purchases
            .IgnoreQueryFilters()
            .AnyAsync(p => !p.IsDeleted && p.BusinessId == businessId && p.BranchId == branchId
                && p.InvoiceNo.ToLower() == normalized
                && (!excludeId.HasValue || p.Id != excludeId.Value));
    }

    public async Task AddAsync(Purchase purchase) => await _context.Purchases.AddAsync(purchase);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
