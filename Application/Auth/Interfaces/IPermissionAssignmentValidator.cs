using POSSystem.Application.Modules.DTOs;
using POSSystem.Application.Users.DTOs;

namespace POSSystem.Application.Auth.Interfaces;

public interface IPermissionAssignmentValidator
{
    Task ValidateModulePermissionsAsync(
        int actorRoleId,
        string? actorRoleName,
        int targetRoleId,
        IReadOnlyList<SaveRolePermissionItemDto> requestedPermissions,
        IReadOnlyDictionary<int, (string ModuleKey, string ModuleName)> moduleLookup);

    Task ValidateFormPermissionsAsync(
        int actorRoleId,
        string? actorRoleName,
        int targetRoleId,
        IReadOnlyList<SaveFormPermissionItemDto> requestedFormPermissions,
        IReadOnlyDictionary<int, int> formToModuleMap);

    Task ValidateLegacyPermissionsAsync(
        int actorRoleId,
        string? actorRoleName,
        int targetRoleId,
        IReadOnlyList<RolePermissionDto> requestedPermissions);
}
