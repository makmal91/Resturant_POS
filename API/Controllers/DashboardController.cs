using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.API.Authorization;
using POSSystem.API.Extensions;
using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Dashboard.DTOs;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private const int TrendDays = 30;
    private const int RecentLimit = 10;

    private readonly POSDbContext _db;
    private readonly IPermissionService _permissionService;

    public DashboardController(POSDbContext db, IPermissionService permissionService)
    {
        _db = db;
        _permissionService = permissionService;
    }

    /// <summary>Personal sales summary for the logged-in user only (cashier / sales person).</summary>
    [HttpGet("my-sales-summary")]
    public async Task<IActionResult> GetMySalesSummary([FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var roleName = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var roleId   = int.TryParse(User.FindFirstValue("roleId"), out var rid) ? rid : 0;

        if (!RoleNames.CanBypassPermissions(roleName))
        {
            var hasPos   = await _permissionService.HasPermissionAsync(roleId, roleName, PermissionModules.PosBilling, PermissionActions.View);
            var hasSales = await _permissionService.HasPermissionAsync(roleId, roleName, PermissionModules.Sales, PermissionActions.View);
            if (!hasPos && !hasSales)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to view sales summary." });
        }

        var userId = ResolveCurrentUserId();
        if (userId <= 0)
            return Unauthorized(new { message = "User identity is missing from the token." });

        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new { u.Id, u.FullName, u.Username })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { message = "User not found." });

        var biz    = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        if (branch <= 0 && !RoleNames.HasGlobalBranchAccess(roleName))
        {
            branch = int.TryParse(User.FindFirstValue("branchId"), out var claimBranch) && claimBranch > 0
                ? claimBranch
                : this.ResolveBranchId(null);
        }

        if (branch <= 0)
            return BadRequest(new { message = "Please select a branch to view your sales summary." });

        var branchName = await _db.Branches.AsNoTracking()
            .Where(b => b.Id == branch)
            .Select(b => b.Name)
            .FirstOrDefaultAsync() ?? "Unknown";

        var now        = DateTime.UtcNow;
        var todayStart = now.Date;
        var todayEnd   = todayStart.AddDays(1);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var trendStart = todayStart.AddDays(-TrendDays);

        var mySalesQuery = ApplyUserSalesFilter(
            _db.SaleInvoices.AsNoTracking()
                .Where(i => i.BusinessId == biz
                         && i.BranchId == branch
                         && i.Status == SaleInvoiceStatus.Completed),
            userId,
            user.FullName,
            user.Username);

        var todayQuery  = mySalesQuery.Where(i => i.SaleDate >= todayStart && i.SaleDate < todayEnd);
        var monthQuery  = mySalesQuery.Where(i => i.SaleDate >= monthStart && i.SaleDate < todayEnd);
        var trendQuery  = mySalesQuery.Where(i => i.SaleDate >= trendStart && i.SaleDate < todayEnd);

        var todayAgg = await todayQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total     = g.Sum(x => x.GrandTotal),
                Count     = g.Count(),
                Cash      = g.Sum(x => x.CashAmount),
                Card      = g.Sum(x => x.CardAmount),
            })
            .FirstOrDefaultAsync();

        var monthInvoices = await monthQuery
            .Select(i => new
            {
                i.GrandTotal,
                i.PaidAmount,
                i.PaymentMethod,
                i.CashAmount,
                i.CardAmount,
            })
            .ToListAsync();

        var monthlySales    = monthInvoices.Sum(i => i.GrandTotal);
        var monthlyCount    = monthInvoices.Count;
        var pendingCount    = monthInvoices.Count(i => i.PaidAmount < i.GrandTotal);
        var paidCount       = monthInvoices.Count(i => i.PaidAmount >= i.GrandTotal);
        var averageSale     = monthlyCount > 0 ? monthlySales / monthlyCount : 0m;

        var payment = new SalesPersonPaymentDto
        {
            TotalCash   = monthInvoices.Where(i => i.PaymentMethod == SalePaymentMethod.Cash).Sum(i => i.GrandTotal),
            TotalCard   = monthInvoices.Where(i => i.PaymentMethod == SalePaymentMethod.Card).Sum(i => i.GrandTotal),
            TotalMixed  = monthInvoices.Where(i => i.PaymentMethod == SalePaymentMethod.Mixed).Sum(i => i.GrandTotal),
            CashInvoices  = monthInvoices.Count(i => i.PaymentMethod == SalePaymentMethod.Cash),
            CardInvoices  = monthInvoices.Count(i => i.PaymentMethod == SalePaymentMethod.Card),
            MixedInvoices = monthInvoices.Count(i => i.PaymentMethod == SalePaymentMethod.Mixed),
        };

        var recentSales = await mySalesQuery
            .OrderByDescending(i => i.SaleDate)
            .Take(RecentLimit)
            .Select(i => new RecentSaleDto
            {
                Id            = i.Id,
                InvoiceNo     = i.InvoiceNo,
                BranchName    = branchName,
                CashierName   = i.CashierName ?? user.FullName,
                GrandTotal    = i.GrandTotal,
                PaidAmount    = i.PaidAmount,
                PaymentStatus = i.PaidAmount >= i.GrandTotal ? "Paid" : "Pending",
                Status        = i.Status.ToString(),
                SaleDate      = i.SaleDate,
            })
            .ToListAsync();

        var trendInvoices = await trendQuery
            .Select(i => new { i.SaleDate, i.GrandTotal })
            .ToListAsync();

        var salesTrend = trendInvoices
            .GroupBy(i => i.SaleDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new SalesTrendPointDto
            {
                Date         = g.Key,
                TotalSales   = g.Sum(x => x.GrandTotal),
                InvoiceCount = g.Count(),
            })
            .ToList();

        var myItemQuery = ApplyUserSalesFilter(
            _db.SaleInvoiceItems.AsNoTracking()
                .Where(i => i.BusinessId == biz
                         && i.BranchId == branch
                         && i.SaleInvoice.Status == SaleInvoiceStatus.Completed
                         && i.SaleInvoice.SaleDate >= monthStart
                         && i.SaleInvoice.SaleDate < todayEnd),
            userId,
            user.FullName,
            user.Username);

        var topProducts = await myItemQuery
            .GroupBy(i => new { i.ProductId, i.Product.ProductName, i.Product.ProductCode })
            .Select(g => new TopProductDto
            {
                ProductId     = g.Key.ProductId,
                ProductName   = g.Key.ProductName,
                ProductCode   = g.Key.ProductCode,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalAmount   = g.Sum(x => x.LineTotal),
            })
            .OrderByDescending(p => p.TotalAmount)
            .Take(10)
            .ToListAsync();

        return Ok(new SalesPersonSummaryDto
        {
            UserId      = user.Id,
            FullName    = user.FullName,
            Username    = user.Username,
            BranchId    = branch,
            BranchName  = branchName,
            GeneratedAt = now,
            Kpis = new SalesPersonKpiDto
            {
                TodaySales          = todayAgg?.Total ?? 0m,
                TodayInvoices       = todayAgg?.Count ?? 0,
                MonthlySales        = monthlySales,
                MonthlyInvoices     = monthlyCount,
                AverageSale         = averageSale,
                TodayCash           = todayAgg?.Cash ?? 0m,
                TodayCard           = todayAgg?.Card ?? 0m,
                PendingPaymentCount = pendingCount,
                PaidCount           = paidCount,
            },
            Payment     = payment,
            RecentSales = recentSales,
            SalesTrend  = salesTrend,
            TopProducts = topProducts,
        });
    }

    private int ResolveCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue("userId")
                 ?? User.FindFirstValue("UserId")
                 ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(claim, out var userId) && userId > 0 ? userId : 0;
    }

    private static IQueryable<SaleInvoice> ApplyUserSalesFilter(
        IQueryable<SaleInvoice> query,
        int userId,
        string fullName,
        string username)
    {
        return query.Where(i =>
            i.CreatedBy == userId
            || (i.CashierName != null && (i.CashierName == fullName || i.CashierName == username)));
    }

    private static IQueryable<SaleInvoiceItem> ApplyUserSalesFilter(
        IQueryable<SaleInvoiceItem> query,
        int userId,
        string fullName,
        string username)
    {
        return query.Where(i =>
            i.SaleInvoice.CreatedBy == userId
            || (i.SaleInvoice.CashierName != null
                && (i.SaleInvoice.CashierName == fullName || i.SaleInvoice.CashierName == username)));
    }

    /// <summary>Central control panel — aggregated KPIs, analytics, stock, financials, and charts.</summary>
    [HttpGet("overview")]
    [RequirePermission(PermissionModules.Dashboard, PermissionActions.View)]
    public async Task<IActionResult> GetOverview([FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var biz    = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        var now        = DateTime.UtcNow;
        var todayStart = now.Date;
        var todayEnd   = todayStart.AddDays(1);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var trendStart = todayStart.AddDays(-TrendDays);

        var branchName = branch > 0
            ? await _db.Branches.AsNoTracking()
                .Where(b => b.Id == branch)
                .Select(b => b.Name)
                .FirstOrDefaultAsync() ?? "Unknown"
            : "All Branches";

        // ─── Base queries ───────────────────────────────────────────────────────

        var salesQuery = _db.SaleInvoices.AsNoTracking()
            .Where(i => i.BusinessId == biz && i.Status == SaleInvoiceStatus.Completed);

        if (branch > 0)
            salesQuery = salesQuery.Where(i => i.BranchId == branch);

        var monthSalesQuery = salesQuery.Where(i => i.SaleDate >= monthStart && i.SaleDate < todayEnd);
        var todaySalesQuery = salesQuery.Where(i => i.SaleDate >= todayStart && i.SaleDate < todayEnd);
        var trendSalesQuery = salesQuery.Where(i => i.SaleDate >= trendStart && i.SaleDate < todayEnd);

        var purchaseQuery = _db.Purchases.AsNoTracking()
            .Where(p => p.BusinessId == biz && p.Status == PurchaseStatus.Posted);

        if (branch > 0)
            purchaseQuery = purchaseQuery.Where(p => p.BranchId == branch);

        var monthPurchaseQuery = purchaseQuery.Where(p => p.PurchaseDate >= monthStart && p.PurchaseDate < todayEnd);

        var expenseQuery = _db.Expenses.AsNoTracking()
            .Where(e => e.BusinessId == biz && !e.IsDeleted);

        if (branch > 0)
            expenseQuery = expenseQuery.Where(e => e.BranchId == branch);

        var monthExpenseQuery = expenseQuery.Where(e => e.ExpenseDate >= monthStart && e.ExpenseDate < todayEnd);

        // ─── KPI counts ─────────────────────────────────────────────────────────

        var branchCountQuery = _db.Branches.AsNoTracking()
            .Where(b => b.BusinessId == biz && !b.IsDeleted);
        if (branch > 0)
            branchCountQuery = branchCountQuery.Where(b => b.Id == branch);

        var userCountQuery = _db.Users.AsNoTracking()
            .Where(u => u.BusinessId == biz && !u.IsDeleted && u.DeletedAt == null);
        if (branch > 0)
        {
            userCountQuery = userCountQuery.Where(u =>
                u.UserBranches.Any(ub => ub.BranchId == branch));
        }

        var totalBranches = await branchCountQuery.CountAsync();
        var totalUsers    = await userCountQuery.CountAsync();

        var todaySalesAgg = await todaySalesQuery
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Sum(x => x.GrandTotal), Count = g.Count() })
            .FirstOrDefaultAsync();

        var monthSalesAgg = await monthSalesQuery
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Sum(x => x.GrandTotal), Count = g.Count() })
            .FirstOrDefaultAsync();

        // ─── Profit (monthly) ─────────────────────────────────────────────────────

        var monthItemQuery = _db.SaleInvoiceItems.AsNoTracking()
            .Include(i => i.Product)
            .Include(i => i.Variant)
            .Include(i => i.SaleInvoice)
            .Where(i => i.BusinessId == biz
                     && i.SaleInvoice.Status == SaleInvoiceStatus.Completed
                     && i.SaleInvoice.SaleDate >= monthStart
                     && i.SaleInvoice.SaleDate < todayEnd);

        if (branch > 0)
            monthItemQuery = monthItemQuery.Where(i => i.BranchId == branch);

        var monthItems = await monthItemQuery
            .Select(i => new
            {
                i.LineTotal,
                i.BaseQuantity,
                CostPrice = i.Variant != null && i.Variant.CostPriceOverride.HasValue
                    ? i.Variant.CostPriceOverride.Value
                    : i.Product.CostPrice,
            })
            .ToListAsync();

        var monthRevenue    = monthItems.Sum(i => i.LineTotal);
        var monthCogs       = monthItems.Sum(i => i.BaseQuantity * i.CostPrice);
        var monthGrossProfit = monthRevenue - monthCogs;
        var monthExpenses   = await monthExpenseQuery.SumAsync(e => (decimal?)e.Amount) ?? 0m;
        var monthNetProfit  = monthGrossProfit - monthExpenses;

        // ─── Stock ────────────────────────────────────────────────────────────────

        var stockData = await BuildStockDataAsync(biz, branch);

        // ─── Branch analytics (monthly) ───────────────────────────────────────────

        var branchSalesRaw = await monthSalesQuery
            .GroupBy(i => i.BranchId)
            .Select(g => new
            {
                BranchId     = g.Key,
                TotalSales   = g.Sum(x => x.GrandTotal),
                InvoiceCount = g.Count(),
            })
            .ToListAsync();

        var branchIds = branchSalesRaw.Select(b => b.BranchId).ToList();
        var branchNames = await _db.Branches.AsNoTracking()
            .Where(b => branchIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Name);

        var branchItemProfits = await monthItemQuery
            .GroupBy(i => i.BranchId)
            .Select(g => new
            {
                BranchId = g.Key,
                Revenue  = g.Sum(x => x.LineTotal),
                Cost     = g.Sum(x => x.BaseQuantity * (
                    x.Variant != null && x.Variant.CostPriceOverride.HasValue
                        ? x.Variant.CostPriceOverride.Value
                        : x.Product.CostPrice)),
            })
            .ToListAsync();

        var branchExpenses = await monthExpenseQuery
            .GroupBy(e => e.BranchId)
            .Select(g => new { BranchId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync();

        var branchAnalytics = branchSalesRaw
            .Select(b =>
            {
                var profit = branchItemProfits.FirstOrDefault(p => p.BranchId == b.BranchId);
                var exp    = branchExpenses.FirstOrDefault(e => e.BranchId == b.BranchId);
                var gross  = (profit?.Revenue ?? 0m) - (profit?.Cost ?? 0m);
                var net    = gross - (exp?.Total ?? 0m);
                return new BranchAnalyticsDto
                {
                    BranchId     = b.BranchId,
                    BranchName   = branchNames.GetValueOrDefault(b.BranchId, "Unknown"),
                    TotalSales   = b.TotalSales,
                    InvoiceCount = b.InvoiceCount,
                    GrossProfit  = gross,
                    NetProfit    = net,
                };
            })
            .OrderByDescending(b => b.TotalSales)
            .ToList();

        // ─── Financial summary ────────────────────────────────────────────────────

        var totalPurchases = await monthPurchaseQuery.SumAsync(p => (decimal?)p.TotalAmount) ?? 0m;

        var cashFlowEntries = await _db.CashFlowTransactions.AsNoTracking()
            .Where(t => t.BusinessId == biz
                     && t.TransactionDate >= trendStart
                     && t.TransactionDate < todayEnd)
            .Where(t => branch <= 0 || t.BranchId == branch)
            .Select(t => new { t.TransactionDate, t.Amount, t.TransactionType })
            .ToListAsync();

        var dailyCashFlow = cashFlowEntries
            .GroupBy(t => t.TransactionDate.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var cashIn = g.Where(t => t.TransactionType is CashFlowTransactionType.Sale
                    or CashFlowTransactionType.CashIn
                    or CashFlowTransactionType.OpeningBalance).Sum(t => t.Amount);
                var cashOut = g.Where(t => t.TransactionType is CashFlowTransactionType.Expense
                    or CashFlowTransactionType.CashOut
                    or CashFlowTransactionType.BankTransfer).Sum(t => t.Amount);
                return new DailyCashFlowDto
                {
                    Date    = g.Key,
                    CashIn  = cashIn,
                    CashOut = cashOut,
                    NetFlow = cashIn - cashOut,
                };
            })
            .ToList();

        // ─── User activity ────────────────────────────────────────────────────────

        var recentUsersQuery = _db.Users.AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.BusinessId == biz && !u.IsDeleted && u.DeletedAt == null);

        if (branch > 0)
            recentUsersQuery = recentUsersQuery.Where(u => u.UserBranches.Any(ub => ub.BranchId == branch));

        var recentUsers = await recentUsersQuery
            .OrderByDescending(u => u.ModifiedAt ?? u.CreatedAt)
            .Take(RecentLimit)
            .Select(u => new RecentUserDto
            {
                UserId       = u.Id,
                FullName     = u.FullName,
                Username     = u.Username,
                RoleName     = u.Role.Name,
                IsActive     = u.IsActive,
                LastActivity = u.ModifiedAt ?? u.CreatedAt,
            })
            .ToListAsync();

        var salesByUsers = await monthSalesQuery
            .Where(i => i.CashierName != null && i.CashierName != "")
            .GroupBy(i => i.CashierName!)
            .Select(g => new SalesByUserDto
            {
                CashierName  = g.Key,
                InvoiceCount = g.Count(),
                TotalSales   = g.Sum(x => x.GrandTotal),
            })
            .OrderByDescending(s => s.TotalSales)
            .Take(RecentLimit)
            .ToListAsync();

        // ─── Recent transactions ──────────────────────────────────────────────────

        var recentSales = await salesQuery
            .OrderByDescending(i => i.SaleDate)
            .Take(RecentLimit)
            .Select(i => new RecentSaleDto
            {
                Id            = i.Id,
                InvoiceNo     = i.InvoiceNo,
                BranchName    = i.Branch.Name,
                CashierName   = i.CashierName ?? "—",
                GrandTotal    = i.GrandTotal,
                PaidAmount    = i.PaidAmount,
                PaymentStatus = i.PaidAmount >= i.GrandTotal ? "Paid" : "Pending",
                Status        = i.Status.ToString(),
                SaleDate      = i.SaleDate,
            })
            .ToListAsync();

        var recentPurchases = await purchaseQuery
            .Include(p => p.Supplier)
            .Include(p => p.Branch)
            .OrderByDescending(p => p.PurchaseDate)
            .Take(RecentLimit)
            .Select(p => new RecentPurchaseDto
            {
                Id           = p.Id,
                InvoiceNo    = p.InvoiceNo,
                BranchName   = p.Branch.Name,
                SupplierName = p.Supplier.Name,
                TotalAmount  = p.TotalAmount,
                Status       = p.Status.ToString(),
                PurchaseDate = p.PurchaseDate,
            })
            .ToListAsync();

        var returnQuery = _db.SaleInvoices.AsNoTracking()
            .Where(i => i.BusinessId == biz
                     && i.Status == SaleInvoiceStatus.Returned
                     && i.SaleDate >= monthStart
                     && i.SaleDate < todayEnd);
        if (branch > 0)
            returnQuery = returnQuery.Where(i => i.BranchId == branch);
        var returnCount = await returnQuery.CountAsync();

        var monthInvoices = await monthSalesQuery
            .Select(i => new { i.GrandTotal, i.PaidAmount })
            .ToListAsync();

        var pendingPaymentCount = monthInvoices.Count(i => i.PaidAmount < i.GrandTotal);
        var paidCount           = monthInvoices.Count(i => i.PaidAmount >= i.GrandTotal);

        // ─── Activity logs (derived from recent transactions) ─────────────────────

        var activityLogs = recentSales
            .Select(s => new ActivityLogDto
            {
                Type       = "Sale",
                Reference  = s.InvoiceNo,
                Amount     = s.GrandTotal,
                BranchName = s.BranchName,
                Timestamp  = s.SaleDate,
                Status     = s.PaymentStatus,
            })
            .Concat(recentPurchases.Select(p => new ActivityLogDto
            {
                Type       = "Purchase",
                Reference  = p.InvoiceNo,
                Amount     = p.TotalAmount,
                BranchName = p.BranchName,
                Timestamp  = p.PurchaseDate,
                Status     = p.Status,
            }))
            .OrderByDescending(a => a.Timestamp)
            .Take(RecentLimit)
            .ToList();

        // ─── Charts ───────────────────────────────────────────────────────────────

        var trendInvoices = await trendSalesQuery
            .Select(i => new { i.SaleDate, i.GrandTotal })
            .ToListAsync();

        var salesTrend = trendInvoices
            .GroupBy(i => i.SaleDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new SalesTrendPointDto
            {
                Date         = g.Key,
                TotalSales   = g.Sum(x => x.GrandTotal),
                InvoiceCount = g.Count(),
            })
            .ToList();

        var trendItems = await _db.SaleInvoiceItems.AsNoTracking()
            .Include(i => i.Product)
            .Include(i => i.Variant)
            .Include(i => i.SaleInvoice)
            .Where(i => i.BusinessId == biz
                     && i.SaleInvoice.Status == SaleInvoiceStatus.Completed
                     && i.SaleInvoice.SaleDate >= trendStart
                     && i.SaleInvoice.SaleDate < todayEnd)
            .Where(i => branch <= 0 || i.BranchId == branch)
            .Select(i => new
            {
                i.SaleInvoice.SaleDate,
                i.LineTotal,
                i.BaseQuantity,
                CostPrice = i.Variant != null && i.Variant.CostPriceOverride.HasValue
                    ? i.Variant.CostPriceOverride.Value
                    : i.Product.CostPrice,
            })
            .ToListAsync();

        var profitTrend = trendItems
            .GroupBy(i => i.SaleDate.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var revenue = g.Sum(x => x.LineTotal);
                var cost    = g.Sum(x => x.BaseQuantity * x.CostPrice);
                return new ProfitTrendPointDto
                {
                    Date        = g.Key,
                    Revenue     = revenue,
                    Cost        = cost,
                    GrossProfit = revenue - cost,
                };
            })
            .ToList();

        var topProducts = await monthItemQuery
            .GroupBy(i => new { i.ProductId, i.Product.ProductName, i.Product.ProductCode })
            .Select(g => new TopProductDto
            {
                ProductId     = g.Key.ProductId,
                ProductName   = g.Key.ProductName,
                ProductCode   = g.Key.ProductCode,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalAmount   = g.Sum(x => x.LineTotal),
            })
            .OrderByDescending(p => p.TotalAmount)
            .Take(10)
            .ToListAsync();

        var categoryPerformance = await monthItemQuery
            .GroupBy(i => new { i.Product.CategoryId, CategoryName = i.Product.Category.Name })
            .Select(g => new CategoryPerformanceDto
            {
                CategoryId    = g.Key.CategoryId,
                CategoryName  = g.Key.CategoryName,
                TotalSales    = g.Sum(x => x.LineTotal),
                TotalQuantity = g.Sum(x => x.Quantity),
            })
            .OrderByDescending(c => c.TotalSales)
            .Take(10)
            .ToListAsync();

        // ─── Response ─────────────────────────────────────────────────────────────

        return Ok(new DashboardOverviewDto
        {
            BranchId   = branch,
            BranchName = branchName,
            GeneratedAt = now,
            Kpis = new DashboardKpiDto
            {
                TotalBranches    = totalBranches,
                TotalUsers       = totalUsers,
                TodaySales       = todaySalesAgg?.Total ?? 0m,
                TodayInvoices    = todaySalesAgg?.Count ?? 0,
                MonthlySales     = monthSalesAgg?.Total ?? 0m,
                MonthlyInvoices  = monthSalesAgg?.Count ?? 0,
                GrossProfit      = monthGrossProfit,
                NetProfit        = monthNetProfit,
                StockValue       = stockData.TotalStockValue,
                LowStockCount    = stockData.LowStockCount,
                OutOfStockCount  = stockData.OutOfStockCount,
            },
            BranchAnalytics = branchAnalytics,
            Stock           = stockData,
            Financial = new DashboardFinancialDto
            {
                TotalSales     = monthSalesAgg?.Total ?? 0m,
                TotalPurchases = totalPurchases,
                GrossProfit    = monthGrossProfit,
                TotalExpenses  = monthExpenses,
                NetProfit      = monthNetProfit,
                DailyCashFlow  = dailyCashFlow,
            },
            UserActivity = new DashboardUserActivityDto
            {
                RecentUsers  = recentUsers,
                SalesByUsers = salesByUsers,
                ActivityLogs = activityLogs,
            },
            RecentTransactions = new DashboardRecentTransactionsDto
            {
                RecentSales          = recentSales,
                RecentPurchases      = recentPurchases,
                ReturnCount          = returnCount,
                PendingPaymentCount  = pendingPaymentCount,
                PaidCount            = paidCount,
            },
            Charts = new DashboardChartsDto
            {
                SalesTrend           = salesTrend,
                ProfitTrend          = profitTrend,
                TopProducts          = topProducts,
                CategoryPerformance  = categoryPerformance,
            },
        });
    }

    private async Task<DashboardStockDto> BuildStockDataAsync(int businessId, int branchId)
    {
        var ledgerQuery = _db.StockLedgerEntries.AsNoTracking()
            .Where(e => e.BusinessId == businessId);

        if (branchId > 0)
            ledgerQuery = ledgerQuery.Where(e => e.BranchId == branchId);

        var balances = await ledgerQuery
            .GroupBy(e => new { e.ProductId, e.VariantId, e.WarehouseId, e.BranchId })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.VariantId,
                g.Key.WarehouseId,
                Quantity = g.Sum(e => e.QuantityInBaseUnit),
            })
            .ToListAsync();

        var productIds   = balances.Select(b => b.ProductId).Distinct().ToList();
        var variantIds   = balances.Where(b => b.VariantId.HasValue).Select(b => b.VariantId!.Value).Distinct().ToList();
        var warehouseIds = balances.Select(b => b.WarehouseId).Distinct().ToList();

        var productMap = productIds.Count > 0
            ? await _db.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => new
                {
                    p.ProductName,
                    p.ProductCode,
                    p.CostPrice,
                    p.EnableLowStockAlert,
                    p.LowStockAlertLevel
                })
            : [];

        var variantMap = variantIds.Count > 0
            ? await _db.ProductVariants.AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => new { v.VariantName, v.CostPriceOverride })
            : [];

        var warehouseMap = warehouseIds.Count > 0
            ? await _db.Warehouses.AsNoTracking()
                .Where(w => warehouseIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.Name)
            : [];

        var totalProducts = branchId > 0
            ? await _db.Products.AsNoTracking().CountAsync(p => p.BusinessId == businessId && p.BranchId == branchId && !p.IsDeleted)
            : await _db.Products.AsNoTracking().CountAsync(p => p.BusinessId == businessId && !p.IsDeleted);

        var totalVariants = branchId > 0
            ? await _db.ProductVariants.AsNoTracking()
                .CountAsync(v => v.BusinessId == businessId && v.BranchId == branchId && !v.IsDeleted)
            : await _db.ProductVariants.AsNoTracking()
                .CountAsync(v => v.BusinessId == businessId && !v.IsDeleted);

        var items = balances.Select(b =>
        {
            productMap.TryGetValue(b.ProductId, out var product);
            variantMap.TryGetValue(b.VariantId ?? 0, out var variant);
            warehouseMap.TryGetValue(b.WarehouseId, out var whName);

            var costPrice  = variant?.CostPriceOverride ?? product?.CostPrice ?? 0m;
            var stockValue = b.Quantity * costPrice;

            return new StockAlertItemDto
            {
                ProductId     = b.ProductId,
                ProductName   = product?.ProductName ?? "Unknown",
                ProductCode   = product?.ProductCode ?? "",
                VariantId     = b.VariantId,
                VariantName   = variant?.VariantName,
                WarehouseId   = b.WarehouseId,
                WarehouseName = whName ?? "Unknown",
                Quantity      = b.Quantity,
                StockValue    = stockValue,
                AlertLevel    = product?.EnableLowStockAlert == true ? product.LowStockAlertLevel : null,
            };
        }).ToList();

        bool IsLowStock(StockAlertItemDto item)
        {
            if (item.Quantity <= 0 || !item.AlertLevel.HasValue)
                return false;
            return item.Quantity <= item.AlertLevel.Value;
        }

        var lowStockItems  = items.Where(IsLowStock).OrderBy(i => i.Quantity).Take(RecentLimit).ToList();
        var outOfStockItems = items.Where(i => i.Quantity <= 0).Take(RecentLimit).ToList();

        var warehouseDistribution = items
            .GroupBy(i => new { i.WarehouseId, i.WarehouseName })
            .Select(g => new WarehouseStockDto
            {
                WarehouseId   = g.Key.WarehouseId,
                WarehouseName = g.Key.WarehouseName,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalValue    = g.Sum(x => x.StockValue),
                ItemCount     = g.Count(),
            })
            .OrderByDescending(w => w.TotalValue)
            .ToList();

        return new DashboardStockDto
        {
            TotalProducts         = totalProducts,
            TotalVariants         = totalVariants,
            TotalQuantity         = items.Sum(i => i.Quantity),
            TotalStockValue       = items.Sum(i => i.StockValue),
            LowStockCount         = items.Count(IsLowStock),
            OutOfStockCount       = items.Count(i => i.Quantity <= 0),
            LowStockItems         = lowStockItems,
            OutOfStockItems       = outOfStockItems,
            WarehouseDistribution = warehouseDistribution,
        };
    }
}
