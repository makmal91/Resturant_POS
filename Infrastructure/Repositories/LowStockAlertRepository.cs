using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class LowStockAlertRepository : ILowStockAlertRepository
{
    private readonly POSDbContext _context;

    public LowStockAlertRepository(POSDbContext context) => _context = context;

    public async Task<Dictionary<int, (bool EnableLowStockAlert, decimal? LowStockAlertLevel)>> GetProductAlertSettingsAsync(
        int businessId, int branchId, IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, (bool, decimal?)>();

        var rows = await _context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id)
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted)
            .Select(p => new { p.Id, p.EnableLowStockAlert, p.LowStockAlertLevel })
            .ToListAsync();

        return rows.ToDictionary(
            p => p.Id,
            p => (p.EnableLowStockAlert, p.LowStockAlertLevel));
    }

    public async Task<List<LowStockAlert>> GetAlertsForProductsAsync(
        int businessId, int branchId, IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
            return new List<LowStockAlert>();

        return await _context.LowStockAlerts
            .IgnoreQueryFilters()
            .Where(a => a.BusinessId == businessId
                        && a.BranchId == branchId
                        && !a.IsDeleted
                        && ids.Contains(a.ProductId))
            .ToListAsync();
    }

    public async Task AddAsync(LowStockAlert alert) => await _context.LowStockAlerts.AddAsync(alert);

    public async Task<List<LowStockAlert>> GetActiveAlertsAsync(
        int businessId, int branchId, int? warehouseId = null)
    {
        var query = _context.LowStockAlerts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(a => a.Product)
            .Include(a => a.Variant)
            .Include(a => a.Warehouse)
            .Where(a => a.BusinessId == businessId
                        && a.BranchId == branchId
                        && a.IsActive
                        && !a.IsDeleted);

        if (warehouseId.HasValue)
            query = query.Where(a => a.WarehouseId == warehouseId.Value);

        return await query
            .OrderBy(a => a.CurrentStock)
            .ThenBy(a => a.Product.ProductName)
            .ToListAsync();
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
