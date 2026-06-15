namespace POSSystem.Application.Brand.DTOs;

public class BrandDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Status { get; set; }
    public bool HasImage { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public int? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public int? ModifiedById { get; set; }
    public string? ModifiedByName { get; set; }
}

public class BrandDetailDto : BrandDto
{
    public string? ImageContentType { get; set; }
    public string? ImageFileName { get; set; }
}

public class CreateBrandDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Status { get; set; } = true;
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
}

public class UpdateBrandDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Status { get; set; } = true;
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
}

public class BrandFormDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Status { get; set; }
    public int BranchId { get; set; }
    public string? RemoveImage { get; set; }
}

public class BrandImageDto
{
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string? ImageContentType { get; set; }
    public string? ImageFileName { get; set; }
}

public class BrandStatusPatchDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public List<BrandStatusItemDto> Items { get; set; } = new();
}

public class BrandStatusItemDto
{
    public int Id { get; set; }
    public bool Status { get; set; }
}
