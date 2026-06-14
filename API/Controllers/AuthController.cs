using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using POSSystem.Application.Auth.DTOs;
using POSSystem.Application.Auth.Interfaces;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
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

        var permissions = await _authService.GetCurrentUserPermissionsAsync(roleId);
        return Ok(permissions);
    }
}
