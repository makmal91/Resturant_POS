namespace POSSystem.Domain;

public class ExpenseCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Status { get; set; } = true;
    public int? GlAccountId { get; set; }

    public virtual Branch Branch { get; set; } = null!;
    public virtual GlAccount? GlAccount { get; set; }
}
