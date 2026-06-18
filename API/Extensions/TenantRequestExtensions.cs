using Microsoft.AspNetCore.Mvc;

namespace POSSystem.API.Extensions;

public static class TenantRequestExtensions
{
    public static int ResolveBusinessId(this ControllerBase controller, int? fallback = null)
    {
        var claimValue = controller.User?.FindFirst("businessId")?.Value ?? controller.User?.FindFirst("BusinessId")?.Value;
        if (int.TryParse(claimValue, out var claimBusinessId) && claimBusinessId > 0)
            return claimBusinessId;

        var headerValue = controller.Request.Headers["X-Business-Id"].FirstOrDefault();
        if (int.TryParse(headerValue, out var headerBusinessId) && headerBusinessId > 0)
            return headerBusinessId;

        var queryValue = controller.Request.Query["businessId"].FirstOrDefault();
        if (int.TryParse(queryValue, out var queryBusinessId) && queryBusinessId > 0)
            return queryBusinessId;

        if (fallback.HasValue && fallback.Value > 0)
            return fallback.Value;

        return 1;
    }

    public static int ResolveBranchId(this ControllerBase controller, int? fallback = null)
    {
        // X-Branch-Id header represents the user's ACTIVE selection (0 = All Branches).
        // It must take priority over the JWT claim (which only stores the primary branch at login).
        var headerValue = controller.Request.Headers["X-Branch-Id"].FirstOrDefault();
        if (int.TryParse(headerValue, out var headerBranchId) && headerBranchId >= 0)
            return headerBranchId;

        if (fallback.HasValue && fallback.Value >= 0)
            return fallback.Value;

        var queryValue = controller.Request.Query["branchId"].FirstOrDefault();
        if (int.TryParse(queryValue, out var queryBranchId) && queryBranchId >= 0)
            return queryBranchId;

        var claimValue = controller.User?.FindFirst("branchId")?.Value ?? controller.User?.FindFirst("BranchId")?.Value;
        if (int.TryParse(claimValue, out var claimBranchId) && claimBranchId >= 0)
            return claimBranchId;

        return 1;
    }

    /// <summary>
    /// Returns the active branch for scoped operations, or null in All Branches mode.
    /// Mirrors frontend getCurrentBranchId().
    /// </summary>
    public static int? GetCurrentBranchId(this ControllerBase controller)
    {
        var resolved = controller.ResolveBranchId(null);
        return resolved > 0 ? resolved : null;
    }

    /// <summary>
    /// Resolves a concrete branch for branch-scoped operations (e.g. code sequences).
    /// Explicit query/body branch wins over the global "All Branches" header (0).
    /// </summary>
    public static int? ResolveEffectiveBranchId(this ControllerBase controller, int? explicitBranchId = null)
    {
        if (explicitBranchId.HasValue && explicitBranchId.Value > 0)
            return explicitBranchId.Value;

        var resolved = controller.ResolveBranchId(null);
        return resolved > 0 ? resolved : null;
    }

    public static int? ResolveUserId(this ControllerBase controller)
    {
        var value = controller.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? controller.User?.FindFirst("userId")?.Value
            ?? controller.User?.FindFirst("UserId")?.Value;
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }
}