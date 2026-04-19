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

    Task<ICollection<SubCategoryDto>> GetSubCategoriesAsync(int branchId, int? categoryId = null);
    Task<SubCategoryDto?> GetSubCategoryByIdAsync(int id, int branchId);
    Task<SubCategory> AddSubCategoryAsync(CreateSubCategoryDto dto);
    Task<SubCategory> UpdateSubCategoryAsync(int id, UpdateSubCategoryDto dto);
    Task DeleteSubCategoryAsync(int id, int branchId);

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
    Task<MenuCategory?> GetCategoryByNameAsync(string name, int branchId, int? excludeCategoryId = null);
    Task<bool> BranchExistsAsync(int branchId);
    Task<SubCategory?> GetSubCategoryAsync(int id);
    Task<SubCategory?> GetSubCategoryByNameAsync(string name, int branchId, int? excludeSubCategoryId = null);
    Task<MenuItem?> GetMenuItemAsync(int id, bool includeOptions = false);
    Task<ICollection<MenuCategory>> GetCategoriesByBranchAsync(int branchId, CategoryType? categoryType = null);
    Task<ICollection<SubCategory>> GetSubCategoriesByBranchAsync(int branchId, int? categoryId = null);
    Task<ICollection<MenuItem>> GetMenuItemsByBranchAsync(int branchId, ProductType? productType = null, bool? isSaleable = null, bool? isInventoryItem = null);
    Task<ICollection<MenuCategory>> GetCategoriesWithItemsAsync(int branchId);
    Task<ICollection<MenuCategory>> GetPosCategoriesWithItemsAsync(int branchId);
    Task AddCategoryAsync(MenuCategory category);
    Task AddSubCategoryAsync(SubCategory subCategory);
    Task AddMenuItemAsync(MenuItem item);
    void RemoveCategory(MenuCategory category);
    void RemoveSubCategory(SubCategory subCategory);
    void RemoveMenuItem(MenuItem item);
    Task AddVariantAsync(MenuItemVariant variant);
    Task AddAddonAsync(MenuItemAddon addon);
    Task SaveChangesAsync();
}