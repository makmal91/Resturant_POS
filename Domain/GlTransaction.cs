namespace POSSystem.Domain;

/// <summary>
/// Immutable debit/credit journal line. <see cref="GroupId"/> ties balanced multi-line entries.
/// Only rows with <see cref="IsActive"/> = true affect balances and reports.
/// </summary>
public class GlTransaction
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int AccountId { get; set; }
    public int BranchId { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public GlTransactionType TransactionType { get; set; }
    /// <summary>Source document id (sale, purchase, payment, expense). Used for lookup only — not in balance filters.</summary>
    public int? ReferenceId { get; set; }
    public Guid GroupId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Chains original, reversal, and replacement journals for one business event.</summary>
    public Guid? OriginalGroupId { get; set; }

    /// <summary>On reversal lines, the <see cref="GroupId"/> of the journal being reversed.</summary>
    public Guid? ReversalOfGroupId { get; set; }

    /// <summary>When true, this line counts in reports and balances. Only one active version per source document.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Reversal journal line (audit trail; always inactive).</summary>
    public bool IsReversal { get; set; }

    public virtual GlAccount Account { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
