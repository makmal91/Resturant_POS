using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Modules.Interfaces;
using POSSystem.API.Authorization;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/modules")]
public class ModulesController : ControllerBase
{
    private readonly IModuleService _moduleService;

    public ModulesController(IModuleService moduleService)
    {
        _moduleService = moduleService;
    }

    [HttpGet]
    [RequirePermission(PermissionModules.Roles, PermissionActions.View)]
    public async Task<IActionResult> GetModules()
    {
        var modules = await _moduleService.GetModulesAsync();
        return Ok(modules);
    }
}
