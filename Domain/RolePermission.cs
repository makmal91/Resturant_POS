namespace POSSystem.Domain;

public class RolePermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int? ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    public bool CanUpload { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public virtual Role Role { get; set; } = null!;
    public virtual PermissionModule? Module { get; set; }
}
