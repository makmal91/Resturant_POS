using POSSystem.Application.Menu.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Menu.Interfaces;

public interface IMenuService
{
    Task<MenuCategory> AddCategoryAsync(CreateMenuCategoryDto dto);
    Task<MenuItem> AddMenuItemAsync(CreateMenuItemDto dto);
    Task<MenuItemVariant> AddVariantAsync(CreateMenuItemVariantDto dto, int menuItemId, int branchId);
    Task<MenuItemAddon> AddAddonAsync(CreateMenuItemAddonDto dto, int menuItemId, int branchId);
    Task<MenuDto> GetFullMenuAsync(int branchId);
}

public interface IMenuRepository
{
    Task<MenuCategory?> GetCategoryAsync(int id);
    Task<MenuItem?> GetMenuItemAsync(int id);
    Task<ICollection<MenuCategory>> GetCategoriesWithItemsAsync(int branchId);
    Task AddCategoryAsync(MenuCategory category);
    Task AddMenuItemAsync(MenuItem item);
    Task AddVariantAsync(MenuItemVariant variant);
    Task AddAddonAsync(MenuItemAddon addon);
    Task SaveChangesAsync();
}