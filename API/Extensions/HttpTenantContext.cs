using System.Security.Claims;
using POSSystem.Application.Interfaces;
using POSSystem.Domain;

namespace POSSystem.API.Extensions;

public class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId =>
        TryReadIntClaim(ClaimTypes.NameIdentifier) ??
        TryReadIntClaim("userId") ??
        TryReadIntClaim("UserId");

    public int? RoleId =>
        TryReadIntClaim("roleId") ??
        TryReadIntClaim("RoleId");

    public string? RoleName => ResolveRoleName();

    public int? BusinessId =>
        TryReadIntClaim("businessId") ??
        TryReadIntClaim("BusinessId") ??
        TryReadIntHeader("X-Business-Id") ??
        TryReadIntQuery("businessId");

    public int? BranchId =>
        // X-Branch-Id header takes priority: it represents the user's ACTIVE branch selection
        // (0 = All Branches, >0 = specific branch).
        // JWT branchId claim only holds the primary/default branch at login time.
        TryReadIntHeader("X-Branch-Id") ??
        TryReadIntQuery("branchId") ??
        TryReadIntClaim("branchId") ??
        TryReadIntClaim("BranchId");

    public bool IsMasterUser
    {
        get
        {
            var roleName = ResolveRoleName();
            return RoleNames.IsMasterUser(roleName ?? string.Empty);
        }
    }

    public bool IsSuperAdmin
    {
        get
        {
            if (IsMasterUser)
                return true;

            var roleName = ResolveRoleName();
            return IsGlobalAdminRole(roleName);
        }
    }

    private string? ResolveRoleName()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal == null)
            return _httpContextAccessor.HttpContext?.Request?.Headers["X-User-Role"].FirstOrDefault();

        if (principal.IsInRole(RoleNames.SystemAdmin))
            return RoleNames.SystemAdmin;

        if (principal.IsInRole("SuperAdmin") || principal.IsInRole(RoleNames.SuperAdmin))
            return RoleNames.SuperAdmin;

        return principal.FindFirstValue(ClaimTypes.Role);
    }

    private static bool IsGlobalAdminRole(string? roleName) =>
        string.Equals(roleName, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(roleName, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(roleName, RoleNames.SystemAdmin, StringComparison.OrdinalIgnoreCase);

    private int? TryReadIntClaim(string claimType)
    {
        var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType);
        return int.TryParse(value, out var parsed) && parsed >= 0 ? parsed : null;
    }

    private int? TryReadIntHeader(string headerName)
    {
        var value = _httpContextAccessor.HttpContext?.Request?.Headers[headerName].FirstOrDefault();
        return int.TryParse(value, out var parsed) && parsed >= 0 ? parsed : null;
    }

    private int? TryReadIntQuery(string queryName)
    {
        var value = _httpContextAccessor.HttpContext?.Request?.Query[queryName].FirstOrDefault();
        return int.TryParse(value, out var parsed) && parsed >= 0 ? parsed : null;
    }
}