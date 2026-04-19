using Microsoft.AspNetCore.Mvc;
using POSSystem.Application.Menu.DTOs;
using POSSystem.Application.Menu.Interfaces;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubCategoriesController : ControllerBase
{
    private readonly IMenuService _menuService;

    public SubCategoriesController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSubCategories([FromQuery] int branchId, [FromQuery] int? categoryId = null)
    {
        if (branchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            var subCategories = await _menuService.GetSubCategoriesAsync(branchId, categoryId);
            return Ok(new { subCategories });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSubCategoryById(int id, [FromQuery] int branchId)
    {
        if (branchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        var subCategory = await _menuService.GetSubCategoryByIdAsync(id, branchId);
        if (subCategory == null)
            return NotFound();

        return Ok(subCategory);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSubCategory([FromBody] CreateSubCategoryDto dto)
    {
        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (dto.CategoryId <= 0)
            return BadRequest(new { message = "CategoryId is required." });

        try
        {
            var subCategory = await _menuService.AddSubCategoryAsync(dto);
            return CreatedAtAction(nameof(GetSubCategoryById), new { id = subCategory.Id, branchId = subCategory.BranchId }, subCategory);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSubCategory(int id, [FromBody] UpdateSubCategoryDto dto)
    {
        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (dto.CategoryId <= 0)
            return BadRequest(new { message = "CategoryId is required." });

        try
        {
            var subCategory = await _menuService.UpdateSubCategoryAsync(id, dto);
            return Ok(subCategory);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSubCategory(int id, [FromQuery] int branchId)
    {
        if (branchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            await _menuService.DeleteSubCategoryAsync(id, branchId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
