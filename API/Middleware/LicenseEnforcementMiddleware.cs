using System.Text.Json;
using POSSystem.Application.License.Interfaces;

namespace POSSystem.API.Middleware;

public sealed class LicenseEnforcementMiddleware
{
    private static readonly HashSet<string> CreatePaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/businesses",
        "/api/branches",
        "/api/users"
    };

    private readonly RequestDelegate _next;

    public LicenseEnforcementMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILicenseEnforcementService enforcementService)
    {
        if (!ShouldEnforce(context))
        {
            await _next(context);
            return;
        }

        try
        {
            var operation = ResolveOperation(context.Request.Path);
            var businessId = await ResolveBusinessIdAsync(context, operation);
            await enforcementService.EnsureCanCreateAsync(operation, businessId, context.RequestAborted);
        }
        catch (InvalidOperationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
            return;
        }

        await _next(context);
    }

    private static bool ShouldEnforce(HttpContext context)
    {
        if (!string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
            return false;

        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            return false;

        var normalized = path.Split('?', 2)[0].TrimEnd('/');
        return CreatePaths.Contains(normalized);
    }

    private static LicenseCreateOperation ResolveOperation(PathString path)
    {
        var segment = path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ElementAtOrDefault(1) ?? string.Empty;

        return segment.ToLowerInvariant() switch
        {
            "businesses" => LicenseCreateOperation.Business,
            "branches" => LicenseCreateOperation.Branch,
            "users" => LicenseCreateOperation.User,
            _ => throw new InvalidOperationException("Unsupported license enforcement route.")
        };
    }

    private static async Task<int?> ResolveBusinessIdAsync(HttpContext context, LicenseCreateOperation operation)
    {
        if (operation == LicenseCreateOperation.Business)
            return null;

        var headerBusinessId = context.Request.Headers["X-Business-Id"].FirstOrDefault();
        if (int.TryParse(headerBusinessId, out var parsedHeaderId) && parsedHeaderId > 0)
            return parsedHeaderId;

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(context.RequestAborted);
        context.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
            return null;

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        if (TryReadInt(root, "businessId", out var businessId))
            return businessId;

        if (TryReadInt(root, "companyId", out var companyId))
            return companyId;

        if (operation == LicenseCreateOperation.User &&
            context.Request.Query.TryGetValue("businessId", out var queryBusinessId) &&
            int.TryParse(queryBusinessId, out var parsedQueryBusinessId))
        {
            return parsedQueryBusinessId;
        }

        return null;
    }

    private static bool TryReadInt(JsonElement root, string propertyName, out int value)
    {
        value = 0;
        if (!root.TryGetProperty(propertyName, out var property))
            return false;

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value))
            return value > 0;

        if (property.ValueKind == JsonValueKind.String &&
            int.TryParse(property.GetString(), out value))
        {
            return value > 0;
        }

        return false;
    }
}
