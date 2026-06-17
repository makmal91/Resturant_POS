namespace POSSystem.Application.License.DTOs;

public sealed class LicenseUsageDto
{
    public int CurrentBusinesses { get; set; }
    public int MaxBusinesses { get; set; }
    public int TotalUsers { get; set; }
    public int MaxUsers { get; set; }
    public IReadOnlyList<LicenseBranchUsageDto> BranchUsageByBusiness { get; set; } = Array.Empty<LicenseBranchUsageDto>();
}

public sealed class LicenseBranchUsageDto
{
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public int CurrentBranches { get; set; }
    public int MaxBranchesPerBusiness { get; set; }
}
