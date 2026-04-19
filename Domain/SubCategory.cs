using System.Collections.Generic;

namespace POSSystem.Domain;

public class SubCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool Status { get; set; } = true;
    public string Icon { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public virtual MenuCategory Category { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
