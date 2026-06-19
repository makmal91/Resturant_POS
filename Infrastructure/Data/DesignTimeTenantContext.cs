using POSSystem.Application.Interfaces;

namespace POSSystem.Infrastructure.Data;

/// <summary>
/// Minimal tenant context used only by EF Core design-time tooling (migrations).
/// </summary>
internal sealed class DesignTimeTenantContext : ITenantContext
{
    public int? UserId => 1;
    public int? BusinessId => 1;
    public int? BranchId => 1;
    public bool IsMasterUser => true;
    public bool IsSuperAdmin => true;
}
