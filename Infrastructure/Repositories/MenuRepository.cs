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

    public async Task<MenuCategory?> GetCategoryAsync(int id, bool includeItems = false)
    {
        if (!includeItems)
        {
            return await _context.MenuCategories.FindAsync(id);
        }

        return await _context.MenuCategories
            .Include(c => c.MenuItems)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<MenuItem?> GetMenuItemAsync(int id, bool includeOptions = false)
    {
        if (!includeOptions)
        {
            return await _context.MenuItems.FindAsync(id);
        }

        return await _context.MenuItems
            .Include(i => i.Variants)
            .Include(i => i.Addons)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<ICollection<MenuCategory>> GetCategoriesByBranchAsync(int branchId, CategoryType? categoryType = null)
    {
        var query = _context.MenuCategories
            .Where(c => c.BranchId == branchId)
            .AsQueryable();

        if (categoryType.HasValue)
        {
            query = query.Where(c => c.CategoryType == categoryType.Value);
        }

        return await query
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<ICollection<MenuItem>> GetMenuItemsByBranchAsync(int branchId, ProductType? productType = null, bool? isSaleable = null, bool? isInventoryItem = null)
    {
        var query = _context.MenuItems
            .Where(i => i.BranchId == branchId)
            .Include(i => i.MenuCategory)
            .Include(i => i.Variants)
            .Include(i => i.Addons)
            .AsQueryable();

        if (productType.HasValue)
        {
            query = query.Where(i => i.ProductType == productType.Value);
        }

        if (isSaleable.HasValue)
        {
            query = query.Where(i => i.IsSaleable == isSaleable.Value);
        }

        if (isInventoryItem.HasValue)
        {
            query = query.Where(i => i.IsInventoryItem == isInventoryItem.Value);
        }

        return await query
            .OrderBy(i => i.Name)
            .ToListAsync();
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

    public async Task<ICollection<MenuCategory>> GetPosCategoriesWithItemsAsync(int branchId)
    {
        return await _context.MenuCategories
            .Where(c => c.BranchId == branchId && c.CategoryType == CategoryType.Sale)
            .Include(c => c.MenuItems.Where(i =>
                i.IsAvailable &&
                i.IsSaleable &&
                i.ProductType == ProductType.FinishedGood))
                .ThenInclude(i => i.Variants)
            .Include(c => c.MenuItems.Where(i =>
                i.IsAvailable &&
                i.IsSaleable &&
                i.ProductType == ProductType.FinishedGood))
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

    public void RemoveCategory(MenuCategory category)
    {
        _context.MenuCategories.Remove(category);
    }

    public void RemoveMenuItem(MenuItem item)
    {
        _context.MenuItems.Remove(item);
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