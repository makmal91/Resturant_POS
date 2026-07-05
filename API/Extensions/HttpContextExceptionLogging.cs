using System.Security.Claims;
using POSSystem.Application.Common.Interfaces;

namespace POSSystem.API.Extensions;

public static class HttpContextExceptionLogging
{
    public static Task LogAsync(HttpContext context, Exception ex)
    {
        var service = context.RequestServices.GetRequiredService<IExceptionLogService>();
        return service.LogAsync(
            ex,
            GetUserId(context),
            GetBranchId(context),
            context.Request.Headers["x-module"].FirstOrDefault(),
            context.Request.Headers["x-form"].FirstOrDefault(),
            context.Request.Headers["x-action"].FirstOrDefault());
    }

    private static long? GetUserId(HttpContext context)
    {
        var value =
            context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
            context.User?.FindFirstValue("userId") ??
            context.User?.FindFirstValue("UserId");

        return long.TryParse(value, out var parsed) && parsed > 0 ? parsed : null;
    }

    private static long? GetBranchId(HttpContext context)
    {
        var headerValue = context.Request.Headers["X-Branch-Id"].FirstOrDefault();
        if (long.TryParse(headerValue, out var headerBranchId) && headerBranchId > 0)
            return headerBranchId;

        var claimValue =
            context.User?.FindFirstValue("branchId") ??
            context.User?.FindFirstValue("BranchId");

        return long.TryParse(claimValue, out var parsed) && parsed > 0 ? parsed : null;
    }
}
