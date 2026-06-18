namespace POSSystem.Application.Modules.DTOs;

public class ModuleListItemDto
{
    public int Id { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty;
    public int? ParentModuleId { get; set; }
    public string? Route { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<ModuleListItemDto> Children { get; set; } = Array.Empty<ModuleListItemDto>();
    public IReadOnlyList<ModuleFormDto> Forms { get; set; } = Array.Empty<ModuleFormDto>();
}

public class ModuleFormDto
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string FormName { get; set; } = string.Empty;
    public string FormCode { get; set; } = string.Empty;
    public string? Route { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class ModulePermissionItemDto
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty;
    public int? ParentModuleId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsViewOnly { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    public bool CanUpload { get; set; }
    public IReadOnlyList<FormPermissionItemDto> Forms { get; set; } = Array.Empty<FormPermissionItemDto>();
}

public class FormPermissionItemDto
{
    public int FormId { get; set; }
    public int ModuleId { get; set; }
    public string FormName { get; set; } = string.Empty;
    public string FormCode { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class SaveRolePermissionsRequestDto
{
    public int RoleId { get; set; }
    public IReadOnlyList<SaveRolePermissionItemDto> Permissions { get; set; } = Array.Empty<SaveRolePermissionItemDto>();
    public IReadOnlyList<SaveFormPermissionItemDto> FormPermissions { get; set; } = Array.Empty<SaveFormPermissionItemDto>();
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

public class SaveFormPermissionItemDto
{
    public int FormId { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
