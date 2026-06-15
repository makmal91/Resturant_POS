using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class MeasurementUnitConfiguration : IEntityTypeConfiguration<MeasurementUnit>
{
    public void Configure(EntityTypeBuilder<MeasurementUnit> builder)
    {
        builder.ToTable("Units");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Code).HasMaxLength(20);
        builder.Property(u => u.Description).HasMaxLength(500);
        builder.Property(u => u.ConversionFactor).HasPrecision(18, 4).HasDefaultValue(1);
        builder.Property(u => u.Status).HasDefaultValue(true);

        builder.HasIndex(u => new { u.BusinessId, u.BranchId, u.Name })
            .IsUnique()
            .HasDatabaseName("idx_unit_business_branch_name");

        builder.HasIndex(u => new { u.BusinessId, u.BranchId, u.Code })
            .HasDatabaseName("idx_unit_business_branch_code");

        builder.HasOne(u => u.Branch)
            .WithMany()
            .HasForeignKey(u => u.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
