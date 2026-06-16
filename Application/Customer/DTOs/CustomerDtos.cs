using POSSystem.Domain;

namespace POSSystem.Application.Customer.DTOs;

// ─── List / Summary ───────────────────────────────────────────────────────────

public class CustomerListDto
{
    public int    Id             { get; set; }
    public string CustomerCode   { get; set; } = string.Empty;
    public string Name           { get; set; } = string.Empty;
    public string? Phone         { get; set; }
    public string? Email         { get; set; }
    public int?   CountryId      { get; set; }
    public int?   CityId         { get; set; }
    public string? CityName      { get; set; }
    public CustomerType CustomerType { get; set; }
    public bool   Status         { get; set; }
    public decimal CreditLimit   { get; set; }
    public bool   IsWalkIn       { get; set; }
    public DateTime CreatedAt  { get; set; }
}

// ─── Detail ───────────────────────────────────────────────────────────────────

public class CustomerDetailDto : CustomerListDto
{
    public string? Address       { get; set; }
    public string? CNIC          { get; set; }
    public decimal OpeningBalance { get; set; }
    public int    LoyaltyPoints  { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

// ─── Create ───────────────────────────────────────────────────────────────────

public class CreateCustomerDto
{
    public string  Name          { get; set; } = string.Empty;
    public string? CustomerCode  { get; set; }
    public string? Phone         { get; set; }
    public string? Email         { get; set; }
    public string? Address       { get; set; }
    public int?    CountryId     { get; set; }
    public int?    CityId        { get; set; }
    public string? CNIC          { get; set; }
    public CustomerType CustomerType { get; set; } = CustomerType.Retail;
    public bool   Status         { get; set; } = true;
    public decimal OpeningBalance { get; set; }
    public decimal CreditLimit   { get; set; }
    public int    BusinessId     { get; set; }
    public int    BranchId       { get; set; }
}

// ─── Update ───────────────────────────────────────────────────────────────────

public class UpdateCustomerDto : CreateCustomerDto { }

// ─── Quick-create from POS ────────────────────────────────────────────────────

public class QuickCreateCustomerDto
{
    public string  Name     { get; set; } = string.Empty;
    public string? Phone    { get; set; }
    public int     BusinessId { get; set; }
    public int     BranchId  { get; set; }
}

// ─── Paged filter ─────────────────────────────────────────────────────────────

public class CustomerFilterDto
{
    public int     BusinessId    { get; set; }
    public int     BranchId      { get; set; }
    public string? Search        { get; set; }
    public CustomerType? Type    { get; set; }
    public bool?   IsActive      { get; set; }
    public int     Page          { get; set; } = 1;
    public int     PageSize      { get; set; } = 25;
}
