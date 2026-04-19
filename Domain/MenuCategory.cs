using System.Collections.Generic;

namespace POSSystem.Domain;

public class MenuCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool Status { get; set; } = true;
    public CategoryType CategoryType { get; set; } = CategoryType.Sale;

    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
    public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
