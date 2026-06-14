namespace POSSystem.Domain;

public class AppMenu
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Icon { get; set; }
    public string? ModuleName { get; set; }
    public int? ParentId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual AppMenu? Parent { get; set; }
    public virtual ICollection<AppMenu> Children { get; set; } = new List<AppMenu>();
}
