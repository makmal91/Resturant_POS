namespace POSSystem.Domain;

/// <summary>
/// Tracks daily opening and closing cash per branch.
/// One record per branch per calendar date.
/// </summary>
public class CashRegister : BaseEntity
{
    public DateTime RegisterDate { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal? ClosingCash { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? ActualCash { get; set; }

    /// <summary>Positive = over, negative = short.</summary>
    public decimal? Difference { get; set; }

    public bool IsClosed { get; set; }
    public string? Notes { get; set; }
    public int? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;
}
