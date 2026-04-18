using System;

namespace POSSystem.Domain;

public class StockMovement : BaseEntity
{
    public int ItemId { get; set; }
    public decimal Quantity { get; set; }
    public StockMovementType Type { get; set; }
    public int? BranchFromId { get; set; }
    public int? BranchToId { get; set; }

    public virtual Branch Branch { get; set; } = null!;
    public virtual InventoryItem Item { get; set; } = null!;
    public virtual Branch? BranchFrom { get; set; }
    public virtual Branch? BranchTo { get; set; }
}
