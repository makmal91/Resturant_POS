namespace POSSystem.Domain;

/// <summary>
/// Global chart-of-accounts entry (structure only — no branch or business ownership).
/// Balances are derived from <see cref="GlTransaction"/> rows filtered by branch.
/// </summary>
public class GlAccount
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public int? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }

    public virtual GlAccount? Parent { get; set; }
    public virtual ICollection<GlAccount> Children { get; set; } = new List<GlAccount>();
}
