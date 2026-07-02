namespace POSSystem.Domain;

public class Supplier : BaseEntity
{
    public string SupplierCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int? AccountId { get; set; }

    public virtual GlAccount? GlAccount { get; set; }
    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
