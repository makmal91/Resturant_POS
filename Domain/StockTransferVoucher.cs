namespace POSSystem.Domain;

public class StockTransferVoucher : BaseEntity
{
    public string TransferNo { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }
    public bool IsReversed { get; set; }
    public DateTime? ReversedAt { get; set; }
    public int? ReversedBy { get; set; }

    public virtual Warehouse FromWarehouse { get; set; } = null!;
    public virtual Warehouse ToWarehouse { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<StockTransferVoucherLine> Lines { get; set; } = new List<StockTransferVoucherLine>();
}

public class StockTransferVoucherLine : BaseEntity
{
    public int VoucherId { get; set; }
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int? UnitId { get; set; }
    public decimal UnitQuantity { get; set; }
    public decimal ConversionFactor { get; set; } = 1;
    /// <summary>Quantity in base unit.</summary>
    public decimal Quantity { get; set; }

    public virtual StockTransferVoucher Voucher { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual ProductVariant? Variant { get; set; }
    public virtual ProductUnit? Unit { get; set; }
}
