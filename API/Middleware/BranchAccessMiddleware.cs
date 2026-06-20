using System.Security.Claims;
using POSSystem.Domain;

namespace POSSystem.API.Middleware;

public class BranchAccessMiddleware
{
    private static readonly PathString[] ExcludedPathPrefixes =
    [
        new("/api/auth/login"),
        new("/api/health"),
        new("/swagger"),
        new("/notificationHub"),
        new("/orderHub")
    ];

    private readonly RequestDelegate _next;

    public BranchAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        if (!path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
            IsExcludedPath(path) ||
            !(context.User.Identity?.IsAuthenticated ?? false))
        {
            await _next(context);
            return;
        }

        var branchIdHeader = context.Request.Headers["X-Branch-Id"].FirstOrDefault();
        var role = context.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        if (!int.TryParse(branchIdHeader, out var branchId))
        {
            if (RoleNames.HasGlobalBranchAccess(role))
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = "X-Branch-Id header is required." });
            return;
        }

        if (RoleNames.HasGlobalBranchAccess(role))
        {
            await _next(context);
            return;
        }

        if (branchId <= 0)
        {
            if (RoleNames.HasGlobalBranchAccess(role))
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = "X-Branch-Id header is required." });
            return;
        }

        if (!HasBranchAccess(context.User, branchId))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "You do not have access to the selected branch." });
            return;
        }

        await _next(context);
    }

    private static bool IsExcludedPath(PathString path)
    {
        foreach (var excluded in ExcludedPathPrefixes)
        {
            if (path.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool HasBranchAccess(ClaimsPrincipal user, int branchId)
    {
        var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (RoleNames.IsMasterUser(role) || RoleNames.HasGlobalBranchAccess(role))
            return true;

        var branchIdsClaim = user.FindFirstValue("branchIds");
        if (string.IsNullOrWhiteSpace(branchIdsClaim))
            return false;

        return branchIdsClaim
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(value => int.TryParse(value, out var allowedBranchId) && allowedBranchId == branchId);
    }
}
