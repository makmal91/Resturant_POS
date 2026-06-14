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
        var claimValue = controller.User?.FindFirst("branchId")?.Value ?? controller.User?.FindFirst("BranchId")?.Value;
        if (int.TryParse(claimValue, out var claimBranchId) && claimBranchId > 0)
            return claimBranchId;

        var headerValue = controller.Request.Headers["X-Branch-Id"].FirstOrDefault();
        if (int.TryParse(headerValue, out var headerBranchId) && headerBranchId > 0)
            return headerBranchId;

        var queryValue = controller.Request.Query["branchId"].FirstOrDefault();
        if (int.TryParse(queryValue, out var queryBranchId) && queryBranchId > 0)
            return queryBranchId;

        if (fallback.HasValue && fallback.Value > 0)
            return fallback.Value;

        return 1;
    }
}