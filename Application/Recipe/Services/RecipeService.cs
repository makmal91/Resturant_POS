using POSSystem.Application.Recipe.DTOs;
using POSSystem.Application.Recipe.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Recipe.Services;

public class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _repository;

    public RecipeService(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<POSSystem.Domain.Recipe> CreateRecipeAsync(CreateRecipeDto dto)
    {
        var menuItem = await _repository.GetMenuItemAsync(dto.MenuItemId);
        if (menuItem == null)
            throw new Exception("Menu item not found");

        if (menuItem.ProductType != ProductType.FinishedGood)
            throw new Exception("Recipe can only be attached to FinishedGood menu items");

        var ingredient = await _repository.GetInventoryItemAsync(dto.IngredientId);
        if (ingredient == null)
            throw new Exception("Ingredient not found");

        if (ingredient.ProductType != ProductType.RawMaterial && ingredient.ProductType != ProductType.SemiFinished)
            throw new Exception("Recipe ingredient must be RawMaterial or SemiFinished");

        if (ingredient.BranchId != dto.BranchId || menuItem.BranchId != dto.BranchId)
            throw new Exception("Recipe item and ingredient must belong to the same branch");

        var recipe = new POSSystem.Domain.Recipe
        {
            MenuItemId = dto.MenuItemId,
            IngredientId = dto.IngredientId,
            QuantityRequired = dto.QuantityRequired,
            Unit = dto.Unit,
            BranchId = dto.BranchId
        };

        await _repository.AddRecipeAsync(recipe);
        await _repository.SaveChangesAsync();

        return recipe;
    }

    public async Task<POSSystem.Domain.Recipe> UpdateRecipeAsync(UpdateRecipeDto dto)
    {
        var recipe = await _repository.GetRecipeAsync(dto.Id);
        if (recipe == null) throw new Exception("Recipe not found");

        recipe.QuantityRequired = dto.QuantityRequired;
        recipe.Unit = dto.Unit;

        await _repository.SaveChangesAsync();

        return recipe;
    }

    public async Task DeleteRecipeAsync(int id)
    {
        var recipe = await _repository.GetRecipeAsync(id);
        if (recipe == null) throw new Exception("Recipe not found");

        // Assuming soft delete or just remove
        // For now, since no IsDeleted, just throw or implement delete

        throw new NotImplementedException("Delete not implemented");
    }

    public async Task<POSSystem.Domain.Recipe?> GetRecipeAsync(int id)
    {
        return await _repository.GetRecipeAsync(id);
    }

    public async Task<ICollection<POSSystem.Domain.Recipe>> GetRecipesByMenuItemAsync(int menuItemId)
    {
        return await _repository.GetRecipesByMenuItemAsync(menuItemId);
    }
}