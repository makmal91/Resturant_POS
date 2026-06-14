using System.Security.Claims;
using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Domain;

namespace POSSystem.API.Middleware;

public class PermissionAuthorizationMiddleware
{
    private static readonly PathString[] ExcludedPathPrefixes =
    [
        new("/api/auth/login"),
        new("/swagger"),
        new("/notificationHub"),
        new("/orderHub")
    ];

    private readonly RequestDelegate _next;

    public PermissionAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IPermissionService permissionService)
    {
        var path = context.Request.Path;

        if (!path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
            IsExcludedPath(path) ||
            !(context.User.Identity?.IsAuthenticated ?? false))
        {
            await _next(context);
            return;
        }

        var mapping = Authorization.ApiPermissionMapper.Resolve(context);
        if (mapping == null)
        {
            await _next(context);
            return;
        }

        var roleIdClaim = context.User.FindFirstValue("roleId");
        var roleName = context.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        if (RoleNames.IsMasterUser(roleName))
        {
            await _next(context);
            return;
        }

        if (!int.TryParse(roleIdClaim, out var roleId))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "Role information is missing from the token." });
            return;
        }

        var (module, action) = mapping.Value;

        if (!await permissionService.HasPermissionAsync(roleId, roleName, module, action))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                message = $"You do not have {action} permission for {module}."
            });
            return;
        }

        if (IsUploadRequest(context) &&
            !permissionService.IsBypassRole(roleName) &&
            !await permissionService.HasPermissionAsync(roleId, roleName, module, PermissionActions.Upload))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                message = $"You do not have Upload permission for {module}."
            });
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

    private static bool IsUploadRequest(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.Contains("/image", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/logo", StringComparison.OrdinalIgnoreCase))
        {
            return context.Request.Method is "POST" or "PUT" or "PATCH";
        }

        return context.Request.HasFormContentType &&
               context.Request.Form.Files.Count > 0 &&
               context.Request.Method is "POST" or "PUT" or "PATCH";
    }
}
