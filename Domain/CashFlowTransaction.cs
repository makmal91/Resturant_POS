namespace POSSystem.Domain;

/// <summary>
/// Records every money movement in the POS (sales, expenses, adjustments, transfers).
/// This is the immutable ledger — entries are never hard-deleted.
/// </summary>
public class CashFlowTransaction : BaseEntity
{
    public CashFlowTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public CashFlowPaymentMethod PaymentMethod { get; set; } = CashFlowPaymentMethod.Cash;

    /// <summary>FK to related SaleInvoice, Purchase, or manual entry.</summary>
    public int? ReferenceId { get; set; }

    /// <summary>Human-readable reference string (e.g. "INV-2026-0001").</summary>
    public string? ReferenceNo { get; set; }

    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public virtual Branch Branch { get; set; } = null!;
}
