using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(sm => sm.Id);

        builder.Property(sm => sm.Quantity)
            .HasPrecision(12, 2);

        // Indexes
        builder.HasIndex(sm => sm.ItemId)
            .HasDatabaseName("idx_stockmovement_itemid");

        builder.HasIndex(sm => sm.BranchId)
            .HasDatabaseName("idx_stockmovement_branchid");

        builder.HasIndex(sm => sm.Type)
            .HasDatabaseName("idx_stockmovement_type");

        builder.HasIndex(sm => new { sm.BranchId, sm.CreatedDate })
            .HasDatabaseName("idx_stockmovement_branch_createddate");

        // Relationships
        builder.HasOne(sm => sm.Item)
            .WithMany(i => i.StockMovements)
            .HasForeignKey(sm => sm.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(sm => sm.BranchFrom)
            .WithMany()
            .HasForeignKey(sm => sm.BranchFromId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sm => sm.BranchTo)
            .WithMany()
            .HasForeignKey(sm => sm.BranchToId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
