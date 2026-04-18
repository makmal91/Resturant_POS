using System;
using System.Collections.Generic;

namespace POSSystem.Domain;

public class Table : BaseEntity
{
    public int TableNumber { get; set; }
    public int Capacity { get; set; }
    public TableStatus Status { get; set; } = TableStatus.Available;
    public string Floor { get; set; } = string.Empty;
    public bool IsQrEnabled { get; set; }

    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
