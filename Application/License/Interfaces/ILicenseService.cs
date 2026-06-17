using POSSystem.Application.License.DTOs;
using POSSystem.Application.License.Models;

namespace POSSystem.Application.License.Interfaces;

public interface ILicenseService
{
    LicenseSnapshot? Current { get; }
    bool IsOperational { get; }
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task ReloadAsync(CancellationToken cancellationToken = default);
    Task InstallLicenseFileAsync(Stream licenseStream, CancellationToken cancellationToken = default);
    LicenseStatusDto GetStatus();
    LicensePayload? GetActivePayload();
}
