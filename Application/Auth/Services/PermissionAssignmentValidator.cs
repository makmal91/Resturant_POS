using POSSystem.Application.Auth.Exceptions;
using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Modules.DTOs;
using POSSystem.Application.Modules.Interfaces;
using POSSystem.Application.Users.DTOs;
using POSSystem.Application.Users.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Auth.Services;

public class PermissionAssignmentValidator : IPermissionAssignmentValidator
{
    private readonly IRoleRepository _roleRepository;
    private readonly IModuleRepository _moduleRepository;

    public PermissionAssignmentValidator(
        IRoleRepository roleRepository,
        IModuleRepository moduleRepository)
    {
        _roleRepository = roleRepository;
        _moduleRepository = moduleRepository;
    }

    public async Task ValidateModulePermissionsAsync(
        int actorRoleId,
        string? actorRoleName,
        int targetRoleId,
        IReadOnlyList<SaveRolePermissionItemDto> requestedPermissions,
        IReadOnlyDictionary<int, (string ModuleKey, string ModuleName)> moduleLookup)
    {
        if (RoleNames.IsMasterUser(actorRoleName ?? string.Empty))
            return;

        var actorPermissions = await _roleRepository.GetPermissionsAsync(actorRoleId);
        var existingTargetPermissions = await _roleRepository.GetPermissionsAsync(targetRoleId);

        foreach (var requested in requestedPermissions)
        {
            if (!moduleLookup.TryGetValue(requested.ModuleId, out var module))
                continue;

            var actorPermission = FindModulePermission(actorPermissions, requested.ModuleId, module.ModuleKey, module.ModuleName);
            var existingPermission = FindModulePermission(existingTargetPermissions, requested.ModuleId, module.ModuleKey, module.ModuleName);

            var actorCanManage = actorPermission?.CanView ?? false;

            if (!actorCanManage)
            {
                EnsureUnchanged(
                    requested.CanView, existingPermission?.CanView ?? false,
                    module.ModuleName, PermissionActions.View);
                EnsureUnchanged(
                    requested.CanCreate, existingPermission?.CanCreate ?? false,
                    module.ModuleName, PermissionActions.Create);
                EnsureUnchanged(
                    requested.CanEdit, existingPermission?.CanEdit ?? false,
                    module.ModuleName, PermissionActions.Edit);
                EnsureUnchanged(
                    requested.CanDelete, existingPermission?.CanDelete ?? false,
                    module.ModuleName, PermissionActions.Delete);
                EnsureUnchanged(
                    requested.CanExport, existingPermission?.CanExport ?? false,
                    module.ModuleName, PermissionActions.Export);
                EnsureUnchanged(
                    requested.CanUpload, existingPermission?.CanUpload ?? false,
                    module.ModuleName, PermissionActions.Upload);
                continue;
            }

            EnsureNotEscalating(requested.CanView, actorPermission?.CanView ?? false, module.ModuleName, PermissionActions.View);
            EnsureNotEscalating(requested.CanCreate, actorPermission?.CanCreate ?? false, module.ModuleName, PermissionActions.Create);
            EnsureNotEscalating(requested.CanEdit, actorPermission?.CanEdit ?? false, module.ModuleName, PermissionActions.Edit);
            EnsureNotEscalating(requested.CanDelete, actorPermission?.CanDelete ?? false, module.ModuleName, PermissionActions.Delete);
            EnsureNotEscalating(requested.CanExport, actorPermission?.CanExport ?? false, module.ModuleName, PermissionActions.Export);
            EnsureNotEscalating(requested.CanUpload, actorPermission?.CanUpload ?? false, module.ModuleName, PermissionActions.Upload);
        }
    }

    public async Task ValidateFormPermissionsAsync(
        int actorRoleId,
        string? actorRoleName,
        int targetRoleId,
        IReadOnlyList<SaveFormPermissionItemDto> requestedFormPermissions,
        IReadOnlyDictionary<int, int> formToModuleMap)
    {
        if (RoleNames.IsMasterUser(actorRoleName ?? string.Empty))
            return;

        var actorFormPermissions = await _moduleRepository.GetFormPermissionsForRoleAsync(actorRoleId);
        var existingTargetFormPermissions = await _moduleRepository.GetFormPermissionsForRoleAsync(targetRoleId);
        var actorModulePermissions = await _roleRepository.GetPermissionsAsync(actorRoleId);
        var existingTargetModulePermissions = await _roleRepository.GetPermissionsAsync(targetRoleId);
        var allForms = await _moduleRepository.GetAllFormsAsync();
        var formLookup = allForms.ToDictionary(f => f.Id);

        foreach (var requested in requestedFormPermissions)
        {
            if (!formLookup.TryGetValue(requested.FormId, out var form))
                continue;

            if (!formToModuleMap.TryGetValue(requested.FormId, out var moduleId))
                moduleId = form.ModuleId;

            var actorForm = actorFormPermissions.FirstOrDefault(fp => fp.FormId == requested.FormId);
            var existingForm = existingTargetFormPermissions.FirstOrDefault(fp => fp.FormId == requested.FormId);

            var moduleInfo = formLookup.ContainsKey(requested.FormId)
                ? (form.ModuleId, string.Empty, form.FormName)
                : (moduleId, string.Empty, form.FormName);

            var actorModule = FindModulePermission(actorModulePermissions, moduleInfo.Item1, string.Empty, string.Empty);
            var actorCanManageModule = actorModule?.CanView ?? false;

            if (!actorCanManageModule)
            {
                EnsureUnchanged(requested.CanView, existingForm?.CanView ?? false, form.FormName, PermissionActions.View);
                EnsureUnchanged(requested.CanCreate, existingForm?.CanCreate ?? false, form.FormName, PermissionActions.Create);
                EnsureUnchanged(requested.CanEdit, existingForm?.CanEdit ?? false, form.FormName, PermissionActions.Edit);
                EnsureUnchanged(requested.CanDelete, existingForm?.CanDelete ?? false, form.FormName, PermissionActions.Delete);
                continue;
            }

            var actorCanView = actorForm?.CanView ?? actorModule?.CanView ?? false;
            var actorCanCreate = actorForm?.CanCreate ?? actorModule?.CanCreate ?? false;
            var actorCanEdit = actorForm?.CanEdit ?? actorModule?.CanEdit ?? false;
            var actorCanDelete = actorForm?.CanDelete ?? actorModule?.CanDelete ?? false;

            EnsureNotEscalating(requested.CanView, actorCanView, form.FormName, PermissionActions.View);
            EnsureNotEscalating(requested.CanCreate, actorCanCreate, form.FormName, PermissionActions.Create);
            EnsureNotEscalating(requested.CanEdit, actorCanEdit, form.FormName, PermissionActions.Edit);
            EnsureNotEscalating(requested.CanDelete, actorCanDelete, form.FormName, PermissionActions.Delete);
        }
    }

    public async Task ValidateLegacyPermissionsAsync(
        int actorRoleId,
        string? actorRoleName,
        int targetRoleId,
        IReadOnlyList<RolePermissionDto> requestedPermissions)
    {
        if (RoleNames.IsMasterUser(actorRoleName ?? string.Empty))
            return;

        var actorPermissions = await _roleRepository.GetPermissionsAsync(actorRoleId);
        var existingTargetPermissions = await _roleRepository.GetPermissionsAsync(targetRoleId);

        foreach (var requested in requestedPermissions)
        {
            if (string.IsNullOrWhiteSpace(requested.ModuleName))
                continue;

            var moduleName = requested.ModuleName.Trim();
            var actorPermission = FindModulePermission(actorPermissions, requested.ModuleId, moduleName, moduleName);
            var existingPermission = FindModulePermission(existingTargetPermissions, requested.ModuleId, moduleName, moduleName);
            var actorCanManage = actorPermission?.CanView ?? false;

            if (!actorCanManage)
            {
                EnsureUnchanged(requested.CanView, existingPermission?.CanView ?? false, moduleName, PermissionActions.View);
                EnsureUnchanged(requested.CanCreate, existingPermission?.CanCreate ?? false, moduleName, PermissionActions.Create);
                EnsureUnchanged(requested.CanEdit, existingPermission?.CanEdit ?? false, moduleName, PermissionActions.Edit);
                EnsureUnchanged(requested.CanDelete, existingPermission?.CanDelete ?? false, moduleName, PermissionActions.Delete);
                EnsureUnchanged(requested.CanExport, existingPermission?.CanExport ?? false, moduleName, PermissionActions.Export);
                EnsureUnchanged(requested.CanUpload, existingPermission?.CanUpload ?? false, moduleName, PermissionActions.Upload);
                continue;
            }

            EnsureNotEscalating(requested.CanView, actorPermission?.CanView ?? false, moduleName, PermissionActions.View);
            EnsureNotEscalating(requested.CanCreate, actorPermission?.CanCreate ?? false, moduleName, PermissionActions.Create);
            EnsureNotEscalating(requested.CanEdit, actorPermission?.CanEdit ?? false, moduleName, PermissionActions.Edit);
            EnsureNotEscalating(requested.CanDelete, actorPermission?.CanDelete ?? false, moduleName, PermissionActions.Delete);
            EnsureNotEscalating(requested.CanExport, actorPermission?.CanExport ?? false, moduleName, PermissionActions.Export);
            EnsureNotEscalating(requested.CanUpload, actorPermission?.CanUpload ?? false, moduleName, PermissionActions.Upload);
        }
    }

    private static void EnsureNotEscalating(bool requested, bool actorHas, string scope, string action)
    {
        if (requested && !actorHas)
        {
            throw new PermissionEscalationException(
                $"You cannot grant {action} permission for {scope} because it is not assigned to your role.");
        }
    }

    private static void EnsureUnchanged(bool requested, bool existing, string scope, string action)
    {
        if (requested != existing)
        {
            throw new PermissionEscalationException(
                $"You cannot modify {action} permission for {scope} because it is outside your assigned permissions.");
        }
    }

    private static RolePermissionDto? FindModulePermission(
        IReadOnlyList<RolePermissionDto> permissions,
        int? moduleId,
        string moduleKey,
        string moduleName)
    {
        if (moduleId.HasValue)
        {
            var byId = permissions.FirstOrDefault(p => p.ModuleId == moduleId);
            if (byId != null)
                return byId;
        }

        if (!string.IsNullOrWhiteSpace(moduleKey))
        {
            var byKey = permissions.FirstOrDefault(p =>
                PermissionModuleResolver.Matches(p.ModuleName, moduleKey));
            if (byKey != null)
                return byKey;
        }

        if (!string.IsNullOrWhiteSpace(moduleName))
        {
            return permissions.FirstOrDefault(p =>
                PermissionModuleResolver.Matches(p.ModuleName, moduleName));
        }

        return null;
    }
}
