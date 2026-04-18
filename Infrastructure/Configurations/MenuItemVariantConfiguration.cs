using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class MenuItemVariantConfiguration : IEntityTypeConfiguration<MenuItemVariant>
{
    public void Configure(EntityTypeBuilder<MenuItemVariant> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(v => v.Price)
            .HasPrecision(10, 2);

        // Indexes
        builder.HasIndex(v => v.MenuItemId)
            .HasDatabaseName("idx_variant_menuitemid");

        builder.HasIndex(v => v.BranchId)
            .HasDatabaseName("idx_variant_branchid");

        // Relationships
        builder.HasOne(v => v.MenuItem)
            .WithMany(mi => mi.Variants)
            .HasForeignKey(v => v.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
