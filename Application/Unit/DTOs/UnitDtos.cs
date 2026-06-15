namespace POSSystem.Application.Unit.DTOs;

public class UnitDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; } = 1;
    public bool Status { get; set; } = true;
    public bool IsActive => Status;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
}

public class CreateUnitDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; } = 1;
    public bool Status { get; set; } = true;
    public bool? IsActive { get; set; }
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
}

public class UpdateUnitDto : CreateUnitDto
{
}
