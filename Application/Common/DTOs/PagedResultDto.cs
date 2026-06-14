namespace POSSystem.Application.Common.DTOs;

public class PagedResultDto<T>
{
    public IReadOnlyList<T> Data { get; set; } = Array.Empty<T>();
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
