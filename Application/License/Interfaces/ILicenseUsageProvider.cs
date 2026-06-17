using POSSystem.Application.License.DTOs;

namespace POSSystem.Application.License.Interfaces;

public interface ILicenseUsageProvider
{
    Task<int> GetBusinessCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetBranchCountAsync(int businessId, CancellationToken cancellationToken = default);
    Task<int> GetTotalUserCountAsync(CancellationToken cancellationToken = default);
    Task<LicenseUsageDto> GetUsageSnapshotAsync(CancellationToken cancellationToken = default);
}
