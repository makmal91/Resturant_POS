using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SupplierCode).HasMaxLength(50);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.ContactPerson).HasMaxLength(150);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(150);
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.TaxNumber).HasMaxLength(50);
        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.HasIndex(s => new { s.BusinessId, s.BranchId, s.Name })
            .HasDatabaseName("idx_supplier_business_branch_name");

        builder.HasIndex(s => new { s.BusinessId, s.BranchId, s.SupplierCode })
            .IsUnique()
            .HasFilter("[SupplierCode] <> '' AND [IsDeleted] = 0")
            .HasDatabaseName("idx_supplier_branch_code");

        builder.HasOne(s => s.Branch)
            .WithMany()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
