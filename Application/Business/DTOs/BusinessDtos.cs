namespace POSSystem.Application.Business.DTOs;

public class BusinessListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "UTC";
    public bool IsActive { get; set; }
    public bool HasLogo { get; set; }
}

public class BusinessDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public int CurrencyId { get; set; } = 1;
    public string Currency { get; set; } = "PKR";
    public string TimeZone { get; set; } = "UTC";
    public bool IsActive { get; set; }
    public bool HasLogo { get; set; }
    public string? LogoFileName { get; set; }
    public string? LogoContentType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public class CreateBusinessDto
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public int? CurrencyId { get; set; }
    public string? Currency { get; set; }
    public string? TimeZone { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateBusinessDto
{
    public string? Name { get; set; }
    public string? LegalName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public int? CurrencyId { get; set; }
    public string? Currency { get; set; }
    public string? TimeZone { get; set; }
    public bool? IsActive { get; set; }
}

public class BusinessLogoDto
{
    public byte[] Logo { get; set; } = Array.Empty<byte>();
    public string? LogoFileName { get; set; }
    public string? LogoContentType { get; set; }
}
