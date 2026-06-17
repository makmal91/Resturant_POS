using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.API.Authorization;
using POSSystem.API.Extensions;
using POSSystem.Application.Common.Constants;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly POSDbContext _db;

    public ReportsController(POSDbContext db) => _db = db;

    // ─── Sales Report (SaleInvoices) ───────────────────────────────────────────

    /// <summary>Sales summary for a branch and date range (completed invoices).</summary>
    [HttpGet("sales-summary")]
    [RequirePermission(PermissionModules.Reports, PermissionActions.View)]
    public async Task<IActionResult> GetSalesSummary(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var biz    = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        var from = (fromDate ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
        var to   = (toDate   ?? DateTime.UtcNow).Date.AddDays(1);

        var query = _db.SaleInvoices
            .AsNoTracking()
            .Where(i => i.BusinessId == biz
                     && i.Status == SaleInvoiceStatus.Completed
                     && i.SaleDate >= from
                     && i.SaleDate < to);

        if (branch > 0)
            query = query.Where(i => i.BranchId == branch);

        var invoices = await query
            .Select(i => new
            {
                i.GrandTotal,
                i.DiscountAmount,
                i.TaxAmount,
                i.CashAmount,
                i.CardAmount,
                i.PaidAmount,
                i.SaleDate,
            })
            .ToListAsync();

        var branchName = branch > 0
            ? await _db.Branches.AsNoTracking().Where(b => b.Id == branch).Select(b => b.Name).FirstOrDefaultAsync() ?? "Unknown"
            : "All Branches";

        var dailyTrend = invoices
            .GroupBy(i => i.SaleDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date       = g.Key,
                invoiceCount = g.Count(),
                totalSales = g.Sum(x => x.GrandTotal),
                cashSales  = g.Sum(x => x.CashAmount),
                cardSales  = g.Sum(x => x.CardAmount),
            })
            .ToList();

        return Ok(new
        {
            branchId   = branch,
            branchName,
            fromDate   = from,
            toDate     = to.AddDays(-1),
            totalInvoices  = invoices.Count,
            totalSales     = invoices.Sum(i => i.GrandTotal),
            totalDiscount  = invoices.Sum(i => i.DiscountAmount),
            totalTax       = invoices.Sum(i => i.TaxAmount),
            totalCash      = invoices.Sum(i => i.CashAmount),
            totalCard      = invoices.Sum(i => i.CardAmount),
            totalPaid      = invoices.Sum(i => i.PaidAmount),
            averageSale    = invoices.Count > 0 ? invoices.Average(i => i.GrandTotal) : 0m,
            dailyTrend,
        });
    }

    /// <summary>Product-wise sales breakdown for a branch and date range.</summary>
    [HttpGet("sales-by-product")]
    [RequirePermission(PermissionModules.Reports, PermissionActions.View)]
    public async Task<IActionResult> GetSalesByProduct(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null)
    {
        var biz    = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        var from = (fromDate ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
        var to   = (toDate   ?? DateTime.UtcNow).Date.AddDays(1);

        var itemQuery = _db.SaleInvoiceItems
            .AsNoTracking()
            .Include(i => i.Product)
            .Include(i => i.SaleInvoice)
            .Where(i => i.BusinessId == biz
                     && i.SaleInvoice.Status == SaleInvoiceStatus.Completed
                     && i.SaleInvoice.SaleDate >= from
                     && i.SaleInvoice.SaleDate < to);

        if (branch > 0)
            itemQuery = itemQuery.Where(i => i.BranchId == branch);

        var grouped = itemQuery
            .GroupBy(i => new { i.ProductId, ProductName = i.Product.ProductName, ProductCode = i.Product.ProductCode })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.ProductCode,
                totalQuantity = g.Sum(x => x.Quantity),
                totalAmount   = g.Sum(x => x.LineTotal),
                invoiceCount  = g.Select(x => x.SaleInvoiceId).Distinct().Count(),
            });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            grouped = grouped.Where(g =>
                g.ProductName.ToLower().Contains(term) ||
                g.ProductCode.ToLower().Contains(term));
        }

        var totalRecords = await grouped.CountAsync();

        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var orderedGrouped = (sortBy ?? "totalAmount").ToLowerInvariant() switch
        {
            "productname" or "product" => descending ? grouped.OrderByDescending(g => g.ProductName) : grouped.OrderBy(g => g.ProductName),
            "productcode" or "code" => descending ? grouped.OrderByDescending(g => g.ProductCode) : grouped.OrderBy(g => g.ProductCode),
            "totalquantity" or "quantity" or "qty" => descending ? grouped.OrderByDescending(g => g.totalQuantity) : grouped.OrderBy(g => g.totalQuantity),
            "invoicecount" or "invoices" => descending ? grouped.OrderByDescending(g => g.invoiceCount) : grouped.OrderBy(g => g.invoiceCount),
            _ => descending ? grouped.OrderByDescending(g => g.totalAmount) : grouped.OrderBy(g => g.totalAmount),
        };

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var rows = await orderedGrouped
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            products     = rows,
            totalRecords,
            totalPages   = (int)Math.Ceiling(totalRecords / (double)pageSize),
            currentPage  = page,
            pageSize,
            fromDate     = from,
            toDate       = to.AddDays(-1),
        });
    }

    // ─── Stock Report (StockLedger) ────────────────────────────────────────────

    /// <summary>Current stock balances with estimated stock value.</summary>
    [HttpGet("stock-summary")]
    [RequirePermission(PermissionModules.Reports, PermissionActions.View)]
    public async Task<IActionResult> GetStockSummary(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int? warehouseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null)
    {
        var biz    = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        var ledgerQuery = _db.StockLedgerEntries
            .AsNoTracking()
            .Where(e => e.BusinessId == biz);

        if (branch > 0)
            ledgerQuery = ledgerQuery.Where(e => e.BranchId == branch);

        if (warehouseId.HasValue && warehouseId.Value > 0)
            ledgerQuery = ledgerQuery.Where(e => e.WarehouseId == warehouseId.Value);

        var balances = await ledgerQuery
            .GroupBy(e => new { e.ProductId, e.VariantId, e.WarehouseId })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.VariantId,
                g.Key.WarehouseId,
                quantity = g.Sum(e => e.QuantityInBaseUnit),
            })
            .Where(b => b.quantity != 0)
            .ToListAsync();

        var productIds   = balances.Select(b => b.ProductId).Distinct().ToList();
        var variantIds   = balances.Where(b => b.VariantId.HasValue).Select(b => b.VariantId!.Value).Distinct().ToList();
        var warehouseIds = balances.Select(b => b.WarehouseId).Distinct().ToList();

        var products = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.ProductName, p.ProductCode, p.CostPrice })
            .ToDictionaryAsync(p => p.Id);

        var variantMap = new Dictionary<int, (string VariantName, decimal? CostPriceOverride)>();
        if (variantIds.Count > 0)
        {
            var variantList = await _db.ProductVariants.AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .Select(v => new { v.Id, v.VariantName, v.CostPriceOverride })
                .ToListAsync();
            foreach (var v in variantList)
                variantMap[v.Id] = (v.VariantName, v.CostPriceOverride);
        }

        var warehouses = await _db.Warehouses.AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id))
            .Select(w => new { w.Id, w.Name })
            .ToDictionaryAsync(w => w.Id);

        var items = balances.Select(b =>
        {
            products.TryGetValue(b.ProductId, out var product);
            variantMap.TryGetValue(b.VariantId ?? 0, out var variant);
            warehouses.TryGetValue(b.WarehouseId, out var wh);

            var costPrice = variant.CostPriceOverride ?? product?.CostPrice ?? 0m;
            var stockValue = b.quantity * costPrice;

            return new
            {
                productId     = b.ProductId,
                productName   = product?.ProductName ?? "Unknown",
                productCode   = product?.ProductCode ?? "",
                variantId     = b.VariantId,
                variantName   = variant.VariantName != null ? variant.VariantName : null,
                warehouseId   = b.WarehouseId,
                warehouseName = wh?.Name ?? "Unknown",
                quantity      = b.quantity,
                costPrice,
                stockValue,
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            items = items.Where(i =>
                i.productName.ToLower().Contains(term) ||
                i.productCode.ToLower().Contains(term) ||
                (i.variantName ?? string.Empty).ToLower().Contains(term) ||
                i.warehouseName.ToLower().Contains(term)).ToList();
        }

        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        items = (sortBy ?? "productName").ToLowerInvariant() switch
        {
            "productcode" or "code" => descending
                ? items.OrderByDescending(i => i.productCode).ThenByDescending(i => i.productName).ToList()
                : items.OrderBy(i => i.productCode).ThenBy(i => i.productName).ToList(),
            "variantname" or "variant" => descending
                ? items.OrderByDescending(i => i.variantName).ThenByDescending(i => i.productName).ToList()
                : items.OrderBy(i => i.variantName).ThenBy(i => i.productName).ToList(),
            "warehousename" or "warehouse" => descending
                ? items.OrderByDescending(i => i.warehouseName).ThenByDescending(i => i.productName).ToList()
                : items.OrderBy(i => i.warehouseName).ThenBy(i => i.productName).ToList(),
            "quantity" or "qty" => descending
                ? items.OrderByDescending(i => i.quantity).ToList()
                : items.OrderBy(i => i.quantity).ToList(),
            "costprice" or "cost" => descending
                ? items.OrderByDescending(i => i.costPrice).ToList()
                : items.OrderBy(i => i.costPrice).ToList(),
            "stockvalue" or "value" => descending
                ? items.OrderByDescending(i => i.stockValue).ToList()
                : items.OrderBy(i => i.stockValue).ToList(),
            _ => descending
                ? items.OrderByDescending(i => i.productName).ThenByDescending(i => i.variantName).ToList()
                : items.OrderBy(i => i.productName).ThenBy(i => i.variantName).ToList(),
        };

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalRecords = items.Count;
        var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new
        {
            items        = paged,
            totalRecords,
            totalPages   = (int)Math.Ceiling(totalRecords / (double)Math.Max(pageSize, 1)),
            currentPage  = page,
            pageSize,
            totalQuantity = items.Sum(i => i.quantity),
            totalStockValue = items.Sum(i => i.stockValue),
            lowStockCount = items.Count(i => i.quantity > 0 && i.quantity <= 5),
        });
    }

    /// <summary>Stock movement summary grouped by ledger type for a date range.</summary>
    [HttpGet("stock-movement")]
    [RequirePermission(PermissionModules.Reports, PermissionActions.View)]
    public async Task<IActionResult> GetStockMovement(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int? warehouseId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var biz    = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        var from = (fromDate ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
        var to   = (toDate   ?? DateTime.UtcNow).Date.AddDays(1);

        var query = _db.StockLedgerEntries
            .AsNoTracking()
            .Where(e => e.BusinessId == biz
                     && e.Date >= from
                     && e.Date < to);

        if (branch > 0)
            query = query.Where(e => e.BranchId == branch);

        if (warehouseId.HasValue && warehouseId.Value > 0)
            query = query.Where(e => e.WarehouseId == warehouseId.Value);

        // Project to memory first — enum.ToString() and nested GroupBy aggregates
        // are not reliably translatable to SQL Server.
        var entries = await query
            .Select(e => new { e.Type, e.QuantityInBaseUnit, e.TotalAmount, e.Date })
            .ToListAsync();

        var byType = entries
            .GroupBy(e => e.Type)
            .Select(g => new
            {
                type          = g.Key.ToString(),
                entryCount    = g.Count(),
                totalQuantity = g.Sum(e => e.QuantityInBaseUnit),
                totalIn       = g.Where(e => e.QuantityInBaseUnit > 0).Sum(e => e.QuantityInBaseUnit),
                totalOut      = g.Where(e => e.QuantityInBaseUnit < 0).Sum(e => Math.Abs(e.QuantityInBaseUnit)),
                totalAmount   = g.Sum(e => e.TotalAmount),
            })
            .OrderBy(g => g.type)
            .ToList();

        var dailyMovement = entries
            .GroupBy(e => e.Date.Date)
            .Select(g => new
            {
                date     = g.Key,
                stockIn  = g.Where(e => e.QuantityInBaseUnit > 0).Sum(e => e.QuantityInBaseUnit),
                stockOut = g.Where(e => e.QuantityInBaseUnit < 0).Sum(e => Math.Abs(e.QuantityInBaseUnit)),
                netQty   = g.Sum(e => e.QuantityInBaseUnit),
            })
            .OrderBy(g => g.date)
            .ToList();

        return Ok(new
        {
            fromDate      = from,
            toDate        = to.AddDays(-1),
            totalEntries  = byType.Sum(t => t.entryCount),
            totalStockIn  = byType.Sum(t => t.totalIn),
            totalStockOut = byType.Sum(t => t.totalOut),
            byType,
            dailyMovement,
        });
    }

    // ─── Legacy endpoints (Orders / InventoryItems) ────────────────────────────

    [HttpGet("sales")]
    [RequirePermission(PermissionModules.Reports, PermissionActions.View)]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] int branchId,
        [FromQuery] int? businessId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var fromDate = from ?? DateTime.UtcNow.Date;
        var toDate = to ?? DateTime.UtcNow;

        var orderItems = await _db.OrderItems
            .Where(oi => oi.BusinessId == resolvedBusinessId && (resolvedBranchId == 0 || oi.BranchId == resolvedBranchId))
            .Include(oi => oi.Order)
            .Include(oi => oi.MenuItem)
            .Where(oi => oi.Order.Status == OrderStatus.Completed &&
                         oi.Order.CreatedAt >= fromDate &&
                         oi.Order.CreatedAt <= toDate &&
                         oi.MenuItem.ProductType == ProductType.FinishedGood)
            .ToListAsync();

        var menuItemIds = orderItems.Select(i => i.MenuItemId).Distinct().ToList();
        var recipes = await _db.Recipes
            .Where(r => r.BusinessId == resolvedBusinessId && (resolvedBranchId == 0 || r.BranchId == resolvedBranchId) && menuItemIds.Contains(r.MenuItemId))
            .Include(r => r.Ingredient)
            .ToListAsync();

        decimal revenue = 0;
        decimal recipeCost = 0;

        foreach (var item in orderItems)
        {
            revenue += item.Total;

            var itemRecipes = recipes.Where(r => r.MenuItemId == item.MenuItemId);
            foreach (var recipe in itemRecipes)
            {
                recipeCost += recipe.QuantityRequired * recipe.Ingredient.PurchasePrice * item.Quantity;
            }
        }

        return Ok(new
        {
            from = fromDate,
            to = toDate,
            totalItems = orderItems.Sum(i => i.Quantity),
            revenue,
            recipeCost,
            grossProfit = revenue - recipeCost
        });
    }

    [HttpGet("inventory")]
    [RequirePermission(PermissionModules.Reports, PermissionActions.View)]
    public async Task<IActionResult> GetInventoryReport(
        [FromQuery] int branchId,
        [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var items = await _db.InventoryItems
            .Where(i => i.BusinessId == resolvedBusinessId &&
                        (resolvedBranchId == 0 || i.BranchId == resolvedBranchId) &&
                        (i.ProductType == ProductType.RawMaterial || i.ProductType == ProductType.SemiFinished))
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.ProductType,
                i.Unit,
                i.CurrentStock,
                i.MinStockLevel,
                i.PurchasePrice,
                stockValue = i.CurrentStock * i.PurchasePrice
            })
            .ToListAsync();

        return Ok(new
        {
            items,
            totalStockValue = items.Sum(i => i.stockValue)
        });
    }

    [HttpGet("sales-by-business")]
    [RequirePermission(PermissionModules.Reports, PermissionActions.View)]
    public async Task<IActionResult> GetBusinessSalesReport(
        [FromQuery] int? businessId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var fromDate = (from ?? DateTime.UtcNow.Date).Date;
        var toDate   = (to   ?? DateTime.UtcNow).Date.AddDays(1);

        var branchSales = await _db.SaleInvoices
            .AsNoTracking()
            .Where(i => i.BusinessId == resolvedBusinessId
                     && i.Status == SaleInvoiceStatus.Completed
                     && i.SaleDate >= fromDate
                     && i.SaleDate < toDate)
            .GroupBy(i => i.BranchId)
            .Select(g => new
            {
                branchId    = g.Key,
                totalInvoices = g.Count(),
                totalSales  = g.Sum(x => x.GrandTotal),
                totalCash   = g.Sum(x => x.CashAmount),
                totalCard   = g.Sum(x => x.CardAmount),
            })
            .ToListAsync();

        var branchIds = branchSales.Select(b => b.branchId).ToList();
        var branchNames = await _db.Branches.AsNoTracking()
            .Where(b => branchIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Name);

        var branches = branchSales.Select(b => new
        {
            b.branchId,
            branchName  = branchNames.GetValueOrDefault(b.branchId, "Unknown"),
            b.totalInvoices,
            b.totalSales,
            b.totalCash,
            b.totalCard,
        }).ToList();

        return Ok(new
        {
            businessId = resolvedBusinessId,
            fromDate,
            toDate     = toDate.AddDays(-1),
            totalSales = branches.Sum(x => x.totalSales),
            totalInvoices = branches.Sum(x => x.totalInvoices),
            branches,
        });
    }
}
