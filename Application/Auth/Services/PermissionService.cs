using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Users.DTOs;
using POSSystem.Application.Users.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Auth.Services;

public class PermissionService : IPermissionService
{
    private readonly IRoleRepository _roleRepository;

    public PermissionService(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public bool IsBypassRole(string roleName) =>
        RoleNames.CanBypassPermissions(roleName);

    public Task<IReadOnlyList<RolePermissionDto>> GetPermissionsAsync(int roleId) =>
        _roleRepository.GetPermissionsAsync(roleId);

    public async Task<bool> HasPermissionAsync(int roleId, string roleName, string moduleName, string action)
    {
        if (IsBypassRole(roleName))
            return true;

        var permissions = await _roleRepository.GetPermissionsAsync(roleId);
        var modulePermission = permissions.FirstOrDefault(p =>
            string.Equals(p.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase));

        if (modulePermission == null)
            return false;

        return action switch
        {
            PermissionActions.View => modulePermission.CanView,
            PermissionActions.Create => modulePermission.CanCreate,
            PermissionActions.Edit => modulePermission.CanEdit,
            PermissionActions.Delete => modulePermission.CanDelete,
            PermissionActions.Export => modulePermission.CanExport,
            PermissionActions.Upload => modulePermission.CanUpload,
            _ => false
        };
    }
}
