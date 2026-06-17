using POSSystem.Application.Inventory.Interfaces;
using POSSystem.Application.Common.DTOs;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace POSSystem.Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public InventoryRepository(POSDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryItem?> GetInventoryItemAsync(int id, int businessId, int branchId)
    {
        return await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id && i.BusinessId == businessId && i.BranchId == branchId);
    }

    public async Task<InventoryItem?> GetInventoryItemByNameAndBranchAsync(string name, int businessId, int branchId)
    {
        return await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.Name == name && i.BusinessId == businessId && i.BranchId == branchId);
    }

    public async Task<ICollection<InventoryItem>> GetInventoryItemsAsync(IEnumerable<int> ids, int businessId, int branchId)
    {
        return await _context.InventoryItems
            .Where(i => ids.Contains(i.Id) && i.BusinessId == businessId && i.BranchId == branchId)
            .ToListAsync();
    }

    public async Task<ICollection<InventoryItem>> GetInventoryItemsByBranchAsync(int businessId, int branchId)
    {
        return await BuildInventoryQuery(businessId, branchId, null)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<PagedResultDto<InventoryItem>> GetInventoryItemsPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null,
        string? sortBy = null,
        string? sortDirection = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = BuildInventoryQuery(businessId, branchId, search);
        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
            page = totalPages;

        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var orderedQuery = (sortBy ?? "name").ToLowerInvariant() switch
        {
            "unit" => descending ? query.OrderByDescending(i => i.Unit) : query.OrderBy(i => i.Unit),
            "currentstock" or "stock" => descending ? query.OrderByDescending(i => i.CurrentStock) : query.OrderBy(i => i.CurrentStock),
            "minstocklevel" => descending ? query.OrderByDescending(i => i.MinStockLevel) : query.OrderBy(i => i.MinStockLevel),
            "purchaseprice" => descending ? query.OrderByDescending(i => i.PurchasePrice) : query.OrderBy(i => i.PurchasePrice),
            "producttype" => descending ? query.OrderByDescending(i => i.ProductType) : query.OrderBy(i => i.ProductType),
            _ => descending ? query.OrderByDescending(i => i.Name) : query.OrderBy(i => i.Name),
        };

        var data = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<InventoryItem>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page
        };
    }

    private IQueryable<InventoryItem> BuildInventoryQuery(int businessId, int branchId, string? search)
    {
        var query = _context.InventoryItems
            .Where(i => i.BusinessId == businessId &&
                        i.BranchId == branchId &&
                        i.IsInventoryItem &&
                        (i.ProductType == ProductType.RawMaterial || i.ProductType == ProductType.SemiFinished));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(i =>
                i.Name.ToLower().Contains(term) ||
                i.Unit.ToLower().Contains(term));
        }

        return query;
    }

    public async Task AddStockMovementAsync(StockMovement movement)
    {
        await _context.StockMovements.AddAsync(movement);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}