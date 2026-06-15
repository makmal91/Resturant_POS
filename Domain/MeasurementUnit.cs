namespace POSSystem.Domain;

public class MeasurementUnit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; } = 1;
    public bool Status { get; set; } = true;

    public virtual Branch Branch { get; set; } = null!;
}
