using POSSystem.Application.Menu.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace POSSystem.Infrastructure.Repositories;

public class MenuRepository : IMenuRepository
{
    private readonly POSDbContext _context;

    public MenuRepository(POSDbContext context)
    {
        _context = context;
    }

    public async Task<MenuCategory?> GetCategoryAsync(int id)
    {
        return await _context.MenuCategories.FindAsync(id);
    }

    public async Task<MenuItem?> GetMenuItemAsync(int id)
    {
        return await _context.MenuItems.FindAsync(id);
    }

    public async Task<ICollection<MenuCategory>> GetCategoriesWithItemsAsync(int branchId)
    {
        return await _context.MenuCategories
            .Where(c => c.BranchId == branchId)
            .Include(c => c.MenuItems)
                .ThenInclude(i => i.Variants)
            .Include(c => c.MenuItems)
                .ThenInclude(i => i.Addons)
            .ToListAsync();
    }

    public async Task AddCategoryAsync(MenuCategory category)
    {
        await _context.MenuCategories.AddAsync(category);
    }

    public async Task AddMenuItemAsync(MenuItem item)
    {
        await _context.MenuItems.AddAsync(item);
    }

    public async Task AddVariantAsync(MenuItemVariant variant)
    {
        await _context.MenuItemVariants.AddAsync(variant);
    }

    public async Task AddAddonAsync(MenuItemAddon addon)
    {
        await _context.MenuItemAddons.AddAsync(addon);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}