using POSSystem.Application.Modules.DTOs;

namespace POSSystem.Application.Modules.Interfaces;

public interface IModuleRepository
{
    Task<IReadOnlyList<ModuleListItemDto>> GetAllAsync();
    Task<IReadOnlyList<(int Id, string ModuleKey, string ModuleName, int? ParentModuleId, int DisplayOrder)>> GetAssignableModulesAsync();
    Task<(int Id, string ModuleKey, string ModuleName)?> GetByIdAsync(int moduleId);
}

public interface IModuleService
{
    Task<IReadOnlyList<ModuleListItemDto>> GetModulesAsync();
}

public interface IRolePermissionService
{
    Task<IReadOnlyList<ModulePermissionItemDto>> GetRolePermissionsAsync(int roleId);
    Task<IReadOnlyList<ModulePermissionItemDto>> SaveRolePermissionsAsync(SaveRolePermissionsRequestDto dto);
}
