namespace POSSystem.Application.Dashboard.DTOs;

public class DashboardOverviewDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }

    public DashboardKpiDto Kpis { get; set; } = new();
    public List<BranchAnalyticsDto> BranchAnalytics { get; set; } = [];
    public DashboardStockDto Stock { get; set; } = new();
    public DashboardFinancialDto Financial { get; set; } = new();
    public DashboardUserActivityDto UserActivity { get; set; } = new();
    public DashboardRecentTransactionsDto RecentTransactions { get; set; } = new();
    public DashboardChartsDto Charts { get; set; } = new();
}

public class DashboardKpiDto
{
    public int TotalBranches { get; set; }
    public int TotalUsers { get; set; }
    public decimal TodaySales { get; set; }
    public int TodayInvoices { get; set; }
    public decimal MonthlySales { get; set; }
    public int MonthlyInvoices { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
    public decimal StockValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
}

public class BranchAnalyticsDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public int InvoiceCount { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
}

public class DashboardStockDto
{
    public int TotalProducts { get; set; }
    public int TotalVariants { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalStockValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public List<StockAlertItemDto> LowStockItems { get; set; } = [];
    public List<StockAlertItemDto> OutOfStockItems { get; set; } = [];
    public List<WarehouseStockDto> WarehouseDistribution { get; set; } = [];
}

public class StockAlertItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public string? VariantName { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal StockValue { get; set; }
    public decimal? AlertLevel { get; set; }
}

public class WarehouseStockDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public int ItemCount { get; set; }
}

public class DashboardFinancialDto
{
    public decimal TotalSales { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal TotalReceivables { get; set; }
    public decimal TotalPayables { get; set; }
    public List<DailyCashFlowDto> DailyCashFlow { get; set; } = [];
}

public class DailyCashFlowDto
{
    public DateTime Date { get; set; }
    public decimal CashIn { get; set; }
    public decimal CashOut { get; set; }
    public decimal NetFlow { get; set; }
}

public class DashboardUserActivityDto
{
    public List<RecentUserDto> RecentUsers { get; set; } = [];
    public List<SalesByUserDto> SalesByUsers { get; set; } = [];
    public List<ActivityLogDto> ActivityLogs { get; set; } = [];
}

public class RecentUserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastActivity { get; set; }
}

public class SalesByUserDto
{
    public string CashierName { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalSales { get; set; }
}

public class ActivityLogDto
{
    public string Type { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DashboardRecentTransactionsDto
{
    public List<RecentSaleDto> RecentSales { get; set; } = [];
    public List<RecentPurchaseDto> RecentPurchases { get; set; } = [];
    public int ReturnCount { get; set; }
    public int PendingPaymentCount { get; set; }
    public int PaidCount { get; set; }
}

public class RecentSaleDto
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
}

public class RecentPurchaseDto
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
}

public class DashboardChartsDto
{
    public List<SalesTrendPointDto> SalesTrend { get; set; } = [];
    public List<ProfitTrendPointDto> ProfitTrend { get; set; } = [];
    public List<TopProductDto> TopProducts { get; set; } = [];
    public List<CategoryPerformanceDto> CategoryPerformance { get; set; } = [];
}

public class SalesTrendPointDto
{
    public DateTime Date { get; set; }
    public decimal TotalSales { get; set; }
    public int InvoiceCount { get; set; }
}

public class ProfitTrendPointDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal GrossProfit { get; set; }
}

public class TopProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CategoryPerformanceDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public decimal TotalQuantity { get; set; }
}

// ─── Sales Person (My Sales) Summary ─────────────────────────────────────────

public class SalesPersonSummaryDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }

    public SalesPersonKpiDto Kpis { get; set; } = new();
    public SalesPersonPaymentDto Payment { get; set; } = new();
    public List<RecentSaleDto> RecentSales { get; set; } = [];
    public List<SalesTrendPointDto> SalesTrend { get; set; } = [];
    public List<TopProductDto> TopProducts { get; set; } = [];
}

public class SalesPersonKpiDto
{
    public decimal TodaySales { get; set; }
    public int TodayInvoices { get; set; }
    public decimal MonthlySales { get; set; }
    public int MonthlyInvoices { get; set; }
    public decimal AverageSale { get; set; }
    public decimal TodayCash { get; set; }
    public decimal TodayCard { get; set; }
    public int PendingPaymentCount { get; set; }
    public int PaidCount { get; set; }
}

public class SalesPersonPaymentDto
{
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal TotalMixed { get; set; }
    public int CashInvoices { get; set; }
    public int CardInvoices { get; set; }
    public int MixedInvoices { get; set; }
}
