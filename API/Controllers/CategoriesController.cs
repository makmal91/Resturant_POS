using Microsoft.AspNetCore.Mvc;
using POSSystem.Application.Menu.DTOs;
using POSSystem.Application.Menu.Interfaces;
using POSSystem.Domain;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMenuService _menuService;
    private const int DefaultBranchId = 1;

    public CategoriesController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories(
        [FromQuery] int branchId = DefaultBranchId,
        [FromQuery] CategoryType? categoryType = null)
    {
        var categories = await _menuService.GetCategoriesAsync(branchId, categoryType);
        return Ok(new { categories });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCategoryById(int id, [FromQuery] int branchId = DefaultBranchId)
    {
        var category = await _menuService.GetCategoryByIdAsync(id, branchId);
        if (category == null)
            return NotFound();

        return Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateMenuCategoryDto dto)
    {
        if (dto.BranchId <= 0)
            dto.BranchId = DefaultBranchId;

        var category = await _menuService.AddCategoryAsync(dto);
        return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id, branchId = category.BranchId }, category);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateMenuCategoryDto dto)
    {
        if (dto.BranchId <= 0)
            dto.BranchId = DefaultBranchId;

        var category = await _menuService.UpdateCategoryAsync(id, dto);
        return Ok(category);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id, [FromQuery] int branchId = DefaultBranchId)
    {
        await _menuService.DeleteCategoryAsync(id, branchId);
        return NoContent();
    }
}
