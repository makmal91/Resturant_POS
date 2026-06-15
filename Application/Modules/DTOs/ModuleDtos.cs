namespace POSSystem.Application.Modules.DTOs;

public class ModuleListItemDto
{
    public int Id { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty;
    public int? ParentModuleId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<ModuleListItemDto> Children { get; set; } = Array.Empty<ModuleListItemDto>();
}

public class ModulePermissionItemDto
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty;
    public int? ParentModuleId { get; set; }
    public int DisplayOrder { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    public bool CanUpload { get; set; }
}

public class SaveRolePermissionsRequestDto
{
    public int RoleId { get; set; }
    public IReadOnlyList<SaveRolePermissionItemDto> Permissions { get; set; } = Array.Empty<SaveRolePermissionItemDto>();
}

public class SaveRolePermissionItemDto
{
    public int ModuleId { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    public bool CanUpload { get; set; }
}
