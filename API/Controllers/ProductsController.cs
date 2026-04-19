using Microsoft.AspNetCore.Mvc;
using POSSystem.Application.Menu.DTOs;
using POSSystem.Application.Menu.Interfaces;
using POSSystem.Domain;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMenuService _menuService;
    private const int DefaultBranchId = 1;

    public ProductsController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int branchId = DefaultBranchId,
        [FromQuery] ProductType? productType = null,
        [FromQuery] bool? isSaleable = null,
        [FromQuery] bool? isInventoryItem = null)
    {
        var items = await _menuService.GetMenuItemsAsync(branchId, productType, isSaleable, isInventoryItem);
        return Ok(new { items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductById(int id, [FromQuery] int branchId = DefaultBranchId)
    {
        var item = await _menuService.GetMenuItemByIdAsync(id, branchId);
        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateMenuItemDto dto)
    {
        if (dto.BranchId <= 0)
            dto.BranchId = DefaultBranchId;

        var item = await _menuService.AddMenuItemAsync(dto);
        return CreatedAtAction(nameof(GetProductById), new { id = item.Id, branchId = item.BranchId }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateMenuItemDto dto)
    {
        if (dto.BranchId <= 0)
            dto.BranchId = DefaultBranchId;

        var item = await _menuService.UpdateMenuItemAsync(id, dto);
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id, [FromQuery] int branchId = DefaultBranchId)
    {
        await _menuService.DeleteMenuItemAsync(id, branchId);
        return NoContent();
    }
}
