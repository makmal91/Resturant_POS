namespace POSSystem.Domain;

public class Purchase : BaseEntity
{
    public string InvoiceNo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Draft;
    public string Notes { get; set; } = string.Empty;
    public DateTime? VoidedAt    { get; set; }
    public string?  VoidedByName { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}

public class PurchaseItem : BaseEntity
{
    public int PurchaseId { get; set; }
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int UnitId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ConversionFactor { get; set; } = 1;
    public decimal BaseQuantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal TotalCost { get; set; }

    public virtual Purchase Purchase { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual ProductVariant? Variant { get; set; }
    public virtual ProductUnit Unit { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
