using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.Application.Navigation.Interfaces;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/menus")]
public class MenusController : ControllerBase
{
    private readonly INavigationMenuService _navigationMenuService;

    public MenusController(INavigationMenuService navigationMenuService)
    {
        _navigationMenuService = navigationMenuService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMenus([FromQuery] int roleId)
    {
        var tokenRoleId = User.FindFirstValue("roleId");
        if (!int.TryParse(tokenRoleId, out var userRoleId) || userRoleId != roleId)
            return Forbid();

        var roleName = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var menus = await _navigationMenuService.GetAllowedMenusAsync(roleId, roleName);
        return Ok(menus);
    }
}
