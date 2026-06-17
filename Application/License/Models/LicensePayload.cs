namespace POSSystem.Application.License.Models;

public sealed class LicensePayload
{
    public string LicenseId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int MaxBusinesses { get; set; }
    public int MaxBranchesPerBusiness { get; set; }
    public int MaxUsers { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
