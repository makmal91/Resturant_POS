namespace POSSystem.Application.Interfaces;

public interface ITenantContext
{
    int? UserId { get; }
    int? BusinessId { get; }
    int? BranchId { get; }
    bool IsMasterUser { get; }
    bool IsSuperAdmin { get; }
}