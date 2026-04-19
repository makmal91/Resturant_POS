using POSSystem.Application.Menu.DTOs;
using POSSystem.Application.Menu.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using POSSystem.Domain;

namespace POSSystem.Application.Menu.Services;

public class MenuService : IMenuService
{
    private readonly IMenuRepository _repository;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan PosCacheDuration = TimeSpan.FromMinutes(2);

    private static (bool isSaleable, bool isInventoryItem, bool isRecipeItem, bool isPurchasable) ResolveFlagsByProductType(ProductType productType)
    {
        return productType switch
        {
            ProductType.RawMaterial => (false, true, true, true),
            ProductType.FinishedGood => (true, false, false, false),
            ProductType.SemiFinished => (false, true, true, false),
            ProductType.Service => (true, false, false, false),
            _ => (true, false, false, false)
        };
    }

    private static void ValidateOptionalFlags(ProductType productType, bool? isSaleable, bool? isInventoryItem, bool? isRecipeItem, bool? isPurchasable, (bool isSaleable, bool isInventoryItem, bool isRecipeItem, bool isPurchasable) expected)
    {
        if (isSaleable.HasValue && isSaleable.Value != expected.isSaleable)
            throw new InvalidOperationException($"Invalid IsSaleable for product type {productType}.");

        if (isInventoryItem.HasValue && isInventoryItem.Value != expected.isInventoryItem)
            throw new InvalidOperationException($"Invalid IsInventoryItem for product type {productType}.");

        if (isRecipeItem.HasValue && isRecipeItem.Value != expected.isRecipeItem)
            throw new InvalidOperationException($"Invalid IsRecipeItem for product type {productType}.");

        if (isPurchasable.HasValue && isPurchasable.Value != expected.isPurchasable)
            throw new InvalidOperationException($"Invalid IsPurchasable for product type {productType}.");
    }

    private static void ValidateCategoryProductCompatibility(CategoryType categoryType, ProductType productType)
    {
        if (categoryType == CategoryType.Sale && productType != ProductType.FinishedGood)
            throw new InvalidOperationException("Sale categories only allow FinishedGood items.");

        if (categoryType == CategoryType.Inventory &&
            productType != ProductType.RawMaterial &&
            productType != ProductType.SemiFinished)
            throw new InvalidOperationException("Inventory categories only allow RawMaterial or SemiFinished items.");
    }

    private static MenuCategoryDto MapCategoryDto(MenuCategory category)
    {
        return new MenuCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CategoryType = category.CategoryType,
            Items = category.MenuItems.Select(MapMenuItemDto).ToList()
        };
    }

    private static MenuItemDto MapMenuItemDto(MenuItem item)
    {
        return new MenuItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            Tax = item.TaxPercentage,
            PreparationTime = item.PreparationTime,
            MenuCategoryId = item.MenuCategoryId,
            BranchId = item.BranchId,
            ProductType = item.ProductType,
            IsSaleable = item.IsSaleable,
            IsInventoryItem = item.IsInventoryItem,
            IsRecipeItem = item.IsRecipeItem,
            IsPurchasable = item.IsPurchasable,
            Variants = item.Variants.Select(v => new MenuItemVariantDto
            {
                Id = v.Id,
                Name = v.Name,
                Price = v.Price
            }).ToList(),
            Addons = item.Addons.Select(a => new MenuItemAddonDto
            {
                Id = a.Id,
                Name = a.Name,
                Price = a.Price
            }).ToList()
        };
    }

    public MenuService(IMenuRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    private static string GetPosMenuCacheKey(int branchId) => $"pos-menu-{branchId}";

    public async Task<ICollection<MenuCategoryDto>> GetCategoriesAsync(int branchId, CategoryType? categoryType = null)
    {
        var categories = await _repository.GetCategoriesByBranchAsync(branchId, categoryType);
        return categories.Select(MapCategoryDto).ToList();
    }

    public async Task<MenuCategoryDto?> GetCategoryByIdAsync(int id, int branchId)
    {
        var category = await _repository.GetCategoryAsync(id, includeItems: true);
        if (category == null || category.BranchId != branchId)
            return null;

        return MapCategoryDto(category);
    }

    public async Task<MenuCategory> AddCategoryAsync(CreateMenuCategoryDto dto)
    {
        var category = new MenuCategory
        {
            Name = dto.Name,
            Description = dto.Description,
            CategoryType = dto.CategoryType,
            BranchId = dto.BranchId
        };

        await _repository.AddCategoryAsync(category);
        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(dto.BranchId));

        return category;
    }

    public async Task<MenuCategory> UpdateCategoryAsync(int id, UpdateMenuCategoryDto dto)
    {
        var category = await _repository.GetCategoryAsync(id, includeItems: true);
        if (category == null)
            throw new InvalidOperationException("Category not found.");

        if (category.BranchId != dto.BranchId)
            throw new InvalidOperationException("Category branch mismatch.");

        if (dto.CategoryType != category.CategoryType)
        {
            foreach (var item in category.MenuItems)
            {
                ValidateCategoryProductCompatibility(dto.CategoryType, item.ProductType);
            }
        }

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.CategoryType = dto.CategoryType;
        category.UpdatedDate = DateTime.UtcNow;

        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(dto.BranchId));

        return category;
    }

    public async Task DeleteCategoryAsync(int id, int branchId)
    {
        var category = await _repository.GetCategoryAsync(id, includeItems: true);
        if (category == null || category.BranchId != branchId)
            throw new InvalidOperationException("Category not found.");

        if (category.MenuItems.Count > 0)
            throw new InvalidOperationException("Cannot delete category that still has products.");

        _repository.RemoveCategory(category);
        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(branchId));
    }

    public async Task<ICollection<MenuItemDto>> GetMenuItemsAsync(int branchId, ProductType? productType = null, bool? isSaleable = null, bool? isInventoryItem = null)
    {
        var items = await _repository.GetMenuItemsByBranchAsync(branchId, productType, isSaleable, isInventoryItem);
        return items.Select(MapMenuItemDto).ToList();
    }

    public async Task<MenuItemDto?> GetMenuItemByIdAsync(int id, int branchId)
    {
        var item = await _repository.GetMenuItemAsync(id, includeOptions: true);
        if (item == null || item.BranchId != branchId)
            return null;

        return MapMenuItemDto(item);
    }

    public async Task<MenuItem> AddMenuItemAsync(CreateMenuItemDto dto)
    {
        var category = await _repository.GetCategoryAsync(dto.MenuCategoryId);
        if (category == null)
            throw new InvalidOperationException("Menu category not found.");

        if (category.BranchId != dto.BranchId)
            throw new InvalidOperationException("Category branch mismatch.");

        ValidateCategoryProductCompatibility(category.CategoryType, dto.ProductType);

        var expectedFlags = ResolveFlagsByProductType(dto.ProductType);
        ValidateOptionalFlags(dto.ProductType, dto.IsSaleable, dto.IsInventoryItem, dto.IsRecipeItem, dto.IsPurchasable, expectedFlags);

        var menuItem = new MenuItem
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            TaxPercentage = dto.Tax,
            PreparationTime = dto.PreparationTime,
            MenuCategoryId = dto.MenuCategoryId,
            BranchId = dto.BranchId,
            ProductType = dto.ProductType,
            IsSaleable = expectedFlags.isSaleable,
            IsInventoryItem = expectedFlags.isInventoryItem,
            IsRecipeItem = expectedFlags.isRecipeItem,
            IsPurchasable = expectedFlags.isPurchasable,
            Variants = dto.Variants.Select(v => new MenuItemVariant
            {
                Name = v.Name,
                Price = v.Price,
                BranchId = dto.BranchId
            }).ToList(),
            Addons = dto.Addons.Select(a => new MenuItemAddon
            {
                Name = a.Name,
                Price = a.Price,
                BranchId = dto.BranchId
            }).ToList()
        };

        await _repository.AddMenuItemAsync(menuItem);
        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(dto.BranchId));

        return menuItem;
    }

    public async Task<MenuItem> UpdateMenuItemAsync(int id, UpdateMenuItemDto dto)
    {
        var menuItem = await _repository.GetMenuItemAsync(id, includeOptions: true);
        if (menuItem == null)
            throw new InvalidOperationException("Product not found.");

        if (menuItem.BranchId != dto.BranchId)
            throw new InvalidOperationException("Product branch mismatch.");

        var category = await _repository.GetCategoryAsync(dto.MenuCategoryId);
        if (category == null)
            throw new InvalidOperationException("Menu category not found.");

        if (category.BranchId != dto.BranchId)
            throw new InvalidOperationException("Category branch mismatch.");

        ValidateCategoryProductCompatibility(category.CategoryType, dto.ProductType);

        var expectedFlags = ResolveFlagsByProductType(dto.ProductType);
        ValidateOptionalFlags(dto.ProductType, dto.IsSaleable, dto.IsInventoryItem, dto.IsRecipeItem, dto.IsPurchasable, expectedFlags);

        menuItem.Name = dto.Name;
        menuItem.Description = dto.Description;
        menuItem.Price = dto.Price;
        menuItem.TaxPercentage = dto.Tax;
        menuItem.PreparationTime = dto.PreparationTime;
        menuItem.MenuCategoryId = dto.MenuCategoryId;
        menuItem.ProductType = dto.ProductType;
        menuItem.IsSaleable = expectedFlags.isSaleable;
        menuItem.IsInventoryItem = expectedFlags.isInventoryItem;
        menuItem.IsRecipeItem = expectedFlags.isRecipeItem;
        menuItem.IsPurchasable = expectedFlags.isPurchasable;
        menuItem.UpdatedDate = DateTime.UtcNow;

        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(dto.BranchId));

        return menuItem;
    }

    public async Task DeleteMenuItemAsync(int id, int branchId)
    {
        var menuItem = await _repository.GetMenuItemAsync(id, includeOptions: true);
        if (menuItem == null || menuItem.BranchId != branchId)
            throw new InvalidOperationException("Product not found.");

        _repository.RemoveMenuItem(menuItem);
        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(branchId));
    }

    public async Task<MenuItemVariant> AddVariantAsync(CreateMenuItemVariantDto dto, int menuItemId, int branchId)
    {
        var variant = new MenuItemVariant
        {
            Name = dto.Name,
            Price = dto.Price,
            MenuItemId = menuItemId,
            BranchId = branchId
        };

        await _repository.AddVariantAsync(variant);
        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(branchId));

        return variant;
    }

    public async Task<MenuItemAddon> AddAddonAsync(CreateMenuItemAddonDto dto, int menuItemId, int branchId)
    {
        var addon = new MenuItemAddon
        {
            Name = dto.Name,
            Price = dto.Price,
            MenuItemId = menuItemId,
            BranchId = branchId
        };

        await _repository.AddAddonAsync(addon);
        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(branchId));

        return addon;
    }

    public async Task<MenuDto> GetFullMenuAsync(int branchId)
    {
        var categories = await _repository.GetCategoriesWithItemsAsync(branchId);

        var menuDto = new MenuDto
        {
            Categories = categories.Select(MapCategoryDto).ToList()
        };

        return menuDto;
    }

    public async Task<MenuDto> GetPosMenuAsync(int branchId)
    {
        if (_cache.TryGetValue(GetPosMenuCacheKey(branchId), out MenuDto? cachedMenu) && cachedMenu != null)
        {
            return cachedMenu;
        }

        var categories = await _repository.GetPosCategoriesWithItemsAsync(branchId);

        var menu = new MenuDto
        {
            Categories = categories.Select(MapCategoryDto).ToList()
        };

        _cache.Set(GetPosMenuCacheKey(branchId), menu, PosCacheDuration);

        return menu;
    }
}