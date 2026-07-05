using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using POSSystem.API.Extensions;
using POSSystem.Application.Auth.DTOs;
using POSSystem.Application.Auth.Interfaces;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var username = request?.Username?.Trim() ?? string.Empty;

        try
        {
            if (request == null)
                return BadRequest(new { message = "Request body is required." });

            var response = await _authService.LoginAsync(request);
            _logger.LogInformation("Login succeeded for user {Username}.", username);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Login rejected for user {Username}: {Message}", username, ex.Message);
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions()
    {
        var roleIdValue = User.FindFirstValue("roleId");
        if (!int.TryParse(roleIdValue, out var roleId))
            return Unauthorized(new { message = "Role information is missing from the token." });

        var roleName = User.FindFirstValue(System.Security.Claims.ClaimTypes.Role) ?? string.Empty;
        var response = await _authService.GetCurrentUserPermissionsAsync(roleId, roleName);
        return Ok(response);
    }
}
