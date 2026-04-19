using POSSystem.Domain;

namespace POSSystem.Application.Menu.DTOs;

public class CreateMenuCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CategoryType CategoryType { get; set; } = CategoryType.Sale;
    public int BranchId { get; set; }
}

public class UpdateMenuCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CategoryType CategoryType { get; set; } = CategoryType.Sale;
    public int BranchId { get; set; }
}

public class CreateMenuItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Tax { get; set; } // TaxPercentage
    public int PreparationTime { get; set; }
    public int MenuCategoryId { get; set; }
    public int BranchId { get; set; }
    public ProductType ProductType { get; set; } = ProductType.FinishedGood;
    public bool? IsSaleable { get; set; }
    public bool? IsInventoryItem { get; set; }
    public bool? IsRecipeItem { get; set; }
    public bool? IsPurchasable { get; set; }
    public List<CreateMenuItemVariantDto> Variants { get; set; } = new();
    public List<CreateMenuItemAddonDto> Addons { get; set; } = new();
}

public class UpdateMenuItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Tax { get; set; }
    public int PreparationTime { get; set; }
    public int MenuCategoryId { get; set; }
    public int BranchId { get; set; }
    public ProductType ProductType { get; set; } = ProductType.FinishedGood;
    public bool? IsSaleable { get; set; }
    public bool? IsInventoryItem { get; set; }
    public bool? IsRecipeItem { get; set; }
    public bool? IsPurchasable { get; set; }
}

public class CreateMenuItemVariantDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class CreateMenuItemAddonDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class MenuDto
{
    public List<MenuCategoryDto> Categories { get; set; } = new();
}

public class MenuCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CategoryType CategoryType { get; set; } = CategoryType.Sale;
    public List<MenuItemDto> Items { get; set; } = new();
}

public class MenuItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Tax { get; set; }
    public int PreparationTime { get; set; }
    public int MenuCategoryId { get; set; }
    public int BranchId { get; set; }
    public ProductType ProductType { get; set; } = ProductType.FinishedGood;
    public bool IsSaleable { get; set; }
    public bool IsInventoryItem { get; set; }
    public bool IsRecipeItem { get; set; }
    public bool IsPurchasable { get; set; }
    public List<MenuItemVariantDto> Variants { get; set; } = new();
    public List<MenuItemAddonDto> Addons { get; set; } = new();
}

public class MenuItemVariantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class MenuItemAddonDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}