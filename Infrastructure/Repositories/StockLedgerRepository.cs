using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Stock.DTOs;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class StockLedgerRepository : IStockLedgerRepository
{
    private const int MaxPageSize = 200;
    private readonly POSDbContext _context;

    public StockLedgerRepository(POSDbContext context) => _context = context;

    public async Task AddAsync(StockLedger entry) => await _context.StockLedgerEntries.AddAsync(entry);

    public async Task AddRangeAsync(IEnumerable<StockLedger> entries) =>
        await _context.StockLedgerEntries.AddRangeAsync(entries);

    public async Task<PagedResultDto<StockLedger>> GetPagedAsync(StockLedgerFilterDto filter)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

        var query = _context.StockLedgerEntries
            .IgnoreQueryFilters()
            .Where(e => e.BusinessId == filter.BusinessId);

        if (filter.BranchId > 0) query = query.Where(e => e.BranchId == filter.BranchId);
        if (filter.WarehouseId.HasValue) query = query.Where(e => e.WarehouseId == filter.WarehouseId.Value);
        if (filter.ProductId.HasValue) query = query.Where(e => e.ProductId == filter.ProductId.Value);
        if (filter.VariantId.HasValue) query = query.Where(e => e.VariantId == filter.VariantId.Value);
        if (filter.Type.HasValue) query = query.Where(e => e.Type == filter.Type.Value);
        if (filter.DateFrom.HasValue) query = query.Where(e => e.Date >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue) query = query.Where(e => e.Date <= filter.DateTo.Value);

        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);
        if (totalPages > 0 && page > totalPages) page = totalPages;

        var data = await query
            .Include(e => e.Product)
            .Include(e => e.Variant)
            .Include(e => e.Warehouse)
            .Include(e => e.Branch)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<StockLedger> { Data = data, TotalRecords = totalRecords, TotalPages = totalPages, CurrentPage = page };
    }

    public async Task<List<StockBalanceDto>> GetStockBalancesAsync(
        int businessId, int branchId, int? warehouseId = null, int? productId = null, int? variantId = null, bool variantWise = false)
    {
        var query = _context.StockLedgerEntries
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.BusinessId == businessId && !e.IsDeleted);

        if (branchId > 0) query = query.Where(e => e.BranchId == branchId);
        if (warehouseId.HasValue) query = query.Where(e => e.WarehouseId == warehouseId.Value);
        if (productId.HasValue) query = query.Where(e => e.ProductId == productId.Value);
        if (variantId.HasValue) query = query.Where(e => e.VariantId == variantId.Value);

        if (variantWise)
        {
            var ledgerRows = await query
                .Select(e => new { e.ProductId, e.VariantId, e.WarehouseId, e.QuantityInBaseUnit })
                .ToListAsync();

            var balances = ledgerRows
                .GroupBy(e => new { e.ProductId, e.VariantId })
                .Select(g => new BalanceGroupRow
                {
                    ProductId   = g.Key.ProductId,
                    VariantId   = g.Key.VariantId,
                    Quantity    = g.Sum(e => e.QuantityInBaseUnit),
                    WarehouseId = warehouseId ?? g.Select(x => x.WarehouseId).FirstOrDefault(),
                })
                .ToList();

            return await MapBalanceDtosAsync(balances, variantWise: true, warehouseId);
        }

        var productRows = await query
            .Select(e => new { e.ProductId, e.WarehouseId, e.QuantityInBaseUnit })
            .ToListAsync();

        var productBalances = productRows
            .GroupBy(e => e.ProductId)
            .Select(g => new BalanceGroupRow
            {
                ProductId   = g.Key,
                VariantId   = null,
                Quantity    = g.Sum(e => e.QuantityInBaseUnit),
                WarehouseId = warehouseId ?? g.Select(x => x.WarehouseId).FirstOrDefault(),
            })
            .ToList();

        return await MapBalanceDtosAsync(productBalances, variantWise: false, warehouseId);
    }

    private sealed class BalanceGroupRow
    {
        public int ProductId { get; init; }
        public int? VariantId { get; init; }
        public decimal Quantity { get; init; }
        public int WarehouseId { get; init; }
    }

    private async Task<List<StockBalanceDto>> MapBalanceDtosAsync(
        IEnumerable<BalanceGroupRow> balances, bool variantWise, int? warehouseId)
    {
        var balanceList = balances.ToList();
        var productIds = balanceList.Select(b => b.ProductId).Distinct().ToList();
        var variantIds = variantWise
            ? balanceList.Where(b => b.VariantId.HasValue).Select(b => b.VariantId!.Value).Distinct().ToList()
            : new List<int>();
        var warehouseIds = warehouseId.HasValue
            ? new List<int> { warehouseId.Value }
            : balanceList.Select(b => b.WarehouseId).Where(id => id > 0).Distinct().ToList();

        var products = await _context.Products
            .IgnoreQueryFilters()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.ProductName, p.ProductCode })
            .ToListAsync();

        var variantMap = new Dictionary<int, string>();
        if (variantIds.Count > 0)
        {
            var variantList = await _context.ProductVariants
                .IgnoreQueryFilters()
                .Where(v => variantIds.Contains(v.Id))
                .Select(v => new { v.Id, v.VariantName })
                .ToListAsync();
            foreach (var v in variantList)
                variantMap[v.Id] = v.VariantName;
        }

        var warehouseMap = new Dictionary<int, string>();
        if (warehouseIds.Count > 0)
        {
            var warehouseList = await _context.Warehouses
                .IgnoreQueryFilters()
                .Where(w => warehouseIds.Contains(w.Id))
                .Select(w => new { w.Id, w.Name })
                .ToListAsync();
            foreach (var wh in warehouseList)
                warehouseMap[wh.Id] = wh.Name;
        }

        var productMap = products.ToDictionary(p => p.Id, p => (p.ProductName, p.ProductCode));

        return balanceList.Select(b =>
        {
            var whId = warehouseId ?? b.WarehouseId;
            return new StockBalanceDto
            {
                ProductId     = b.ProductId,
                ProductName   = productMap.TryGetValue(b.ProductId, out var p) ? p.ProductName : string.Empty,
                ProductCode   = productMap.TryGetValue(b.ProductId, out var pc) ? pc.ProductCode : string.Empty,
                VariantId     = variantWise ? b.VariantId : null,
                VariantName   = variantWise && b.VariantId.HasValue && variantMap.TryGetValue(b.VariantId.Value, out var vn) ? vn : null,
                WarehouseId   = whId,
                WarehouseName = warehouseMap.TryGetValue(whId, out var w) ? w : string.Empty,
                Quantity      = b.Quantity,
            };
        }).OrderBy(b => b.ProductName).ThenBy(b => b.VariantName).ToList();
    }

    public async Task<decimal> GetCurrentStockAsync(
        int businessId, int branchId, int productId, int? variantId, int warehouseId)
    {
        var query = _context.StockLedgerEntries
            .IgnoreQueryFilters()
            .Where(e => e.BusinessId == businessId && e.ProductId == productId && e.WarehouseId == warehouseId);

        if (branchId > 0) query = query.Where(e => e.BranchId == branchId);

        if (variantId.HasValue)
            query = query.Where(e => e.VariantId == variantId.Value);

        return await query.SumAsync(e => (decimal?)e.QuantityInBaseUnit) ?? 0m;
    }

    public async Task<Dictionary<string, decimal>> GetStockForProductsAsync(
        int businessId, int branchId, IEnumerable<int> productIds, int? warehouseId)
    {
        var ids = productIds.ToList();
        if (ids.Count == 0) return new Dictionary<string, decimal>();

        var query = _context.StockLedgerEntries
            .IgnoreQueryFilters()
            .Where(e => e.BusinessId == businessId && e.BranchId == branchId && ids.Contains(e.ProductId));

        if (warehouseId.HasValue)
            query = query.Where(e => e.WarehouseId == warehouseId.Value);

        var rows = await query
            .GroupBy(e => new { e.ProductId, e.VariantId })
            .Select(g => new { g.Key.ProductId, g.Key.VariantId, Total = g.Sum(e => e.QuantityInBaseUnit) })
            .ToListAsync();

        return rows.ToDictionary(
            r => $"{r.ProductId}:{r.VariantId ?? 0}",
            r => r.Total);
    }

    public async Task<List<StockLedger>> GetByReferenceAsync(
        int referenceId, int businessId, int branchId, params StockLedgerType[] types)
    {
        var query = _context.StockLedgerEntries
            .IgnoreQueryFilters()
            .Where(e => e.ReferenceId == referenceId
                        && e.BusinessId == businessId
                        && e.BranchId == branchId);

        if (types.Length > 0)
            query = query.Where(e => types.Contains(e.Type));

        return await query.ToListAsync();
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
