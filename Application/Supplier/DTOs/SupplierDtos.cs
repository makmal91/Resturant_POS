namespace POSSystem.Application.Supplier.DTOs;

public class SupplierDto
{
    public int Id { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int? AccountId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public class CreateSupplierDto
{
    public string? SupplierCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
}

public class UpdateSupplierDto
{
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
}
