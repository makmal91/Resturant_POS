namespace POSSystem.Domain;

public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public byte[]? ImageData { get; set; }
    public string? ImageContentType { get; set; }
    public string? ImageFileName { get; set; }
    public bool Status { get; set; } = true;

    public virtual Branch Branch { get; set; } = null!;
}
