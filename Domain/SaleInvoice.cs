namespace POSSystem.Domain;

public enum SaleInvoiceStatus
{
    Draft = 0,
    Completed = 1,
    Held = 2,
    Cancelled = 3,
    Returned  = 4,
    Voided    = 5   // invoice was voided after completion; stock reversed via ledger
}

public enum SalePaymentMethod
{
    Cash = 1,
    Card = 2,
    Mixed = 3
}

public enum PricingType
{
    Retail = 1,
    Wholesale = 2
}

public class SaleInvoice : BaseEntity
{
    public string InvoiceNo { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public int WarehouseId { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ReturnAmount { get; set; }
    public SalePaymentMethod PaymentMethod { get; set; } = SalePaymentMethod.Cash;
    public decimal CardAmount { get; set; }
    public decimal CashAmount { get; set; }
    public SaleInvoiceStatus Status { get; set; } = SaleInvoiceStatus.Draft;
    public PricingType PricingType { get; set; } = PricingType.Retail;
    public string? Notes { get; set; }
    public string? HeldNote { get; set; }
    public string? CashierName   { get; set; }
    public DateTime? VoidedAt    { get; set; }
    public string?  VoidedByName { get; set; }

    public virtual Customer? Customer { get; set; }
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<SaleInvoiceItem> Items { get; set; } = new List<SaleInvoiceItem>();
}

public class SaleInvoiceItem : BaseEntity
{
    public int SaleInvoiceId { get; set; }
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int UnitId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ConversionFactor { get; set; } = 1;  // unit conversion factor at time of sale
    public decimal BaseQuantity { get; set; }            // Quantity * ConversionFactor → base-unit qty for stock ledger
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? ItemNote { get; set; }

    public virtual SaleInvoice SaleInvoice { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual ProductVariant? Variant { get; set; }
    public virtual ProductUnit Unit { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
