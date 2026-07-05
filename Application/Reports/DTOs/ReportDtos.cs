namespace POSSystem.Application.Reports.DTOs;

public class ReportFilterDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? Search { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public int? SubCategoryId { get; set; }
    public int? BrandId { get; set; }

    /// <summary>Aging bucket filter: 0-30, 31-60, 61-90, 90+</summary>
    public string? AgingBucket { get; set; }

    /// <summary>Profit &amp; loss detail grouping: day, month, or year.</summary>
    public string? GroupBy { get; set; }

    public (int PageNumber, int PageSize) Normalize(int maxPageSize = 100)
    {
        var pageNumber = Math.Max(1, PageNumber);
        var pageSize = Math.Clamp(PageSize, 1, maxPageSize);
        return (pageNumber, pageSize);
    }

    public bool IsDescending() =>
        string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

    public (DateTime From, DateTime ToExclusive) ResolveDateRange()
    {
        var from = (FromDate ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
        var toExclusive = (ToDate ?? DateTime.UtcNow).Date.AddDays(1);
        return (from, toExclusive);
    }
}

public class ReportPagedResultDto<T>
{
    public IReadOnlyList<T> Data { get; set; } = Array.Empty<T>();
    public int TotalRecords { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }

    public static ReportPagedResultDto<T> Create(
        IReadOnlyList<T> data, int totalRecords, int pageNumber, int pageSize)
    {
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);
        if (totalPages > 0 && pageNumber > totalPages)
            pageNumber = totalPages;

        return new ReportPagedResultDto<T>
        {
            Data = data,
            TotalRecords = totalRecords,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }
}

public class SalesReportRowDto
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public bool IsCreditSale { get; set; }
    public decimal CashAmount { get; set; }
    public decimal CardAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CashierName { get; set; }
}

public class PurchaseReportRowDto
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsCreditPurchase { get; set; }
}

public class CustomerOutstandingRowDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal OpeningBalance { get; set; }
    public int OutstandingInvoices { get; set; }
    public decimal InvoiceOutstanding { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateTime? LastSaleDate { get; set; }
}

public class SupplierPayableRowDto
{
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int OutstandingInvoices { get; set; }
    public decimal InvoicePayable { get; set; }
    public decimal PayableAmount { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
}

public class ProfitLossRowDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public decimal Discounts { get; set; }
    public decimal Tax { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetProfit { get; set; }
    public int SalesCount { get; set; }
}

public class ProfitLossReportSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalCostOfGoodsSold { get; set; }
    public decimal TotalGrossProfit { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalNetProfit { get; set; }
    public int TotalSalesCount { get; set; }
    /// <summary>Stock adjustment gain (increase) amounts included in <see cref="TotalRevenue"/>.</summary>
    public decimal StockAdjustmentGain { get; set; }
    /// <summary>Stock adjustment loss amounts included in <see cref="TotalExpenses"/>.</summary>
    public decimal StockAdjustmentLoss { get; set; }
}

public class ProfitLossReportPagedResultDto : ReportPagedResultDto<ProfitLossRowDto>
{
    public ProfitLossReportSummaryDto Summary { get; set; } = new();
}

public class ProfitLossExpenseLineDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class ProfitLossStatementDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public ProfitLossReportSummaryDto Summary { get; set; } = new();
    public IReadOnlyList<ProfitLossExpenseLineDto> ExpenseLines { get; set; } = Array.Empty<ProfitLossExpenseLineDto>();
}

public class ReceivableAgingRowDto
{
    public int InvoiceId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Outstanding { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingBucket { get; set; } = string.Empty;
}

public class PayableAgingRowDto
{
    public int InvoiceId { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Outstanding { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingBucket { get; set; } = string.Empty;
}

public class AgingReportSummaryDto
{
    public decimal TotalOutstanding { get; set; }
    public decimal Bucket0To30 { get; set; }
    public decimal Bucket31To60 { get; set; }
    public decimal Bucket61To90 { get; set; }
    public decimal Bucket90Plus { get; set; }
    public DateTime AsOfDate { get; set; }
}

public class AgingReportPagedResultDto<T> : ReportPagedResultDto<T>
{
    public AgingReportSummaryDto Summary { get; set; } = new();
}

public class ProductWiseSalesReportRowDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? SubCategoryId { get; set; }
    public string? SubCategoryName { get; set; }
    public int? BrandId { get; set; }
    public string? BrandName { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalBaseQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit { get; set; }
    public int InvoiceCount { get; set; }
}

public class ProductWiseSalesReportSummaryDto
{
    public int TotalProducts { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit { get; set; }
    public int TotalInvoices { get; set; }
}

public class ProductWiseSalesReportPagedResultDto : ReportPagedResultDto<ProductWiseSalesReportRowDto>
{
    public ProductWiseSalesReportSummaryDto Summary { get; set; } = new();
}
