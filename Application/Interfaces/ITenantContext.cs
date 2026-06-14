namespace POSSystem.Application.Interfaces;

public interface ITenantContext
{
    int? BusinessId { get; }
    int? BranchId { get; }
    bool IsSuperAdmin { get; }
}