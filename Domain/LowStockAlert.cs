namespace POSSystem.Domain;

public class LowStockAlert : BaseEntity
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int WarehouseId { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal AlertLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime LastTriggeredAt { get; set; } = DateTime.UtcNow;

    public virtual Product Product { get; set; } = null!;
    public virtual ProductVariant? Variant { get; set; }
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
