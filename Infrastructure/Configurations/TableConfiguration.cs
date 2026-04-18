using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class TableConfiguration : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TableNumber)
            .IsRequired();

        builder.Property(t => t.Capacity)
            .IsRequired();

        builder.Property(t => t.Floor)
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(t => t.BranchId)
            .HasDatabaseName("idx_table_branchid");

        builder.HasIndex(t => new { t.BranchId, t.TableNumber })
            .IsUnique()
            .HasDatabaseName("idx_table_branch_number");

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("idx_table_status");

        // Relationships
        builder.HasMany(t => t.Orders)
            .WithOne(o => o.Table)
            .HasForeignKey(o => o.TableId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
