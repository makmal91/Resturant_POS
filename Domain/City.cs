namespace POSSystem.Domain;

public class City
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Country Country { get; set; } = null!;
}
