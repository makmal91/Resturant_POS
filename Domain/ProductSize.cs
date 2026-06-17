namespace POSSystem.Domain;

public class ProductSize : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Branch Branch { get; set; } = null!;
}
