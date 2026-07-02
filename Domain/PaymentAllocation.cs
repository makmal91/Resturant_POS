namespace POSSystem.Domain;

/// <summary>
/// Links an <see cref="InvoicePayment"/> to one or more sale/purchase invoices.
/// </summary>
public class PaymentAllocation : BaseEntity
{
    public int InvoicePaymentId { get; set; }
    public int? SaleInvoiceId { get; set; }
    public int? PurchaseId { get; set; }
    public decimal AppliedAmount { get; set; }

    public virtual InvoicePayment InvoicePayment { get; set; } = null!;
    public virtual SaleInvoice? SaleInvoice { get; set; }
    public virtual Purchase? Purchase { get; set; }
}

public enum InvoiceSettlementStatus
{
    Pending = 0,
    Partial = 1,
    Paid = 2
}
