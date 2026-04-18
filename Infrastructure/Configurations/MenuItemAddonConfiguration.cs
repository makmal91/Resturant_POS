using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class MenuItemAddonConfiguration : IEntityTypeConfiguration<MenuItemAddon>
{
    public void Configure(EntityTypeBuilder<MenuItemAddon> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.Price)
            .HasPrecision(10, 2);

        // Indexes
        builder.HasIndex(a => a.MenuItemId)
            .HasDatabaseName("idx_addon_menuitemid");

        builder.HasIndex(a => a.BranchId)
            .HasDatabaseName("idx_addon_branchid");

        // Relationships
        builder.HasOne(a => a.MenuItem)
            .WithMany(mi => mi.Addons)
            .HasForeignKey(a => a.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}