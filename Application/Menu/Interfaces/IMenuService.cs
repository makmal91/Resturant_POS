using POSSystem.Application.Menu.DTOs;
using POSSystem.Application.Common.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Menu.Interfaces;

public interface IMenuService
{
    Task<PagedResultDto<MenuCategoryDto>> GetCategoriesPagedAsync(int businessId, int branchId, int page, int pageSize, CategoryType? categoryType = null);
    Task<ICollection<MenuCategoryDto>> GetCategoriesAsync(int businessId, int branchId, CategoryType? categoryType = null);
    Task<MenuCategoryDto?> GetCategoryByIdAsync(int id, int businessId, int branchId);
    Task<CategoryImageDto?> GetCategoryImageAsync(int id, int businessId, int branchId);
    Task<MenuCategory> AddCategoryAsync(CreateMenuCategoryDto dto, byte[]? imageBytes = null, string? imageFileName = null, string? imageContentType = null);
    Task<MenuCategory> UpdateCategoryAsync(int id, UpdateMenuCategoryDto dto, byte[]? imageBytes = null, string? imageFileName = null, string? imageContentType = null, bool replaceImage = false);
    Task DeleteCategoryAsync(int id, int businessId, int branchId);

    Task<PagedResultDto<SubCategoryDto>> GetSubCategoriesPagedAsync(int businessId, int branchId, int page, int pageSize, string? search = null, int? categoryId = null, bool? status = null);
    Task<ICollection<SubCategoryDto>> GetSubCategoriesAsync(int businessId, int branchId, int? categoryId = null);
    Task<SubCategoryDto?> GetSubCategoryByIdAsync(int id, int businessId, int branchId, bool includeImage = false);
    Task<SubCategoryImageDto?> GetSubCategoryImageAsync(int id, int businessId, int branchId);
    Task<SubCategory> AddSubCategoryAsync(CreateSubCategoryDto dto, byte[]? imageBytes = null, string? imageContentType = null);
    Task<SubCategory> UpdateSubCategoryAsync(int id, UpdateSubCategoryDto dto, byte[]? imageBytes = null, string? imageContentType = null, bool replaceImage = false);
    Task PatchSubCategoryStatusAsync(SubCategoryStatusPatchDto dto);
    Task DeleteSubCategoryAsync(int id, int businessId, int branchId);

    Task<ICollection<MenuItemDto>> GetMenuItemsAsync(int businessId, int branchId, ProductType? productType = null, bool? isSaleable = null, bool? isInventoryItem = null);
    Task<MenuItemDto?> GetMenuItemByIdAsync(int id, int businessId, int branchId);
    Task<MenuItem> AddMenuItemAsync(CreateMenuItemDto dto);
    Task<MenuItem> UpdateMenuItemAsync(int id, UpdateMenuItemDto dto);
    Task DeleteMenuItemAsync(int id, int businessId, int branchId);

    Task<MenuItemVariant> AddVariantAsync(CreateMenuItemVariantDto dto, int menuItemId, int businessId, int branchId);
    Task<MenuItemAddon> AddAddonAsync(CreateMenuItemAddonDto dto, int menuItemId, int businessId, int branchId);
    Task<MenuDto> GetFullMenuAsync(int businessId, int branchId);
    Task<MenuDto> GetPosMenuAsync(int businessId, int branchId);
}

public interface IMenuRepository
{
    Task<MenuCategory?> GetCategoryAsync(int id, int businessId, int branchId, bool includeItems = false);
    Task<MenuCategory?> GetCategoryByNameAsync(string name, int businessId, int branchId, int? excludeCategoryId = null);
    Task<bool> CategoryCodeExistsAsync(string code, int businessId, int branchId, int? excludeCategoryId = null);
    Task<bool> SubCategoryCodeExistsAsync(string code, int businessId, int branchId, int? excludeSubCategoryId = null);
    Task<bool> BranchExistsAsync(int businessId, int branchId);
    Task<SubCategory?> GetSubCategoryAsync(int id, int businessId, int branchId);
    Task<SubCategory?> GetSubCategoryByNameAsync(string name, int categoryId, int businessId, int branchId, int? excludeSubCategoryId = null);
    Task<PagedResultDto<SubCategory>> GetSubCategoriesPagedAsync(int businessId, int branchId, int page, int pageSize, string? search = null, int? categoryId = null, bool? status = null);
    Task<bool> SubCategoryHasProductsAsync(int subCategoryId, int businessId, int branchId);
    Task<MenuItem?> GetMenuItemAsync(int id, int businessId, int branchId, bool includeOptions = false);
    Task<PagedResultDto<MenuCategory>> GetCategoriesPagedAsync(int businessId, int branchId, int page, int pageSize, CategoryType? categoryType = null);
    Task<ICollection<MenuCategory>> GetCategoriesByBranchAsync(int businessId, int branchId, CategoryType? categoryType = null);
    Task<ICollection<SubCategory>> GetSubCategoriesByBranchAsync(int businessId, int branchId, int? categoryId = null);
    Task<ICollection<MenuItem>> GetMenuItemsByBranchAsync(int businessId, int branchId, ProductType? productType = null, bool? isSaleable = null, bool? isInventoryItem = null);
    Task<ICollection<MenuCategory>> GetCategoriesWithItemsAsync(int businessId, int branchId);
    Task<ICollection<MenuCategory>> GetPosCategoriesWithItemsAsync(int businessId, int branchId);
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