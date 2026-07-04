namespace POSSystem.Domain;

/// <summary>Physical cash drawer / counter linked to a GL cash sub-account.</summary>
public class PosRegister : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int LinkedCashAccountId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }

    public virtual GlAccount LinkedCashAccount { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<RegisterSession> Sessions { get; set; } = new List<RegisterSession>();
}
