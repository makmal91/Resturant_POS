using POSSystem.Application.License.Interfaces;
using POSSystem.Domain;

namespace POSSystem.API.Middleware;

public sealed class LicenseGateMiddleware
{
    private static readonly PathString[] ExcludedPathPrefixes =
    [
        new("/api/auth/login"),
        new("/api/health"),
        new("/api/licenses/status"),
        new("/swagger"),
        new("/notificationHub"),
        new("/orderHub")
    ];

    private readonly RequestDelegate _next;

    public LicenseGateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILicenseService licenseService)
    {
        var path = context.Request.Path;

        if (!path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
            IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        if (IsLicenseUploadRequest(context))
        {
            await _next(context);
            return;
        }

        if (licenseService.IsOperational)
        {
            await _next(context);
            return;
        }

        var status = licenseService.GetStatus();
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            message = status.Message ?? "System license is invalid or expired.",
            licenseStatus = status
        });
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

    private static bool IsLicenseUploadRequest(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/licenses/upload", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
            return false;

        var role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        return RoleNames.IsMasterUser(role ?? string.Empty);
    }
}
