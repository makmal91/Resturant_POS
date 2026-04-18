using POSSystem.Application.Menu.DTOs;
using POSSystem.Application.Menu.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Menu.Services;

public class MenuService : IMenuService
{
    private readonly IMenuRepository _repository;

    public MenuService(IMenuRepository repository)
    {
        _repository = repository;
    }

    public async Task<MenuCategory> AddCategoryAsync(CreateMenuCategoryDto dto)
    {
        var category = new MenuCategory
        {
            Name = dto.Name,
            Description = dto.Description,
            BranchId = dto.BranchId
        };

        await _repository.AddCategoryAsync(category);
        await _repository.SaveChangesAsync();

        return category;
    }

    public async Task<MenuItem> AddMenuItemAsync(CreateMenuItemDto dto)
    {
        var menuItem = new MenuItem
        {
            Name = dto.Name,
            Price = dto.Price,
            TaxPercentage = dto.Tax,
            PreparationTime = dto.PreparationTime,
            MenuCategoryId = dto.MenuCategoryId,
            BranchId = dto.BranchId,
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

        return menuItem;
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

        return addon;
    }

    public async Task<MenuDto> GetFullMenuAsync(int branchId)
    {
        var categories = await _repository.GetCategoriesWithItemsAsync(branchId);

        var menuDto = new MenuDto
        {
            Categories = categories.Select(c => new MenuCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Items = c.MenuItems.Select(i => new MenuItemDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Price = i.Price,
                    Tax = i.TaxPercentage,
                    PreparationTime = i.PreparationTime,
                    Variants = i.Variants.Select(v => new MenuItemVariantDto
                    {
                        Id = v.Id,
                        Name = v.Name,
                        Price = v.Price
                    }).ToList(),
                    Addons = i.Addons.Select(a => new MenuItemAddonDto
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Price = a.Price
                    }).ToList()
                }).ToList()
            }).ToList()
        };

        return menuDto;
    }
}