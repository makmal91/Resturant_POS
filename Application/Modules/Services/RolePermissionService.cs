using POSSystem.Application.Modules.DTOs;
using POSSystem.Application.Modules.Interfaces;
using POSSystem.Application.Users.DTOs;
using POSSystem.Application.Users.Interfaces;

namespace POSSystem.Application.Modules.Services;

public class RolePermissionService : IRolePermissionService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IModuleRepository _moduleRepository;

    public RolePermissionService(
        IRoleRepository roleRepository,
        IModuleRepository moduleRepository)
    {
        _roleRepository = roleRepository;
        _moduleRepository = moduleRepository;
    }

    public async Task<IReadOnlyList<ModulePermissionItemDto>> GetRolePermissionsAsync(int roleId)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null)
            throw new InvalidOperationException("Role not found.");

        var modules = await _moduleRepository.GetAllModulesFlatAsync();
        var forms = await _moduleRepository.GetAllFormsAsync();
        var permissions = await _roleRepository.GetPermissionsAsync(roleId);
        var formPermissions = await _moduleRepository.GetFormPermissionsForRoleAsync(roleId);
        var formPermissionMap = formPermissions.ToDictionary(fp => fp.FormId);

        return modules
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.ModuleName)
            .Select(module =>
            {
                var permission = FindModulePermission(permissions, module.Id, module.ModuleKey, module.ModuleName);
                var moduleCanView = permission?.CanView ?? false;

                var moduleForms = forms
                    .Where(f => f.ModuleId == module.Id)
                    .OrderBy(f => f.SortOrder)
                    .Select(f =>
                    {
                        formPermissionMap.TryGetValue(f.Id, out var formPerm);
                        return new FormPermissionItemDto
                        {
                            FormId = f.Id,
                            ModuleId = f.ModuleId,
                            FormName = f.FormName,
                            FormCode = f.FormCode,
                            CanView = moduleCanView && (formPerm?.CanView ?? permission?.CanView ?? false),
                            CanCreate = moduleCanView && (formPerm?.CanCreate ?? permission?.CanCreate ?? false),
                            CanEdit = moduleCanView && (formPerm?.CanEdit ?? permission?.CanEdit ?? false),
                            CanDelete = moduleCanView && (formPerm?.CanDelete ?? permission?.CanDelete ?? false)
                        };
                    })
                    .ToList();

                return new ModulePermissionItemDto
                {
                    ModuleId = module.Id,
                    ModuleName = module.ModuleName,
                    ModuleKey = module.ModuleKey,
                    ParentModuleId = module.ParentModuleId,
                    DisplayOrder = module.DisplayOrder,
                    CanView = moduleCanView,
                    CanCreate = moduleCanView && (permission?.CanCreate ?? false),
                    CanEdit = moduleCanView && (permission?.CanEdit ?? false),
                    CanDelete = moduleCanView && (permission?.CanDelete ?? false),
                    CanExport = moduleCanView && (permission?.CanExport ?? false),
                    CanUpload = moduleCanView && (permission?.CanUpload ?? false),
                    Forms = moduleForms
                };
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ModulePermissionItemDto>> SaveRolePermissionsAsync(SaveRolePermissionsRequestDto dto)
    {
        var role = await _roleRepository.GetTrackedByIdAsync(dto.RoleId);
        if (role == null)
            throw new InvalidOperationException("Role not found.");

        var modules = await _moduleRepository.GetAllModulesFlatAsync();
        var moduleLookup = modules.ToDictionary(m => m.Id);
        var allForms = await _moduleRepository.GetAllFormsAsync();
        var formsByModule = allForms.ToLookup(f => f.ModuleId);

        var normalizedFormPermissions = dto.FormPermissions
            .Select(fp => new SaveFormPermissionItemDto
            {
                FormId = fp.FormId,
                CanView = fp.CanView,
                CanCreate = fp.CanCreate,
                CanEdit = fp.CanEdit,
                CanDelete = fp.CanDelete
            })
            .ToList();

        var permissionDtos = new List<RolePermissionDto>();

        foreach (var item in dto.Permissions)
        {
            if (!moduleLookup.TryGetValue(item.ModuleId, out var module))
                continue;

            var moduleFormIds = formsByModule[module.Id].Select(f => f.Id).ToHashSet();
            var relevantFormPerms = normalizedFormPermissions
                .Where(fp => moduleFormIds.Contains(fp.FormId))
                .ToList();

            if (!item.CanView)
            {
                foreach (var formPerm in relevantFormPerms)
                {
                    formPerm.CanView = false;
                    formPerm.CanCreate = false;
                    formPerm.CanEdit = false;
                    formPerm.CanDelete = false;
                }
            }

            var aggregatedCreate = item.CanView && (
                relevantFormPerms.Count > 0
                    ? relevantFormPerms.Any(fp => fp.CanCreate)
                    : item.CanCreate);
            var aggregatedEdit = item.CanView && (
                relevantFormPerms.Count > 0
                    ? relevantFormPerms.Any(fp => fp.CanEdit)
                    : item.CanEdit);
            var aggregatedDelete = item.CanView && (
                relevantFormPerms.Count > 0
                    ? relevantFormPerms.Any(fp => fp.CanDelete)
                    : item.CanDelete);

            permissionDtos.Add(new RolePermissionDto
            {
                ModuleId = module.Id,
                ModuleName = ResolvePermissionModuleName(module.ModuleKey, module.ModuleName),
                CanView = item.CanView,
                CanCreate = aggregatedCreate,
                CanEdit = aggregatedEdit,
                CanDelete = aggregatedDelete,
                CanExport = item.CanView && item.CanExport,
                CanUpload = item.CanView && item.CanUpload
            });
        }

        await _roleRepository.ReplacePermissionsAsync(dto.RoleId, permissionDtos);
        await _moduleRepository.ReplaceFormPermissionsAsync(dto.RoleId, normalizedFormPermissions);
        await _roleRepository.SaveChangesAsync();

        return await GetRolePermissionsAsync(dto.RoleId);
    }

    private static RolePermissionDto? FindModulePermission(
        IReadOnlyList<RolePermissionDto> permissions,
        int moduleId,
        string moduleKey,
        string moduleName)
    {
        var byId = permissions.FirstOrDefault(p => p.ModuleId == moduleId);
        if (byId != null)
            return byId;

        if (!string.IsNullOrWhiteSpace(moduleKey))
        {
            var byKey = permissions.FirstOrDefault(p =>
                string.Equals(p.ModuleName, moduleKey, StringComparison.OrdinalIgnoreCase));
            if (byKey != null)
                return byKey;
        }

        return permissions.FirstOrDefault(p =>
            string.Equals(p.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolvePermissionModuleName(string moduleKey, string moduleName) =>
        !string.IsNullOrWhiteSpace(moduleKey) ? moduleKey : moduleName;
}
