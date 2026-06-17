using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.Application.License.Interfaces;
using POSSystem.Domain;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LicensesController : ControllerBase
{
    private const long MaxLicenseFileSizeBytes = 256 * 1024;
    private readonly ILicenseService _licenseService;
    private readonly ILicenseUsageProvider _usageProvider;

    public LicensesController(ILicenseService licenseService, ILicenseUsageProvider usageProvider)
    {
        _licenseService = licenseService;
        _usageProvider = usageProvider;
    }

    [AllowAnonymous]
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(_licenseService.GetStatus());
    }

    [Authorize]
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage(CancellationToken cancellationToken)
    {
        if (!IsSystemAdmin())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only System Admin can view license usage." });

        var usage = await _usageProvider.GetUsageSnapshotAsync(cancellationToken);
        return Ok(usage);
    }

    [Authorize]
    [HttpPost("upload")]
    [RequestSizeLimit(MaxLicenseFileSizeBytes)]
    public async Task<IActionResult> UploadLicense(IFormFile? file, CancellationToken cancellationToken)
    {
        if (!IsSystemAdmin())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only System Admin can upload a license." });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "License file (.lic) is required." });

        if (!string.Equals(Path.GetExtension(file.FileName), ".lic", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only .lic files are supported." });

        if (file.Length > MaxLicenseFileSizeBytes)
            return BadRequest(new { message = "License file is too large." });

        try
        {
            await using var stream = file.OpenReadStream();
            await _licenseService.InstallLicenseFileAsync(stream, cancellationToken);

            var status = _licenseService.GetStatus();
            if (!status.IsValid || status.IsExpired)
            {
                return BadRequest(new
                {
                    message = status.Message ?? "Uploaded license is invalid or expired.",
                    licenseStatus = status
                });
            }

            var usage = await _usageProvider.GetUsageSnapshotAsync(cancellationToken);
            return Ok(new
            {
                message = "License installed successfully.",
                licenseStatus = status,
                usage
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("reload")]
    public async Task<IActionResult> ReloadLicense(CancellationToken cancellationToken)
    {
        if (!IsSystemAdmin())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only System Admin can reload the license." });

        await _licenseService.ReloadAsync(cancellationToken);
        return Ok(new
        {
            message = "License cache reloaded.",
            licenseStatus = _licenseService.GetStatus()
        });
    }

    private bool IsSystemAdmin()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        return RoleNames.IsMasterUser(role ?? string.Empty);
    }
}
