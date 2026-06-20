using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Application.License.Interfaces;
using POSSystem.Infrastructure.Data;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    private readonly POSDbContext _db;
    private readonly ILicenseService _licenseService;

    public HealthController(POSDbContext db, ILicenseService licenseService)
    {
        _db = db;
        _licenseService = licenseService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var canConnect = false;
        string? dbError = null;
        var hasExceptionLogsTable = false;
        var hasUsers = false;

        try
        {
            canConnect = await _db.Database.CanConnectAsync(cancellationToken);
            if (canConnect)
            {
                hasExceptionLogsTable = await _db.Database
                    .SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'dbo.ExceptionLogs', N'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]")
                    .FirstOrDefaultAsync(cancellationToken) == 1;

                hasUsers = await _db.Users.IgnoreQueryFilters().AnyAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            dbError = ex.Message;
        }

        var license = _licenseService.GetStatus();

        return Ok(new
        {
            status = canConnect ? "ok" : "degraded",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            database = new
            {
                connected = canConnect,
                error = dbError,
                hasExceptionLogsTable,
                hasUsers
            },
            license = new
            {
                isValid = license.IsValid,
                isOperational = _licenseService.IsOperational,
                message = license.Message
            },
            serverTimeUtc = DateTime.UtcNow
        });
    }
}
