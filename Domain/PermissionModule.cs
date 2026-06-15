namespace POSSystem.Domain;

public class PermissionModule
{
    public int Id { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty;
    public int? ParentModuleId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public virtual PermissionModule? ParentModule { get; set; }
    public virtual ICollection<PermissionModule> ChildModules { get; set; } = new List<PermissionModule>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
