using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Modules.DTOs;
using POSSystem.Application.Modules.Interfaces;
using POSSystem.API.Authorization;

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
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    [RequirePermission(PermissionModules.Roles, PermissionActions.Edit)]
    public async Task<IActionResult> SaveRolePermissions([FromBody] SaveRolePermissionsRequestDto dto)
    {
        try
        {
            var permissions = await _rolePermissionService.SaveRolePermissionsAsync(dto);
            return Ok(permissions);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
