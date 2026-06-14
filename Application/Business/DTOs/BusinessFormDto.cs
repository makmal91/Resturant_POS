namespace POSSystem.Application.Business.DTOs;

public class BusinessFormDto
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? Currency { get; set; }
    public string? TimeZone { get; set; }
    public string? IsActive { get; set; }
    public string? RemoveLogo { get; set; }
}
