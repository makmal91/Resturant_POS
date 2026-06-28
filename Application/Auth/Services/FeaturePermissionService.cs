using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Auth.Services;

public class FeaturePermissionService : IFeaturePermissionService
{
    private readonly ITenantContext _tenantContext;
    private readonly IPermissionService _permissionService;
    private IReadOnlySet<string>? _requestCache;

    public FeaturePermissionService(ITenantContext tenantContext, IPermissionService permissionService)
    {
        _tenantContext = tenantContext;
        _permissionService = permissionService;
    }

    public Task<bool> IsUnitEnabledAsync() => IsEnabledAsync(PermissionFeatureKeys.UnitEnable);

    public Task<bool> IsVariantEnabledAsync() => IsEnabledAsync(PermissionFeatureKeys.VariantEnable);

    public Task<bool> IsStockEnabledAsync() => IsEnabledAsync(PermissionFeatureKeys.StockEnable);

    public Task<bool> IsBarcodeEnabledAsync() => IsEnabledAsync(PermissionFeatureKeys.BarcodeEnable);

    public async Task<bool> IsEnabledAsync(string featureKey)
    {
        if (string.IsNullOrWhiteSpace(featureKey))
            return false;

        var roleName = ResolveRoleName() ?? string.Empty;
        if (RoleNames.CanBypassPermissions(roleName))
            return true;

        var moduleName = PermissionFeatureKeys.MapToModule(featureKey);
        if (moduleName == null)
            return false;

        var roleId = _tenantContext.RoleId;
        if (!roleId.HasValue)
            return false;

        return await _permissionService.HasPermissionAsync(
            roleId.Value, roleName, moduleName, PermissionActions.View);
    }

    public async Task<IReadOnlySet<string>> GetEnabledFeaturesAsync()
    {
        if (_requestCache != null)
            return _requestCache;

        var roleId = _tenantContext.RoleId;
        var roleName = ResolveRoleName() ?? string.Empty;

        if (!roleId.HasValue)
        {
            _requestCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return _requestCache;
        }

        var features = await GetEnabledFeaturesForRoleAsync(roleId.Value, roleName);
        _requestCache = features.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return _requestCache;
    }

    public async Task<IReadOnlyList<string>> GetEnabledFeaturesForRoleAsync(int roleId, string roleName)
    {
        if (RoleNames.CanBypassPermissions(roleName))
            return PermissionFeatureKeys.All.ToList();

        var enabled = new List<string>();

        foreach (var featureKey in PermissionFeatureKeys.All)
        {
            var moduleName = PermissionFeatureKeys.MapToModule(featureKey);
            if (moduleName == null)
                continue;

            if (await _permissionService.HasPermissionAsync(roleId, roleName, moduleName, PermissionActions.View))
                enabled.Add(featureKey);
        }

        return enabled;
    }

    private string? ResolveRoleName() => _tenantContext.RoleName;
}
