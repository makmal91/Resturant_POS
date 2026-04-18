namespace POSSystem.Domain;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public int? ModifiedById { get; set; }
    public string? ModifiedByName { get; set; }
    public bool IsDeleted { get; set; }
    public int BranchId { get; set; }
}
