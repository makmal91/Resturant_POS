namespace POSSystem.Application.Branch.DTOs;

public class BranchSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class BranchListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class BranchDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class CreateBranchDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int BusinessId { get; set; }
    public int CompanyId { get; set; }
    public int CountryId { get; set; }
    public int CityId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateBranchDto
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int? BusinessId { get; set; }
    public int? CompanyId { get; set; }
    public int? CountryId { get; set; }
    public int? CityId { get; set; }
    public bool? IsActive { get; set; }
}

public class CountryListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class CityListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CountryId { get; set; }
}
