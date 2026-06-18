namespace POSSystem.Domain;

public class StockLedger : BaseEntity
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int WarehouseId { get; set; }
    public StockLedgerType Type { get; set; }
    public int? ReferenceId { get; set; }
    public decimal QuantityInBaseUnit { get; set; }
    public int? UnitId { get; set; }
    public decimal? UnitQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Remarks { get; set; } = string.Empty;

    public virtual Product Product { get; set; } = null!;
    public virtual ProductVariant? Variant { get; set; }
    public virtual ProductUnit? Unit { get; set; }
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
