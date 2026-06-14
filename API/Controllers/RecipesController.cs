using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Recipe.DTOs;
using POSSystem.Application.Recipe.Interfaces;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RecipesController : ControllerBase
{
    private readonly IRecipeService _recipeService;

    public RecipesController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecipe([FromBody] CreateRecipeDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        var recipe = await _recipeService.CreateRecipeAsync(dto);
        return CreatedAtAction(nameof(GetRecipeById), new { id = recipe.Id }, recipe);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecipe(int id, [FromBody] UpdateRecipeDto dto)
    {
        dto.Id = id;
        var recipe = await _recipeService.UpdateRecipeAsync(dto);
        return Ok(recipe);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRecipeById(int id)
    {
        var recipe = await _recipeService.GetRecipeAsync(id);
        if (recipe == null)
            return NotFound();

        return Ok(recipe);
    }

    [HttpGet("menu-item/{menuItemId:int}")]
    public async Task<IActionResult> GetByMenuItem(int menuItemId)
    {
        var recipes = await _recipeService.GetRecipesByMenuItemAsync(menuItemId);
        return Ok(new { recipes });
    }
}
