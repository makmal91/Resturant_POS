namespace POSSystem.Application.License.DTOs;

public sealed class LicenseStatusDto
{
    public bool IsValid { get; set; }
    public bool IsExpired { get; set; }
    public string? Message { get; set; }
    public string? LicenseId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int MaxBusinesses { get; set; }
    public int MaxBranchesPerBusiness { get; set; }
    public int MaxUsers { get; set; }
    public DateTime? LoadedAt { get; set; }
}
