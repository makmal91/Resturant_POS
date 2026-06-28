using POSSystem.Application.Modules.DTOs;

namespace POSSystem.Application.Modules.Interfaces;

public interface IModuleRepository
{
    Task<IReadOnlyList<ModuleListItemDto>> GetAllAsync();
    Task<IReadOnlyList<ModuleListItemDto>> GetSidebarModulesAsync();
    Task<IReadOnlyList<(int Id, string ModuleKey, string ModuleName, int? ParentModuleId, int DisplayOrder, string? Route, string? Icon)>> GetAllModulesFlatAsync();
    Task<IReadOnlyList<(int Id, string ModuleKey, string ModuleName, int? ParentModuleId, int DisplayOrder)>> GetAssignableModulesAsync();
    Task<(int Id, string ModuleKey, string ModuleName)?> GetByIdAsync(int moduleId);
    Task<IReadOnlyList<(int Id, int ModuleId, string FormName, string FormCode, string? Route, int SortOrder)>> GetAllFormsAsync();
    Task<IReadOnlyList<FormPermissionItemDto>> GetFormPermissionsForRoleAsync(int roleId);
    Task<IReadOnlyList<string>> GetEnabledFeatureKeysForRoleAsync(int roleId);
    Task ReplaceFormPermissionsAsync(int roleId, IReadOnlyList<SaveFormPermissionItemDto> formPermissions);
}

public interface IModuleService
{
    Task<IReadOnlyList<ModuleListItemDto>> GetModulesAsync();
}

public interface IRolePermissionService
{
    Task<IReadOnlyList<ModulePermissionItemDto>> GetRolePermissionsAsync(int roleId);
    Task<IReadOnlyList<ModulePermissionItemDto>> SaveRolePermissionsAsync(SaveRolePermissionsRequestDto dto, string? actorRoleName = null);
}
