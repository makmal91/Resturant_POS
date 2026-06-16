namespace POSSystem.Domain;

public class ExpenseCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Status { get; set; } = true;

    public virtual Branch Branch { get; set; } = null!;
}
