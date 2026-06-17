namespace POSSystem.Domain;

public class ExceptionLog
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public long? BranchId { get; set; }
    public string Module { get; set; } = "Unknown";
    public string? FormName { get; set; }
    public string? ActionName { get; set; }
    public string ExceptionMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? InnerException { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
