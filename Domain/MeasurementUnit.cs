namespace POSSystem.Domain;

public class MeasurementUnit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    /// <summary>Fallback conversion: base units in 1 of this unit. Overridable per product.</summary>
    public decimal DefaultConversionFactor { get; set; } = 1;
    public bool Status { get; set; } = true;

    public virtual Branch Branch { get; set; } = null!;
}
