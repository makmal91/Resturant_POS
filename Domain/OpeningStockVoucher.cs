namespace POSSystem.Domain;

public class OpeningStockVoucher : BaseEntity
{
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public int WarehouseId { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsReversed { get; set; }
    public DateTime? ReversedAt { get; set; }
    public int? ReversedBy { get; set; }
    /// <summary>Original voucher this entry replaces after an edit.</summary>
    public int? ReferenceVoucherId { get; set; }
    /// <summary>Replacement voucher created after reversing this entry for edit.</summary>
    public int? ReversalVoucherId { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual OpeningStockVoucher? ReferenceVoucher { get; set; }
    public virtual OpeningStockVoucher? ReversalVoucher { get; set; }
    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<OpeningStockVoucherLine> Lines { get; set; } = new List<OpeningStockVoucherLine>();
}

public class OpeningStockVoucherLine : BaseEntity
{
    public int VoucherId { get; set; }
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int? UnitId { get; set; }
    /// <summary>Quantity entered in the selected unit.</summary>
    public decimal UnitQuantity { get; set; }
    public decimal ConversionFactor { get; set; } = 1;
    /// <summary>Stock quantity stored in base unit (UnitQuantity × ConversionFactor).</summary>
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal TotalAmount { get; set; }

    public virtual OpeningStockVoucher Voucher { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual ProductVariant? Variant { get; set; }
    public virtual ProductUnit? Unit { get; set; }
}
