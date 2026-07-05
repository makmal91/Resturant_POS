using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.API.Authorization;
using POSSystem.API.Extensions;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Common.Helpers;
using POSSystem.Application.Reports.Interfaces;
using POSSystem.Application.Reports.DTOs;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly POSDbContext _db;
    private readonly IReportService _reportService;

    public ReportsController(POSDbContext db, IReportService reportService)
    {
        _db = db;
        _reportService = reportService;
    }

    // ─── Paginated Reports (server-side) ───────────────────────────────────────

    [HttpGet("sales")]
    [RequirePermission(PermissionModules.SalesReports, PermissionActions.View)]
    public async Task<IActionResult> GetSalesReport([FromQuery] ReportQueryParams q)
        => Ok(await _reportService.GetSalesReportAsync(BuildFilter(q)));

    [HttpGet("purchases")]
    [RequirePermission(PermissionModules.PurchaseReports, PermissionActions.View)]
    public async Task<IActionResult> GetPurchaseReport([FromQuery] ReportQueryParams q)
        => Ok(await _reportService.GetPurchaseReportAsync(BuildFilter(q)));

    [HttpGet("customer-outstanding")]
    [RequirePermission(PermissionModules.CustomerOutstandingReport, PermissionActions.View)]
    public async Task<IActionResult> GetCustomerOutstandingReport([FromQuery] ReportQueryParams q)
        => Ok(await _reportService.GetCustomerOutstandingReportAsync(BuildFilter(q)));

    [HttpGet("supplier-payable")]
    [RequirePermission(PermissionModules.SupplierPayableReport, PermissionActions.View)]
    public async Task<IActionResult> GetSupplierPayableReport([FromQuery] ReportQueryParams q)
        => Ok(await _reportService.GetSupplierPayableReportAsync(BuildFilter(q)));

    [HttpGet("profit-loss")]
    [RequirePermission(PermissionModules.ProfitLossReport, PermissionActions.View)]
    public async Task<IActionResult> GetProfitLossReport([FromQuery] ReportQueryParams q)
        => Ok(await _reportService.GetProfitLossReportAsync(BuildFilter(q)));

    [HttpGet("profit-loss-statement")]
    [RequirePermission(PermissionModules.ProfitLossReport, PermissionActions.View)]
    public async Task<IActionResult> GetProfitLossStatement([FromQuery] ReportQueryParams q)
        => Ok(await _reportService.GetProfitLossStatementAsync(BuildFilter(q)));

    [HttpGet("receivable-aging")]
    [RequirePermission(PermissionModules.CustomerReceivableAgingReport, PermissionActions.View)]
    public async Task<IActionResult> GetReceivableAgingReport([FromQuery] ReportQueryParams q)
        => Ok(await _reportService.GetReceivableAgingReportAsync(BuildFilter(q)));

    [HttpGet("payable-aging")]
    [RequirePermission(PermissionModules.SupplierPayableAgingReport, PermissionActions.View)]
    public async Task<IActionResult> GetPayableAgingReport([FromQuery] ReportQueryParams q)
        => Ok(await _reportService.GetPayableAgingReportAsync(BuildFilter(q)));

    [HttpGet("product-wise-sales")]
    [RequirePermission(PermissionModules.ProductWiseSalesReport, PermissionActions.View)]
    public async Task<IActionResult> GetProductWiseSalesReport([FromQuery] ReportQueryParams q)
        => Ok(await _reportService.GetProductWiseSalesReportAsync(BuildFilter(q)));

    private ReportFilterDto BuildFilter(ReportQueryParams q)
    {
        var pageNumber = q.PageNumber ?? q.Page ?? 1;
        return new ReportFilterDto
        {
            BusinessId = this.ResolveBusinessId(q.BusinessId),
            BranchId = this.ResolveBranchId(q.BranchId),
            PageNumber = pageNumber,
            PageSize = q.PageSize ?? 25,
            Search = q.Search,
            SortColumn = q.SortColumn ?? q.SortBy,
            SortDirection = q.SortDirection,
            FromDate = q.FromDate,
            ToDate = q.ToDate,
            CustomerId = q.CustomerId,
            SupplierId = q.SupplierId,
            ProductId = q.ProductId,
            CategoryId = q.CategoryId,
            SubCategoryId = q.SubCategoryId,
            BrandId = q.BrandId,
            AgingBucket = q.AgingBucket,
            GroupBy = q.GroupBy
        };
    }

    public class ReportQueryParams
    {
        public int? BranchId { get; set; }
        public int? BusinessId { get; set; }
        public int? PageNumber { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? Search { get; set; }
        public string? SortColumn { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? CustomerId { get; set; }
        public int? SupplierId { get; set; }
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public int? BrandId { get; set; }
        public string? AgingBucket { get; set; }
        public string? GroupBy { get; set; }
    }

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

    /// <summary>Aggregated closing stock balance per product (Opening + In − Out from ledger).</summary>
    [HttpGet("stock-summary")]
    [RequirePermission(PermissionModules.StockReports, PermissionActions.View)]
    public async Task<IActionResult> GetStockSummary(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int? warehouseId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null)
    {
        var biz    = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        var from = (fromDate ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
        var to   = (toDate   ?? DateTime.UtcNow).Date.AddDays(1);

        var ledgerQuery = _db.StockLedgerEntries
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.BusinessId == biz && !e.IsDeleted);

        if (branch > 0)
            ledgerQuery = ledgerQuery.Where(e => e.BranchId == branch);

        if (warehouseId.HasValue && warehouseId.Value > 0)
            ledgerQuery = ledgerQuery.Where(e => e.WarehouseId == warehouseId.Value);

        // Remaining balance = cumulative ledger sum through toDate (Opening + In − Out).
        // Materialize then group in memory — nested GroupBy aggregates are unreliable in EF Core.
        var ledgerRows = await ledgerQuery
            .Where(e => e.Date < to)
            .Select(e => new { e.ProductId, e.QuantityInBaseUnit })
            .ToListAsync();

        var balances = ledgerRows
            .GroupBy(e => e.ProductId)
            .Select(g => new
            {
                ProductId      = g.Key,
                ClosingBalance = g.Sum(e => e.QuantityInBaseUnit),
            })
            .Where(b => b.ClosingBalance != 0)
            .ToList();

        var productIds = balances.Select(b => b.ProductId).ToList();

        var productRows = productIds.Count > 0
            ? await _db.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.ProductName, p.EnableLowStockAlert, p.LowStockAlertLevel, p.BaseUnitId })
                .ToListAsync()
            : [];

        var productUnits = productIds.Count > 0
            ? await _db.ProductUnits.AsNoTracking()
                .Where(u => productIds.Contains(u.ProductId) && !u.IsDeleted)
                .Select(u => new { u.Id, u.ProductId, u.UnitName, u.ConversionFactor, u.IsBaseUnit })
                .ToListAsync()
            : [];

        var unitsByProduct = productUnits.GroupBy(u => u.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        var productMap = productRows.ToDictionary(p => p.Id);

        var items = balances.Select(b =>
        {
            productMap.TryGetValue(b.ProductId, out var product);
            unitsByProduct.TryGetValue(b.ProductId, out var units);
            var baseUnit = units?.FirstOrDefault(u => u.IsBaseUnit) ?? units?.FirstOrDefault();

            return new
            {
                productId            = b.ProductId,
                productName          = product?.ProductName ?? "Unknown",
                closingBalance       = b.ClosingBalance,
                baseUnitName         = baseUnit?.UnitName ?? string.Empty,
                baseUnitId           = product?.BaseUnitId ?? baseUnit?.Id,
                hasMultipleUnits     = (units?.Count ?? 0) > 1,
                enableLowStockAlert  = product?.EnableLowStockAlert ?? false,
                lowStockAlertLevel   = product?.LowStockAlertLevel,
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            items = items.Where(i => i.productName.ToLower().Contains(term)).ToList();
        }

        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        items = (sortBy ?? "productName").ToLowerInvariant() switch
        {
            "productid" or "id" => descending
                ? items.OrderByDescending(i => i.productId).ToList()
                : items.OrderBy(i => i.productId).ToList(),
            "closingbalance" or "balance" or "quantity" or "qty" => descending
                ? items.OrderByDescending(i => i.closingBalance).ToList()
                : items.OrderBy(i => i.closingBalance).ToList(),
            _ => descending
                ? items.OrderByDescending(i => i.productName).ToList()
                : items.OrderBy(i => i.productName).ToList(),
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
            fromDate     = from,
            toDate       = to.AddDays(-1),
            totalClosingBalance = items.Sum(i => i.closingBalance),
        });
    }

    /// <summary>On-demand unit-wise stock for one product (base stock × conversion factor per unit).</summary>
    [HttpGet("stock-summary/{productId:int}/unit-breakdown")]
    [RequirePermission(PermissionModules.StockReports, PermissionActions.View)]
    public async Task<IActionResult> GetStockUnitBreakdown(
        int productId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int? warehouseId,
        [FromQuery] DateTime? toDate)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);
        var to = (toDate ?? DateTime.UtcNow).Date.AddDays(1);

        var ledgerQuery = _db.StockLedgerEntries
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.BusinessId == biz && e.ProductId == productId && !e.IsDeleted && e.Date < to);

        if (branch > 0)
            ledgerQuery = ledgerQuery.Where(e => e.BranchId == branch);

        if (warehouseId.HasValue && warehouseId.Value > 0)
            ledgerQuery = ledgerQuery.Where(e => e.WarehouseId == warehouseId.Value);

        var closingBalance = await ledgerQuery.SumAsync(e => (decimal?)e.QuantityInBaseUnit) ?? 0m;

        var product = await _db.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.ProductName, p.BaseUnitId })
            .FirstOrDefaultAsync();

        if (product == null)
            return NotFound(new { message = "Product not found." });

        var units = await _db.ProductUnits.AsNoTracking()
            .Where(u => u.ProductId == productId && !u.IsDeleted)
            .Select(u => new { u.Id, u.UnitName, u.ConversionFactor, u.IsBaseUnit })
            .ToListAsync();

        var baseUnit = units.FirstOrDefault(u => u.IsBaseUnit) ?? units.FirstOrDefault();
        var unitInputs = units.Select(u => new StockUnitDisplayInput
        {
            UnitId = u.Id,
            UnitName = u.UnitName,
            ConversionFactor = u.ConversionFactor,
            IsBaseUnit = u.IsBaseUnit
        });

        var converted = UnitConversionHelper.ConvertStockToAllUnits(closingBalance, unitInputs);

        return Ok(new
        {
            productId,
            productName = product.ProductName,
            closingBalance,
            baseUnitName = baseUnit?.UnitName ?? string.Empty,
            units = converted.Select(u => new
            {
                unitId = u.UnitId,
                unitName = u.UnitName,
                quantity = u.Quantity,
                isBaseUnit = u.IsBaseUnit,
            }),
        });
    }

    /// <summary>
    /// Pivot-style stock report: one row per product, dynamic columns per unit (baseStock ÷ factor).
    /// </summary>
    [HttpGet("stock-by-unit-pivot")]
    [RequirePermission(PermissionModules.StockReports, PermissionActions.View)]
    public async Task<IActionResult> GetStockByUnitPivot(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int? warehouseId,
        [FromQuery] int? productId,
        [FromQuery] int? categoryId,
        [FromQuery] int? subCategoryId,
        [FromQuery] int? brandId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null)
    {
        var biz    = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        var from = (fromDate ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
        var to   = (toDate   ?? DateTime.UtcNow).Date.AddDays(1);

        var productQuery = _db.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.BusinessId == biz && !p.IsDeleted && p.Status);

        if (branch > 0)
            productQuery = productQuery.Where(p => p.BranchId == branch);

        if (productId.HasValue && productId.Value > 0)
            productQuery = productQuery.Where(p => p.Id == productId.Value);

        if (categoryId.HasValue && categoryId.Value > 0)
            productQuery = productQuery.Where(p => p.CategoryId == categoryId.Value);

        if (subCategoryId.HasValue && subCategoryId.Value > 0)
            productQuery = productQuery.Where(p => p.SubCategoryId == subCategoryId.Value);

        if (brandId.HasValue && brandId.Value > 0)
            productQuery = productQuery.Where(p => p.BrandId == brandId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            productQuery = productQuery.Where(p => p.ProductName.Contains(term));
        }

        var filteredProducts = await productQuery
            .OrderBy(p => p.ProductName)
            .Select(p => new { p.Id, p.ProductName })
            .ToListAsync();

        if (filteredProducts.Count == 0)
        {
            return Ok(new
            {
                columns = new[] { new { key = "productName", label = "Product Name" } },
                rows = Array.Empty<object>(),
                totalRecords = 0,
                totalPages = 0,
                currentPage = Math.Max(1, page),
                pageSize = Math.Clamp(pageSize, 1, 100),
                fromDate = from,
                toDate = to.AddDays(-1),
            });
        }

        var filteredProductIds = filteredProducts.Select(p => p.Id).ToList();

        var ledgerQuery = _db.StockLedgerEntries
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.BusinessId == biz
                && !e.IsDeleted
                && filteredProductIds.Contains(e.ProductId)
                && e.Date < to);

        if (branch > 0)
            ledgerQuery = ledgerQuery.Where(e => e.BranchId == branch);

        if (warehouseId.HasValue && warehouseId.Value > 0)
            ledgerQuery = ledgerQuery.Where(e => e.WarehouseId == warehouseId.Value);

        var ledgerRows = await ledgerQuery
            .Select(e => new { e.ProductId, e.QuantityInBaseUnit })
            .ToListAsync();

        var balanceByProduct = ledgerRows
            .GroupBy(e => e.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.QuantityInBaseUnit));

        var productUnits = await _db.ProductUnits
            .AsNoTracking()
            .Where(u => filteredProductIds.Contains(u.ProductId) && !u.IsDeleted)
            .Select(u => new { u.ProductId, u.UnitName, u.ConversionFactor, u.IsBaseUnit })
            .ToListAsync();

        var unitsByProduct = productUnits
            .GroupBy(u => u.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var unitColumns = productUnits
            .Select(u => u.UnitName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unitKeyByName = unitColumns.ToDictionary(
            n => n,
            n => $"unit_{SanitizeColumnKey(n)}",
            StringComparer.OrdinalIgnoreCase);

        var columns = new List<object>
        {
            new { key = "productName", label = "Product Name" },
        };
        columns.AddRange(unitColumns.Select(n => new
        {
            key = unitKeyByName[n],
            label = n,
        }));

        var rows = filteredProducts.Select(p =>
        {
            unitsByProduct.TryGetValue(p.Id, out var units);
            var baseStock = balanceByProduct.GetValueOrDefault(p.Id, 0m);

            var unitLookup = (units ?? [])
                .GroupBy(u => u.UnitName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var row = new Dictionary<string, object?>
            {
                ["productId"]   = p.Id,
                ["productName"] = p.ProductName,
                ["baseStock"]   = baseStock,
            };

            foreach (var unitName in unitColumns)
            {
                var colKey = unitKeyByName[unitName];
                if (unitLookup.TryGetValue(unitName, out var unit))
                {
                    // factor = base units contained in 1 of this unit, so express base
                    // stock in this unit by dividing (e.g. 9 PCS ÷ Pack factor 5 = 1.8 Pack).
                    var factor = unit.IsBaseUnit ? 1m : unit.ConversionFactor;
                    row[colKey] = factor > 0 ? baseStock / factor : (decimal?)null;
                }
                else
                {
                    row[colKey] = null;
                }
            }

            return row;
        }).ToList();

        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        rows = (sortBy ?? "productName").ToLowerInvariant() switch
        {
            "productid" or "id" => descending
                ? rows.OrderByDescending(r => (int)r["productId"]!).ToList()
                : rows.OrderBy(r => (int)r["productId"]!).ToList(),
            "basestock" or "closingbalance" or "balance" => descending
                ? rows.OrderByDescending(r => (decimal)r["baseStock"]!).ToList()
                : rows.OrderBy(r => (decimal)r["baseStock"]!).ToList(),
            _ when sortBy != null && unitKeyByName.Values.Contains(sortBy) => descending
                ? rows.OrderByDescending(r => r[sortBy] as decimal? ?? decimal.MinValue).ToList()
                : rows.OrderBy(r => r[sortBy] as decimal? ?? decimal.MinValue).ToList(),
            _ => descending
                ? rows.OrderByDescending(r => (string)r["productName"]!).ToList()
                : rows.OrderBy(r => (string)r["productName"]!).ToList(),
        };

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalRecords = rows.Count;
        var paged = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new
        {
            columns,
            rows = paged,
            totalRecords,
            totalPages = (int)Math.Ceiling(totalRecords / (double)Math.Max(pageSize, 1)),
            currentPage = page,
            pageSize,
            fromDate = from,
            toDate = to.AddDays(-1),
        });
    }

    private static string SanitizeColumnKey(string unitName)
    {
        var chars = unitName
            .Trim()
            .Select(c => char.IsLetterOrDigit(c) ? c : '_')
            .ToArray();
        var key = new string(chars).Trim('_');
        return string.IsNullOrEmpty(key) ? "unit" : key;
    }

    // ─── Legacy endpoints (Orders / InventoryItems) ────────────────────────────

    [HttpGet("orders-sales")]
    [RequirePermission(PermissionModules.Reports, PermissionActions.View)]
    public async Task<IActionResult> GetOrdersSalesReport(
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
