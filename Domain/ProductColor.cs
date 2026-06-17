namespace POSSystem.Domain;

public class ProductColor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? HexCode { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Branch Branch { get; set; } = null!;
}
