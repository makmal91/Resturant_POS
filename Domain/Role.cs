using System.Collections.Generic;

namespace POSSystem.Domain;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Permissions { get; set; } = string.Empty;

    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
