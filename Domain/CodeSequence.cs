namespace POSSystem.Domain;

public class CodeSequence
{
    public int Id { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public long LastNumber { get; set; }
    public CodeResetType ResetType { get; set; } = CodeResetType.None;
    public DateTime? LastResetDate { get; set; }
}
