namespace POSSystem.Application.License.Interfaces;

public enum LicenseCreateOperation
{
    Business,
    Branch,
    User
}

public interface ILicenseEnforcementService
{
    Task EnsureCanCreateAsync(
        LicenseCreateOperation operation,
        int? businessId = null,
        CancellationToken cancellationToken = default);
}
