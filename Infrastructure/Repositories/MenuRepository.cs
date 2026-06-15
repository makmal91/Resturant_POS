using POSSystem.Application.Menu.Interfaces;
using POSSystem.Application.Common.DTOs;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace POSSystem.Infrastructure.Repositories;

public class MenuRepository : IMenuRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public MenuRepository(POSDbContext context)
    {
        _context = context;
    }

    public async Task<MenuCategory?> GetCategoryAsync(int id, int businessId, int branchId, bool includeItems = false)
    {
        if (!includeItems)
        {
            return await _context.MenuCategories
                .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId && c.BranchId == branchId);
        }

        return await _context.MenuCategories
            .Include(c => c.Branch)
            .Include(c => c.SubCategories)
            .Include(c => c.MenuItems)
                .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId && c.BranchId == branchId);
    }

            public async Task<MenuCategory?> GetCategoryByNameAsync(string name, int businessId, int branchId, int? excludeCategoryId = null)
    {
        var normalized = name.Trim();

        return await _context.MenuCategories
            .IgnoreQueryFilters()
            .Where(c => c.BusinessId == businessId && c.BranchId == branchId && c.Name.ToLower() == normalized.ToLower())
            .Where(c => !excludeCategoryId.HasValue || c.Id != excludeCategoryId.Value)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> BranchExistsAsync(int businessId, int branchId)
    {
        return await _context.Branches.AnyAsync(b => b.Id == branchId && b.BusinessId == businessId);
    }

    public async Task<SubCategory?> GetSubCategoryAsync(int id, int businessId, int branchId)
    {
        return await _context.SubCategories
            .Include(sc => sc.Category)
            .Include(sc => sc.Branch)
            .FirstOrDefaultAsync(sc => sc.Id == id && sc.BusinessId == businessId && sc.BranchId == branchId);
    }

    public async Task<SubCategory?> GetSubCategoryByNameAsync(string name, int categoryId, int businessId, int branchId, int? excludeSubCategoryId = null)
    {
        var normalized = name.Trim();

        return await _context.SubCategories
            .IgnoreQueryFilters()
            .Where(sc => !sc.IsDeleted && sc.BusinessId == businessId && sc.BranchId == branchId && sc.CategoryId == categoryId && sc.Name.ToLower() == normalized.ToLower())
            .Where(sc => !excludeSubCategoryId.HasValue || sc.Id != excludeSubCategoryId.Value)
            .FirstOrDefaultAsync();
    }

    public async Task<PagedResultDto<SubCategory>> GetSubCategoriesPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null,
        int? categoryId = null,
        bool? status = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _context.SubCategories
            .IgnoreQueryFilters()
            .Where(sc => !sc.IsDeleted && sc.BusinessId == businessId);

        if (branchId > 0)
        {
            query = query.Where(sc => sc.BranchId == branchId);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(sc => sc.CategoryId == categoryId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(sc => sc.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(sc =>
                sc.Name.ToLower().Contains(term) ||
                sc.Code.ToLower().Contains(term) ||
                sc.Description.ToLower().Contains(term));
        }

        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
            page = totalPages;

        var orderedQuery = branchId == 0
            ? query.OrderBy(sc => sc.Branch!.Name).ThenBy(sc => sc.DisplayOrder).ThenBy(sc => sc.Name)
            : query.OrderBy(sc => sc.DisplayOrder).ThenBy(sc => sc.Name);

        var data = await orderedQuery
            .Include(sc => sc.Category)
            .Include(sc => sc.Branch)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<SubCategory>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page
        };
    }

    public async Task<bool> SubCategoryHasProductsAsync(int subCategoryId, int businessId, int branchId)
    {
        return await _context.MenuItems
            .IgnoreQueryFilters()
            .AnyAsync(i => !i.IsDeleted && i.BusinessId == businessId && i.BranchId == branchId && i.SubCategoryId == subCategoryId);
    }

    public async Task<MenuItem?> GetMenuItemAsync(int id, int businessId, int branchId, bool includeOptions = false)
    {
        if (!includeOptions)
        {
            return await _context.MenuItems
                .FirstOrDefaultAsync(i => i.Id == id && i.BusinessId == businessId && i.BranchId == branchId);
        }

        return await _context.MenuItems
            .Include(i => i.Variants)
            .Include(i => i.Addons)
                .FirstOrDefaultAsync(i => i.Id == id && i.BusinessId == businessId && i.BranchId == branchId);
    }

    public async Task<PagedResultDto<MenuCategory>> GetCategoriesPagedAsync(int businessId, int branchId, int page, int pageSize, CategoryType? categoryType = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _context.MenuCategories
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted && c.BusinessId == businessId);

        if (branchId > 0)
        {
            query = query.Where(c => c.BranchId == branchId);
        }

        if (categoryType.HasValue)
        {
            query = query.Where(c => c.CategoryType == categoryType.Value);
        }

        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
            page = totalPages;

        var orderedQuery = branchId == 0
            ? query.OrderBy(c => c.Branch.Name).ThenBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            : query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name);

        var data = await orderedQuery
            .Include(c => c.Branch)
            .Include(c => c.SubCategories)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<MenuCategory>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page
        };
    }

    public async Task<ICollection<MenuCategory>> GetCategoriesByBranchAsync(int businessId, int branchId, CategoryType? categoryType = null)
    {
        var query = _context.MenuCategories
                .Where(c => c.BusinessId == businessId && c.BranchId == branchId)
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

    public async Task<ICollection<SubCategory>> GetSubCategoriesByBranchAsync(int businessId, int branchId, int? categoryId = null)
    {
        var query = _context.SubCategories
            .Where(sc => sc.BusinessId == businessId && sc.BranchId == branchId)
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

    public async Task<ICollection<MenuItem>> GetMenuItemsByBranchAsync(int businessId, int branchId, ProductType? productType = null, bool? isSaleable = null, bool? isInventoryItem = null)
    {
        var query = _context.MenuItems
            .Where(i => i.BusinessId == businessId && i.BranchId == branchId)
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

    public async Task<ICollection<MenuCategory>> GetCategoriesWithItemsAsync(int businessId, int branchId)
    {
        return await _context.MenuCategories
            .Where(c => c.BusinessId == businessId && c.BranchId == branchId)
            .Include(c => c.Branch)
            .Include(c => c.SubCategories)
            .Include(c => c.MenuItems)
                .ThenInclude(i => i.Variants)
            .Include(c => c.MenuItems)
                .ThenInclude(i => i.Addons)
            .ToListAsync();
    }

    public async Task<ICollection<MenuCategory>> GetPosCategoriesWithItemsAsync(int businessId, int branchId)
    {
        return await _context.MenuCategories
            .Where(c => c.BusinessId == businessId && c.BranchId == branchId && c.CategoryType == CategoryType.Sale)
            .Include(c => c.Branch)
            .Include(c => c.SubCategories.Where(sc => sc.Status))
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