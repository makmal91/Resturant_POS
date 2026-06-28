namespace POSSystem.Application.Interfaces;

public interface ITenantContext
{
    int? UserId { get; }
    int? RoleId { get; }
    string? RoleName { get; }
    int? BusinessId { get; }
    int? BranchId { get; }
    bool IsMasterUser { get; }
    bool IsSuperAdmin { get; }
}