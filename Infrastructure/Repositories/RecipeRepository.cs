using POSSystem.Application.Recipe.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace POSSystem.Infrastructure.Repositories;

public class RecipeRepository : IRecipeRepository
{
    private readonly POSDbContext _context;

    public RecipeRepository(POSDbContext context)
    {
        _context = context;
    }

    public async Task<POSSystem.Domain.Recipe?> GetRecipeAsync(int id)
    {
        return await _context.Recipes.FindAsync(id);
    }

    public async Task<ICollection<POSSystem.Domain.Recipe>> GetRecipesByMenuItemAsync(int menuItemId)
    {
        return await _context.Recipes
            .Where(r => r.MenuItemId == menuItemId)
            .ToListAsync();
    }

    public async Task<MenuItem?> GetMenuItemAsync(int id)
    {
        return await _context.MenuItems.FindAsync(id);
    }

    public async Task<InventoryItem?> GetInventoryItemAsync(int id)
    {
        return await _context.InventoryItems.FindAsync(id);
    }

    public async Task AddRecipeAsync(POSSystem.Domain.Recipe recipe)
    {
        await _context.Recipes.AddAsync(recipe);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}