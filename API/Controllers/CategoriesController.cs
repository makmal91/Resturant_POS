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

    public CategoriesController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories(
        [FromQuery] int branchId,
        [FromQuery] CategoryType? categoryType = null)
    {
        if (branchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        var categories = await _menuService.GetCategoriesAsync(branchId, categoryType);
        return Ok(new { categories });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCategoryById(int id, [FromQuery] int branchId)
    {
        if (branchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        var category = await _menuService.GetCategoryByIdAsync(id, branchId);
        if (category == null)
            return NotFound();

        return Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateMenuCategoryDto dto)
    {
        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var category = await _menuService.AddCategoryAsync(dto);
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id, branchId = category.BranchId }, category);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateMenuCategoryDto dto)
    {
        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var category = await _menuService.UpdateCategoryAsync(id, dto);
            return Ok(category);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id, [FromQuery] int branchId)
    {
        if (branchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            await _menuService.DeleteCategoryAsync(id, branchId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
