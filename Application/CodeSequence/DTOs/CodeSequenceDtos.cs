namespace POSSystem.Application.CodeSequence.DTOs;

public class CodeSequenceListDto
{
    public int Id { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public long LastNumber { get; set; }
    public string NextCodePreview { get; set; } = string.Empty;
    public string ResetType { get; set; } = string.Empty;
    public DateTime? LastResetDate { get; set; }
}

public class UpdateCodeSequenceDto
{
    public long LastNumber { get; set; }
}
