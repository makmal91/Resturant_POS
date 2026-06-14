namespace POSSystem.Domain;

public class UserBranch
{
    public int UserId { get; set; }
    public int BranchId { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
