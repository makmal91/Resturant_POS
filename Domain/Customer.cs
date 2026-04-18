using System;
using System.Collections.Generic;

namespace POSSystem.Domain;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int LoyaltyPoints { get; set; }

    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
