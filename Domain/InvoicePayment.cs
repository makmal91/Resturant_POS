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

public class InvoicePayment : BaseEntity
{
    public InvoicePaymentModule Module { get; set; }
    public int? SaleInvoiceId { get; set; }
    public int? PurchaseId { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public PartyPaymentType PaymentType { get; set; } = PartyPaymentType.Cash;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string ReferenceNo { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public virtual SaleInvoice? SaleInvoice { get; set; }
    public virtual Purchase? Purchase { get; set; }
    public virtual Customer? Customer { get; set; }
    public virtual Supplier? Supplier { get; set; }
}
