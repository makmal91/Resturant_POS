namespace POSSystem.Application.Interfaces;

public interface ITenantContext
{
    int? BusinessId { get; }
    int? BranchId { get; }
    bool IsMasterUser { get; }
    bool IsSuperAdmin { get; }
}