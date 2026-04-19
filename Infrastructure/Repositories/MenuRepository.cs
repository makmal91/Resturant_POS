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
            .Include(c => c.Branch)
            .Include(c => c.SubCategories)
            .Include(c => c.MenuItems)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<MenuCategory?> GetCategoryByNameAsync(string name, int branchId, int? excludeCategoryId = null)
    {
        var normalized = name.Trim();

        return await _context.MenuCategories
            .Where(c => c.BranchId == branchId && c.Name.ToLower() == normalized.ToLower())
            .Where(c => !excludeCategoryId.HasValue || c.Id != excludeCategoryId.Value)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> BranchExistsAsync(int branchId)
    {
        return await _context.Branches.AnyAsync(b => b.Id == branchId);
    }

    public async Task<SubCategory?> GetSubCategoryAsync(int id)
    {
        return await _context.SubCategories
            .Include(sc => sc.Category)
            .Include(sc => sc.Branch)
            .FirstOrDefaultAsync(sc => sc.Id == id);
    }

    public async Task<SubCategory?> GetSubCategoryByNameAsync(string name, int branchId, int? excludeSubCategoryId = null)
    {
        var normalized = name.Trim();

        return await _context.SubCategories
            .Where(sc => sc.BranchId == branchId && sc.Name.ToLower() == normalized.ToLower())
            .Where(sc => !excludeSubCategoryId.HasValue || sc.Id != excludeSubCategoryId.Value)
            .FirstOrDefaultAsync();
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
            .Include(c => c.Branch)
            .Include(c => c.SubCategories)
            .AsQueryable();

        if (categoryType.HasValue)
        {
            query = query.Where(c => c.CategoryType == categoryType.Value);
        }

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<ICollection<SubCategory>> GetSubCategoriesByBranchAsync(int branchId, int? categoryId = null)
    {
        var query = _context.SubCategories
            .Where(sc => sc.BranchId == branchId)
            .Include(sc => sc.Category)
            .Include(sc => sc.Branch)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(sc => sc.CategoryId == categoryId.Value);
        }

        return await query
            .OrderBy(sc => sc.DisplayOrder)
            .ThenBy(sc => sc.Name)
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
            .Include(c => c.Branch)
            .Include(c => c.SubCategories)
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
            .Include(c => c.Branch)
            .Include(c => c.SubCategories)
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

    public async Task AddSubCategoryAsync(SubCategory subCategory)
    {
        await _context.SubCategories.AddAsync(subCategory);
    }

    public async Task AddMenuItemAsync(MenuItem item)
    {
        await _context.MenuItems.AddAsync(item);
    }

    public void RemoveCategory(MenuCategory category)
    {
        _context.MenuCategories.Remove(category);
    }

    public void RemoveSubCategory(SubCategory subCategory)
    {
        _context.SubCategories.Remove(subCategory);
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