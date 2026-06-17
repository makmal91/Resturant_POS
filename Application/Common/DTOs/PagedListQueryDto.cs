namespace POSSystem.Application.Common.DTOs;

public class PagedListQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }

    public (int Page, int PageSize) Normalize(int maxPageSize = 100)
    {
        var page = Math.Max(1, Page);
        var pageSize = Math.Clamp(PageSize, 1, maxPageSize);
        return (page, pageSize);
    }

    public bool IsDescending() =>
        string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
}
