namespace POSSystem.Application.Users.DTOs;

public class RoleListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class RoleDetailDto : RoleListItemDto
{
    public IReadOnlyList<RolePermissionDto> Permissions { get; set; } = Array.Empty<RolePermissionDto>();
}

public class RolePermissionDto
{
    public int Id { get; set; }
    public int? ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    public bool CanUpload { get; set; }
}

public class CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UpdateRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UpdateRolePermissionsDto
{
    public IReadOnlyList<RolePermissionDto> Permissions { get; set; } = Array.Empty<RolePermissionDto>();
}
