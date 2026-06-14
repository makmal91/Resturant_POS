using System.Security.Claims;
using POSSystem.Application.Interfaces;

namespace POSSystem.API.Extensions;

public class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? BusinessId =>
        TryReadIntClaim("businessId") ??
        TryReadIntClaim("BusinessId") ??
        TryReadIntHeader("X-Business-Id") ??
        TryReadIntQuery("businessId");

    public int? BranchId =>
        TryReadIntClaim("branchId") ??
        TryReadIntClaim("BranchId") ??
        TryReadIntHeader("X-Branch-Id") ??
        TryReadIntQuery("branchId");

    public bool IsSuperAdmin
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal == null)
                return false;

            return principal.IsInRole("SuperAdmin") ||
                   principal.Claims.Any(c =>
                       c.Type == ClaimTypes.Role &&
                       string.Equals(c.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase));
        }
    }

    private int? TryReadIntClaim(string claimType)
    {
        var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType);
        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : null;
    }

    private int? TryReadIntHeader(string headerName)
    {
        var value = _httpContextAccessor.HttpContext?.Request?.Headers[headerName].FirstOrDefault();
        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : null;
    }

    private int? TryReadIntQuery(string queryName)
    {
        var value = _httpContextAccessor.HttpContext?.Request?.Query[queryName].FirstOrDefault();
        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : null;
    }
}