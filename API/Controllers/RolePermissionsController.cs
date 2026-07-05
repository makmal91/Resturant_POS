using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Auth.Exceptions;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Modules.DTOs;
using POSSystem.Application.Modules.Interfaces;
using POSSystem.API.Authorization;
using System.Security.Claims;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/role-permissions")]
public class RolePermissionsController : ControllerBase
{
    private readonly IRolePermissionService _rolePermissionService;

    public RolePermissionsController(IRolePermissionService rolePermissionService)
    {
        _rolePermissionService = rolePermissionService;
    }

    [HttpGet("{roleId:int}")]
    [RequirePermission(PermissionModules.Roles, PermissionActions.View)]
    public async Task<IActionResult> GetRolePermissions(int roleId)
    {
        try
        {
            var permissions = await _rolePermissionService.GetRolePermissionsAsync(roleId);
            return Ok(permissions);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    [RequirePermission(PermissionModules.Roles, PermissionActions.Edit)]
    public async Task<IActionResult> SaveRolePermissions([FromBody] SaveRolePermissionsRequestDto dto)
    {
        try
        {
            var permissions = await _rolePermissionService.SaveRolePermissionsAsync(
                dto,
                ResolveRoleId(),
                ResolveRoleName());
            return Ok(permissions);
        }
        catch (PermissionEscalationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    private string? ResolveRoleName() =>
        User?.FindFirst(ClaimTypes.Role)?.Value;

    private int? ResolveRoleId()
    {
        var claim = User?.FindFirstValue("roleId");
        return int.TryParse(claim, out var roleId) ? roleId : null;
    }
}
