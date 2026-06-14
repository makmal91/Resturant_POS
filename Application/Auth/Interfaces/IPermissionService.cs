using POSSystem.Application.Common.Constants;
using POSSystem.Application.Users.DTOs;

namespace POSSystem.Application.Auth.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int roleId, string roleName, string moduleName, string action);
    Task<IReadOnlyList<RolePermissionDto>> GetPermissionsAsync(int roleId);
    bool IsBypassRole(string roleName);
}
