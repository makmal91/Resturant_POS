using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class AppMenuConfiguration : IEntityTypeConfiguration<AppMenu>
{
    public void Configure(EntityTypeBuilder<AppMenu> builder)
    {
        builder.ToTable("Menus");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Route)
            .HasMaxLength(200);

        builder.Property(m => m.Icon)
            .HasMaxLength(50);

        builder.Property(m => m.ModuleName)
            .HasMaxLength(100);

        builder.HasIndex(m => m.DisplayOrder)
            .HasDatabaseName("idx_menus_displayorder");

        builder.HasIndex(m => m.ParentId)
            .HasDatabaseName("idx_menus_parentid");

        builder.HasOne(m => m.Parent)
            .WithMany(m => m.Children)
            .HasForeignKey(m => m.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
