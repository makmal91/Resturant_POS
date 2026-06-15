namespace POSSystem.Domain;

public class Warehouse : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public virtual ICollection<StockLedger> StockLedgerEntries { get; set; } = new List<StockLedger>();
}
