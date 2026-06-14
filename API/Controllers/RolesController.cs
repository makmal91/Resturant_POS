using Microsoft.AspNetCore.Mvc;
using POSSystem.Application.Users.DTOs;
using POSSystem.Application.Users.Interfaces;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleService.GetRolesAsync();
        return Ok(roles);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRoleById(int id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        if (role == null)
            return NotFound();

        return Ok(role);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        try
        {
            var created = await _roleService.CreateRoleAsync(dto);
            return CreatedAtAction(nameof(GetRoleById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleDto dto)
    {
        try
        {
            var updated = await _roleService.UpdateRoleAsync(id, dto);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        try
        {
            await _roleService.DeleteRoleAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/permissions")]
    public async Task<IActionResult> GetRolePermissions(int id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        if (role == null)
            return NotFound();

        var permissions = await _roleService.GetRolePermissionsAsync(id);
        return Ok(permissions);
    }

    [HttpPut("{id:int}/permissions")]
    public async Task<IActionResult> UpdateRolePermissions(int id, [FromBody] UpdateRolePermissionsDto dto)
    {
        try
        {
            await _roleService.UpdateRolePermissionsAsync(id, dto);
            var permissions = await _roleService.GetRolePermissionsAsync(id);
            return Ok(permissions);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
