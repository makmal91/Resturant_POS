namespace POSSystem.Application.Auth.Interfaces;

public interface IFeaturePermissionService
{
    Task<bool> IsEnabledAsync(string featureKey);
    Task<bool> IsUnitEnabledAsync();
    Task<bool> IsVariantEnabledAsync();
    Task<bool> IsStockEnabledAsync();
    Task<bool> IsBarcodeEnabledAsync();
    Task<IReadOnlySet<string>> GetEnabledFeaturesAsync();
    Task<IReadOnlyList<string>> GetEnabledFeaturesForRoleAsync(int roleId, string roleName);
}
