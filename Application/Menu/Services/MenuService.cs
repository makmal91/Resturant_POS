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
            Code = category.Code,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            ImageUrl = category.ImageUrl,
            Icon = category.Icon,
            Color = category.Color,
            Status = category.Status,
            CategoryType = category.CategoryType,
            BranchId = category.BranchId,
            BranchName = category.Branch?.Name ?? string.Empty,
            SubCategories = category.SubCategories.Select(MapSubCategoryDto).ToList(),
            Items = category.MenuItems.Select(MapMenuItemDto).ToList()
        };
    }

    private static SubCategoryDto MapSubCategoryDto(SubCategory subCategory)
    {
        return new SubCategoryDto
        {
            Id = subCategory.Id,
            Name = subCategory.Name,
            Description = subCategory.Description,
            DisplayOrder = subCategory.DisplayOrder,
            Status = subCategory.Status,
            Icon = subCategory.Icon,
            CategoryId = subCategory.CategoryId,
            CategoryName = subCategory.Category?.Name ?? string.Empty,
            BranchId = subCategory.BranchId,
            BranchName = subCategory.Branch?.Name ?? string.Empty
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

    private async Task EnsureBranchExistsAsync(int branchId)
    {
        if (branchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        var branchExists = await _repository.BranchExistsAsync(branchId);
        if (!branchExists)
            throw new InvalidOperationException("Selected branch does not exist.");
    }

    private async Task ValidateCategoryInputAsync(string categoryName, string categoryCode, int branchId, int? excludeCategoryId = null)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new InvalidOperationException("Category name is required.");

        if (string.IsNullOrWhiteSpace(categoryCode))
            throw new InvalidOperationException("Category code is required.");

        await EnsureBranchExistsAsync(branchId);

        var duplicateCategory = await _repository.GetCategoryByNameAsync(categoryName, branchId, excludeCategoryId);
        if (duplicateCategory != null)
            throw new InvalidOperationException("Category name must be unique per branch.");
    }

    private async Task<MenuCategory> ValidateSubCategoryContextAsync(int categoryId, int branchId)
    {
        var category = await _repository.GetCategoryAsync(categoryId);
        if (category == null)
            throw new InvalidOperationException("Category not found.");

        if (category.BranchId != branchId)
            throw new InvalidOperationException("Category branch mismatch.");

        return category;
    }

    public async Task<ICollection<MenuCategoryDto>> GetCategoriesAsync(int branchId, CategoryType? categoryType = null)
    {
        await EnsureBranchExistsAsync(branchId);

        var categories = await _repository.GetCategoriesByBranchAsync(branchId, categoryType);
        return categories.Select(MapCategoryDto).ToList();
    }

    public async Task<MenuCategoryDto?> GetCategoryByIdAsync(int id, int branchId)
    {
        await EnsureBranchExistsAsync(branchId);

        var category = await _repository.GetCategoryAsync(id, includeItems: true);
        if (category == null || category.BranchId != branchId)
            return null;

        return MapCategoryDto(category);
    }

    public async Task<MenuCategory> AddCategoryAsync(CreateMenuCategoryDto dto)
    {
        await ValidateCategoryInputAsync(dto.Name, dto.Code, dto.BranchId);

        var category = new MenuCategory
        {
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim(),
            Description = dto.Description,
            DisplayOrder = dto.DisplayOrder,
            ImageUrl = dto.ImageUrl,
            Icon = dto.Icon,
            Color = dto.Color,
            Status = dto.Status,
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

        await ValidateCategoryInputAsync(dto.Name, dto.Code, dto.BranchId, excludeCategoryId: id);

        if (category.BranchId != dto.BranchId)
            throw new InvalidOperationException("Category branch mismatch.");

        if (dto.CategoryType != category.CategoryType)
        {
            foreach (var item in category.MenuItems)
            {
                ValidateCategoryProductCompatibility(dto.CategoryType, item.ProductType);
            }
        }

        category.Name = dto.Name.Trim();
        category.Code = dto.Code.Trim();
        category.Description = dto.Description;
        category.DisplayOrder = dto.DisplayOrder;
        category.ImageUrl = dto.ImageUrl;
        category.Icon = dto.Icon;
        category.Color = dto.Color;
        category.Status = dto.Status;
        category.CategoryType = dto.CategoryType;
        category.UpdatedDate = DateTime.UtcNow;

        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(dto.BranchId));

        return category;
    }

    public async Task DeleteCategoryAsync(int id, int branchId)
    {
        await EnsureBranchExistsAsync(branchId);

        var category = await _repository.GetCategoryAsync(id, includeItems: true);
        if (category == null || category.BranchId != branchId)
            throw new InvalidOperationException("Category not found.");

        if (category.MenuItems.Count > 0)
            throw new InvalidOperationException("Cannot delete category that still has products.");

        _repository.RemoveCategory(category);
        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(branchId));
    }

    public async Task<ICollection<SubCategoryDto>> GetSubCategoriesAsync(int branchId, int? categoryId = null)
    {
        await EnsureBranchExistsAsync(branchId);

        if (categoryId.HasValue)
        {
            await ValidateSubCategoryContextAsync(categoryId.Value, branchId);
        }

        var subCategories = await _repository.GetSubCategoriesByBranchAsync(branchId, categoryId);
        return subCategories.Select(MapSubCategoryDto).ToList();
    }

    public async Task<SubCategoryDto?> GetSubCategoryByIdAsync(int id, int branchId)
    {
        await EnsureBranchExistsAsync(branchId);

        var subCategory = await _repository.GetSubCategoryAsync(id);
        if (subCategory == null || subCategory.BranchId != branchId)
            return null;

        return MapSubCategoryDto(subCategory);
    }

    public async Task<SubCategory> AddSubCategoryAsync(CreateSubCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("SubCategory name is required.");

        await EnsureBranchExistsAsync(dto.BranchId);
        await ValidateSubCategoryContextAsync(dto.CategoryId, dto.BranchId);

        var duplicate = await _repository.GetSubCategoryByNameAsync(dto.Name, dto.BranchId);
        if (duplicate != null)
            throw new InvalidOperationException("SubCategory name must be unique per branch.");

        var subCategory = new SubCategory
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            DisplayOrder = dto.DisplayOrder,
            Status = dto.Status,
            Icon = dto.Icon,
            CategoryId = dto.CategoryId,
            BranchId = dto.BranchId
        };

        await _repository.AddSubCategoryAsync(subCategory);
        await _repository.SaveChangesAsync();

        return subCategory;
    }

    public async Task<SubCategory> UpdateSubCategoryAsync(int id, UpdateSubCategoryDto dto)
    {
        var subCategory = await _repository.GetSubCategoryAsync(id);
        if (subCategory == null)
            throw new InvalidOperationException("SubCategory not found.");

        await EnsureBranchExistsAsync(dto.BranchId);

        if (subCategory.BranchId != dto.BranchId)
            throw new InvalidOperationException("SubCategory branch mismatch.");

        await ValidateSubCategoryContextAsync(dto.CategoryId, dto.BranchId);

        var duplicate = await _repository.GetSubCategoryByNameAsync(dto.Name, dto.BranchId, id);
        if (duplicate != null)
            throw new InvalidOperationException("SubCategory name must be unique per branch.");

        subCategory.Name = dto.Name.Trim();
        subCategory.Description = dto.Description;
        subCategory.DisplayOrder = dto.DisplayOrder;
        subCategory.Status = dto.Status;
        subCategory.Icon = dto.Icon;
        subCategory.CategoryId = dto.CategoryId;
        subCategory.UpdatedDate = DateTime.UtcNow;

        await _repository.SaveChangesAsync();

        return subCategory;
    }

    public async Task DeleteSubCategoryAsync(int id, int branchId)
    {
        await EnsureBranchExistsAsync(branchId);

        var subCategory = await _repository.GetSubCategoryAsync(id);
        if (subCategory == null || subCategory.BranchId != branchId)
            throw new InvalidOperationException("SubCategory not found.");

        _repository.RemoveSubCategory(subCategory);
        await _repository.SaveChangesAsync();
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