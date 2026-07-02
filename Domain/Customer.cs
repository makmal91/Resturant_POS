namespace POSSystem.Domain;

public enum CustomerType
{
    Retail    = 1,
    Wholesale = 2,
    VIP       = 3
}

public class Customer : BaseEntity
{
    public string  CustomerCode    { get; set; } = string.Empty;
    public string  Name            { get; set; } = string.Empty;
    public string? Phone           { get; set; }
    public string? Email           { get; set; }
    public string? Address         { get; set; }
    public int?    CountryId       { get; set; }
    public int?    CityId          { get; set; }
    public string? CNIC            { get; set; }
    public CustomerType CustomerType { get; set; } = CustomerType.Retail;
    public bool   Status           { get; set; } = true;
    public decimal OpeningBalance  { get; set; }
    public decimal CreditLimit     { get; set; }
    public int    LoyaltyPoints    { get; set; }
    public bool   IsWalkIn         { get; set; }   // system-protected walk-in customer
    public int?   AccountId        { get; set; }

    public virtual GlAccount? GlAccount { get; set; }
    public virtual Branch Branch { get; set; } = null!;
    public virtual Country? Country { get; set; }
    public virtual City? City { get; set; }
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
