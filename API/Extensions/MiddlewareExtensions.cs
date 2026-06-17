using POSSystem.API.Middleware;

namespace POSSystem.API.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionMiddleware>();
    }

    public static IApplicationBuilder UseBranchAccessMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<BranchAccessMiddleware>();
    }

    public static IApplicationBuilder UsePermissionAuthorizationMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PermissionAuthorizationMiddleware>();
    }

    public static IApplicationBuilder UseLicenseGateMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<LicenseGateMiddleware>();
    }

    public static IApplicationBuilder UseLicenseEnforcementMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<LicenseEnforcementMiddleware>();
    }
}