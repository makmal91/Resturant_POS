using System.ComponentModel.DataAnnotations.Schema;

namespace POSSystem.Domain;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public int BranchId { get; set; }

    [Column("CreatedDate")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("CreatedById")]
    public int? CreatedBy { get; set; }

    [Column("UpdatedDate")]
    public DateTime? ModifiedAt { get; set; }

    [Column("ModifiedById")]
    public int? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; }
}
