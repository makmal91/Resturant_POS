using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DatabaseController : ControllerBase
{
    private readonly POSDbContext _context;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(POSDbContext context, ILogger<DatabaseController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Authorize]
    [HttpPost("seed")]
    public async Task<IActionResult> RunSeed(CancellationToken cancellationToken)
    {
        if (!IsSystemAdmin())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only System Admin can run database seed." });

        var report = await DatabaseBootstrapper.RunSeedAsync(_context, _logger);
        return Ok(new
        {
            message = report.IsComplete ? "Database seed completed." : "Database seed finished with warnings.",
            report.IsComplete,
            report.Warnings,
            report.Counts,
            report.MissingModuleKeys
        });
    }

    [Authorize]
    [HttpGet("seed-status")]
    public async Task<IActionResult> GetSeedStatus(CancellationToken cancellationToken)
    {
        if (!IsSystemAdmin())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only System Admin can view seed status." });

        var report = await DatabaseSeedVerifier.VerifyAsync(_context, _logger);
        return Ok(report);
    }

    [Authorize]
    [HttpPost("schema-patches")]
    public async Task<IActionResult> RunSchemaPatches(CancellationToken cancellationToken)
    {
        if (!IsSystemAdmin())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only System Admin can run schema patches." });

        await DatabaseBootstrapper.RunSchemaPatchesAsync(_context, _logger);
        return Ok(new { message = "Schema patches completed." });
    }

    private bool IsSystemAdmin()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        return RoleNames.IsMasterUser(role ?? string.Empty);
    }
}
