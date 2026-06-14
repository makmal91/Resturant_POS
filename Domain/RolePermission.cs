namespace POSSystem.Domain;

public class RolePermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    public bool CanUpload { get; set; }

    public virtual Role Role { get; set; } = null!;
}
