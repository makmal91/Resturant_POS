using POSSystem.Application.Interfaces;

namespace POSSystem.Application.Common.Extensions;

/// <summary>
/// Centralized branch context helpers for multi-branch filtering.
/// BranchId 0 = All Branches (no filter). BranchId &gt; 0 = specific branch.
/// </summary>
public static class TenantBranchExtensions
{
    /// <summary>
    /// Returns the active branch for scoped operations, or null in All Branches mode.
    /// </summary>
    public static int? GetCurrentBranchId(this ITenantContext context)
    {
        if (!context.BranchId.HasValue || context.BranchId.Value <= 0)
            return null;

        return context.BranchId.Value;
    }

    public static bool IsAllBranchesMode(this ITenantContext context) =>
        context.BranchId == 0;

    /// <summary>
    /// Resolves branch filter for queries: explicit &gt; header/context &gt; 0 (all).
    /// </summary>
    public static int ResolveBranchFilter(this ITenantContext context, int? explicitBranchId = null)
    {
        if (explicitBranchId.HasValue && explicitBranchId.Value > 0)
            return explicitBranchId.Value;

        return context.BranchId ?? 0;
    }

    /// <summary>
    /// Whether an entity matches the branch filter (0 = all branches).
    /// </summary>
    public static bool MatchesBranchFilter(int entityBranchId, int filterBranchId) =>
        filterBranchId == 0 || entityBranchId == filterBranchId;
}
