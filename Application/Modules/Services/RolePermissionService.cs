using POSSystem.Application.Modules.DTOs;
using POSSystem.Application.Modules.Interfaces;
using POSSystem.Application.Users.DTOs;
using POSSystem.Application.Users.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Modules.Services;

public class RolePermissionService : IRolePermissionService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IModuleRepository _moduleRepository;

    public RolePermissionService(IRoleRepository roleRepository, IModuleRepository moduleRepository)
    {
        _roleRepository = roleRepository;
        _moduleRepository = moduleRepository;
    }

    public async Task<IReadOnlyList<ModulePermissionItemDto>> GetRolePermissionsAsync(int roleId)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null)
            throw new InvalidOperationException("Role not found.");

        var modules = await _moduleRepository.GetAssignableModulesAsync();
        var permissions = await _roleRepository.GetPermissionsAsync(roleId);
        var permissionMap = permissions.ToDictionary(
            p => p.ModuleName,
            p => p,
            StringComparer.OrdinalIgnoreCase);

        return modules
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.ModuleName)
            .Select(module =>
            {
                permissionMap.TryGetValue(module.ModuleName, out var permission);
                return new ModulePermissionItemDto
                {
                    ModuleId = module.Id,
                    ModuleName = module.ModuleName,
                    ModuleKey = module.ModuleKey,
                    ParentModuleId = module.ParentModuleId,
                    DisplayOrder = module.DisplayOrder,
                    CanView = permission?.CanView ?? false,
                    CanCreate = permission?.CanCreate ?? false,
                    CanEdit = permission?.CanEdit ?? false,
                    CanDelete = permission?.CanDelete ?? false,
                    CanExport = permission?.CanExport ?? false,
                    CanUpload = permission?.CanUpload ?? false
                };
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ModulePermissionItemDto>> SaveRolePermissionsAsync(SaveRolePermissionsRequestDto dto)
    {
        var role = await _roleRepository.GetTrackedByIdAsync(dto.RoleId);
        if (role == null)
            throw new InvalidOperationException("Role not found.");

        var permissionDtos = new List<RolePermissionDto>();

        foreach (var item in dto.Permissions)
        {
            var module = await _moduleRepository.GetByIdAsync(item.ModuleId);
            if (module == null)
                continue;

            permissionDtos.Add(new RolePermissionDto
            {
                ModuleId = module.Value.Id,
                ModuleName = module.Value.ModuleName,
                CanView = item.CanView,
                CanCreate = item.CanCreate,
                CanEdit = item.CanEdit,
                CanDelete = item.CanDelete,
                CanExport = item.CanExport,
                CanUpload = item.CanUpload
            });
        }

        await _roleRepository.ReplacePermissionsAsync(dto.RoleId, permissionDtos);
        await _roleRepository.SaveChangesAsync();

        return await GetRolePermissionsAsync(dto.RoleId);
    }
}
