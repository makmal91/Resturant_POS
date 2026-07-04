namespace POSSystem.Domain;

/// <summary>Open/close session for a <see cref="PosRegister"/>.</summary>
public class RegisterSession : BaseEntity
{
    public int PosRegisterId { get; set; }
    public DateTime SessionDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsOpeningOverride { get; set; }
    public string? OpeningOverrideReason { get; set; }
    public int? OpenedBy { get; set; }
    public DateTime OpenedAt { get; set; }

    public decimal? ExpectedClosing { get; set; }
    public decimal? PhysicalCash { get; set; }
    public decimal? Difference { get; set; }
    public decimal TotalCashSales { get; set; }
    public decimal TotalExpensesCash { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalAdjustments { get; set; }

    public bool IsClosed { get; set; }
    public int? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? CloseMismatchReason { get; set; }
    public string? Notes { get; set; }

    public virtual PosRegister PosRegister { get; set; } = null!;
}
