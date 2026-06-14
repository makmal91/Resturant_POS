using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Menu.DTOs;
using POSSystem.Application.Menu.Interfaces;
using POSSystem.Domain;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpPost("categories")]
    public async Task<ActionResult<MenuCategory>> AddCategory([FromBody] CreateMenuCategoryDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        var category = await _menuService.AddCategoryAsync(dto);
        return CreatedAtAction(nameof(AddCategory), new { id = category.Id }, category);
    }

    [HttpPost("items")]
    public async Task<ActionResult<MenuItem>> AddMenuItem([FromBody] CreateMenuItemDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        var menuItem = await _menuService.AddMenuItemAsync(dto);
        return CreatedAtAction(nameof(AddMenuItem), new { id = menuItem.Id }, menuItem);
    }

    [HttpPost("items/{menuItemId}/variants")]
    public async Task<ActionResult<MenuItemVariant>> AddVariant(int menuItemId, [FromBody] CreateMenuItemVariantDto dto, [FromQuery] int branchId, [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);
        var variant = await _menuService.AddVariantAsync(dto, menuItemId, resolvedBusinessId, resolvedBranchId);
        return CreatedAtAction(nameof(AddVariant), new { menuItemId, id = variant.Id }, variant);
    }

    [HttpPost("items/{menuItemId}/addons")]
    public async Task<ActionResult<MenuItemAddon>> AddAddon(int menuItemId, [FromBody] CreateMenuItemAddonDto dto, [FromQuery] int branchId, [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);
        var addon = await _menuService.AddAddonAsync(dto, menuItemId, resolvedBusinessId, resolvedBranchId);
        return CreatedAtAction(nameof(AddAddon), new { menuItemId, id = addon.Id }, addon);
    }

    [HttpGet("all")]
    public async Task<ActionResult<MenuDto>> GetAllMenu([FromQuery] int branchId, [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);
        var menu = await _menuService.GetFullMenuAsync(resolvedBusinessId, resolvedBranchId);
        return Ok(menu);
    }

    [HttpGet]
    [HttpGet("pos")]
    public async Task<ActionResult<MenuDto>> GetPosMenu([FromQuery] int branchId, [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);
        var menu = await _menuService.GetPosMenuAsync(resolvedBusinessId, resolvedBranchId);
        return Ok(menu);
    }
}