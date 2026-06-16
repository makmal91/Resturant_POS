using System;
using System.Collections.Generic;

namespace POSSystem.Domain;

public class Business : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public byte[]? Logo { get; set; }
    public string? LogoFileName { get; set; }
    public string? LogoContentType { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public int CurrencyId { get; set; } = 1;
    public string Currency { get; set; } = "PKR";
    public string TimeZone { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;

    public virtual Currency CurrencyEntity { get; set; } = null!;
    public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
