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
