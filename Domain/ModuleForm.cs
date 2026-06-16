namespace POSSystem.Domain;

public class ModuleForm
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string FormName { get; set; } = string.Empty;
    public string FormCode { get; set; } = string.Empty;
    public string? Route { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public virtual PermissionModule Module { get; set; } = null!;
    public virtual ICollection<RoleFormPermission> RoleFormPermissions { get; set; } = new List<RoleFormPermission>();
}
