namespace POSSystem.Domain;

public class StockAdjustment : BaseEntity
{
    public string AdjustmentNo { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;
    public int WarehouseId { get; set; }
    public int AdjustmentTypeId { get; set; }
    public string? Remarks { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsReversed { get; set; }
    public DateTime? ReversedAt { get; set; }
    public int? ReversedBy { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual AdjustmentType AdjustmentType { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<StockAdjustmentLine> Lines { get; set; } = new List<StockAdjustmentLine>();
}

public class StockAdjustmentLine : BaseEntity
{
    public int StockAdjustmentId { get; set; }
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int UnitId { get; set; }
    /// <summary>Signed quantity in selected unit (+ gain, − loss).</summary>
    public decimal UnitQuantity { get; set; }
    public decimal ConversionFactor { get; set; } = 1;
    /// <summary>Signed quantity in base unit.</summary>
    public decimal BaseQuantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal TotalCost { get; set; }

    public virtual StockAdjustment StockAdjustment { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual ProductVariant? Variant { get; set; }
    public virtual ProductUnit Unit { get; set; } = null!;
}
