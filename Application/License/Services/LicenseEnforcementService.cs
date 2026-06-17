using POSSystem.Application.License.Interfaces;

namespace POSSystem.Application.License.Services;

public sealed class LicenseEnforcementService : ILicenseEnforcementService
{
    private readonly ILicenseService _licenseService;
    private readonly ILicenseUsageProvider _usageProvider;

    public LicenseEnforcementService(ILicenseService licenseService, ILicenseUsageProvider usageProvider)
    {
        _licenseService = licenseService;
        _usageProvider = usageProvider;
    }

    public async Task EnsureCanCreateAsync(
        LicenseCreateOperation operation,
        int? businessId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_licenseService.IsOperational)
            throw new InvalidOperationException(_licenseService.GetStatus().Message ?? "License is invalid or expired.");

        var payload = _licenseService.GetActivePayload()
            ?? throw new InvalidOperationException("License is invalid or expired.");

        switch (operation)
        {
            case LicenseCreateOperation.Business:
            {
                var currentBusinesses = await _usageProvider.GetBusinessCountAsync(cancellationToken);
                if (currentBusinesses >= payload.MaxBusinesses)
                    throw new InvalidOperationException("Business limit reached.");
                break;
            }
            case LicenseCreateOperation.Branch:
            {
                if (!businessId.HasValue || businessId.Value <= 0)
                    throw new InvalidOperationException("BusinessId is required for branch license validation.");

                var branchesCount = await _usageProvider.GetBranchCountAsync(businessId.Value, cancellationToken);
                if (branchesCount >= payload.MaxBranchesPerBusiness)
                    throw new InvalidOperationException("Branch limit reached.");
                break;
            }
            case LicenseCreateOperation.User:
            {
                var totalUsers = await _usageProvider.GetTotalUserCountAsync(cancellationToken);
                if (totalUsers >= payload.MaxUsers)
                    throw new InvalidOperationException("User limit reached.");
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unsupported license operation.");
        }
    }
}
