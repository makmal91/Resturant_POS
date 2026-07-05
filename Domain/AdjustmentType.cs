namespace POSSystem.Domain;

public class AdjustmentType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int ExpenseAccountId { get; set; }
    public int IncomeAccountId { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual GlAccount ExpenseAccount { get; set; } = null!;
    public virtual GlAccount IncomeAccount { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
