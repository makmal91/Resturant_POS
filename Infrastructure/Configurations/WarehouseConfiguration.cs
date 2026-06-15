using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name).IsRequired().HasMaxLength(150);
        builder.Property(w => w.Code).HasMaxLength(30);
        builder.Property(w => w.Address).HasMaxLength(500);
        builder.Property(w => w.IsActive).HasDefaultValue(true);

        builder.HasIndex(w => new { w.BusinessId, w.BranchId, w.Name })
            .IsUnique()
            .HasDatabaseName("idx_warehouse_business_branch_name");

        builder.HasOne(w => w.Branch)
            .WithMany()
            .HasForeignKey(w => w.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
