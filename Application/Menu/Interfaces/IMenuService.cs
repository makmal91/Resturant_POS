using POSSystem.Application.Menu.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Menu.Interfaces;

public interface IMenuService
{
    Task<ICollection<MenuCategoryDto>> GetCategoriesAsync(int branchId, CategoryType? categoryType = null);
    Task<MenuCategoryDto?> GetCategoryByIdAsync(int id, int branchId);
    Task<MenuCategory> AddCategoryAsync(CreateMenuCategoryDto dto);
    Task<MenuCategory> UpdateCategoryAsync(int id, UpdateMenuCategoryDto dto);
    Task DeleteCategoryAsync(int id, int branchId);

    Task<ICollection<MenuItemDto>> GetMenuItemsAsync(int branchId, ProductType? productType = null, bool? isSaleable = null, bool? isInventoryItem = null);
    Task<MenuItemDto?> GetMenuItemByIdAsync(int id, int branchId);
    Task<MenuItem> AddMenuItemAsync(CreateMenuItemDto dto);
    Task<MenuItem> UpdateMenuItemAsync(int id, UpdateMenuItemDto dto);
    Task DeleteMenuItemAsync(int id, int branchId);

    Task<MenuItemVariant> AddVariantAsync(CreateMenuItemVariantDto dto, int menuItemId, int branchId);
    Task<MenuItemAddon> AddAddonAsync(CreateMenuItemAddonDto dto, int menuItemId, int branchId);
    Task<MenuDto> GetFullMenuAsync(int branchId);
    Task<MenuDto> GetPosMenuAsync(int branchId);
}

public interface IMenuRepository
{
    Task<MenuCategory?> GetCategoryAsync(int id, bool includeItems = false);
    Task<MenuItem?> GetMenuItemAsync(int id, bool includeOptions = false);
    Task<ICollection<MenuCategory>> GetCategoriesByBranchAsync(int branchId, CategoryType? categoryType = null);
    Task<ICollection<MenuItem>> GetMenuItemsByBranchAsync(int branchId, ProductType? productType = null, bool? isSaleable = null, bool? isInventoryItem = null);
    Task<ICollection<MenuCategory>> GetCategoriesWithItemsAsync(int branchId);
    Task<ICollection<MenuCategory>> GetPosCategoriesWithItemsAsync(int branchId);
    Task AddCategoryAsync(MenuCategory category);
    Task AddMenuItemAsync(MenuItem item);
    void RemoveCategory(MenuCategory category);
    void RemoveMenuItem(MenuItem item);
    Task AddVariantAsync(MenuItemVariant variant);
    Task AddAddonAsync(MenuItemAddon addon);
    Task SaveChangesAsync();
}