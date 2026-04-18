using System.Collections.Generic;

namespace POSSystem.Domain;

public class MenuCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
