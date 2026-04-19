using POSSystem.Application.Recipe.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Recipe.Interfaces;

public interface IRecipeService
{
    Task<POSSystem.Domain.Recipe> CreateRecipeAsync(CreateRecipeDto dto);
    Task<POSSystem.Domain.Recipe> UpdateRecipeAsync(UpdateRecipeDto dto);
    Task DeleteRecipeAsync(int id);
    Task<POSSystem.Domain.Recipe?> GetRecipeAsync(int id);
    Task<ICollection<POSSystem.Domain.Recipe>> GetRecipesByMenuItemAsync(int menuItemId);
}

public interface IRecipeRepository
{
    Task<POSSystem.Domain.Recipe?> GetRecipeAsync(int id);
    Task<ICollection<POSSystem.Domain.Recipe>> GetRecipesByMenuItemAsync(int menuItemId);
    Task<MenuItem?> GetMenuItemAsync(int id);
    Task<InventoryItem?> GetInventoryItemAsync(int id);
    Task AddRecipeAsync(POSSystem.Domain.Recipe recipe);
    Task SaveChangesAsync();
}