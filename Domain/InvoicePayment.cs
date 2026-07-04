namespace POSSystem.Domain;

/// <summary>
/// Payment against a sale/purchase invoice, or an advance payment with no invoice linked.
/// Distinct from restaurant <see cref="Payment"/> (order payments).
/// </summary>
public enum InvoicePaymentModule
{
    Sale = 1,
    Purchase = 2
}

public enum PartyPaymentType
{
    Cash = 1,
    Bank = 2,
    Online = 3
}

/// <summary>
/// Business purpose of a party ledger payment (against invoice, advance, or adjustment).
/// </summary>
public enum InvoicePaymentCategory
{
    AgainstInvoice = 1,
    Advance = 2,
    Adjustment = 3,
    // Cash/card tendered at POS sale time. The cash leg is already booked by the
    // sale journal (PostSaleAsync), so these payments must NOT create a GL receipt.
    PosSale = 4
}

public class InvoicePayment : BaseEntity
{
    public InvoicePaymentModule Module { get; set; }
    public int? SaleInvoiceId { get; set; }
    public int? PurchaseId { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public PartyPaymentType PaymentType { get; set; } = PartyPaymentType.Cash;
    public InvoicePaymentCategory Category { get; set; } = InvoicePaymentCategory.AgainstInvoice;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string ReferenceNo { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsReversed { get; set; }
    public int? OriginalPaymentId { get; set; }
    public int? ReversedBy { get; set; }
    public DateTime? ReversedAt { get; set; }
    public int? DeletedBy { get; set; }

    public virtual SaleInvoice? SaleInvoice { get; set; }
    public virtual Purchase? Purchase { get; set; }
    public virtual Customer? Customer { get; set; }
    public virtual Supplier? Supplier { get; set; }
    public virtual ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
}
