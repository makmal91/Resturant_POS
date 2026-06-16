using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class PermissionModuleConfiguration : IEntityTypeConfiguration<PermissionModule>
{
    public void Configure(EntityTypeBuilder<PermissionModule> builder)
    {
        builder.ToTable("Modules");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.ModuleName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.ModuleKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Route)
            .HasMaxLength(200);

        builder.Property(m => m.Icon)
            .HasMaxLength(50);

        builder.HasIndex(m => m.ModuleKey)
            .IsUnique()
            .HasFilter("[ModuleKey] <> '' AND [IsDeleted] = 0")
            .HasDatabaseName("idx_module_key");

        builder.HasQueryFilter(m => !m.IsDeleted);

        builder.HasOne(m => m.ParentModule)
            .WithMany(m => m.ChildModules)
            .HasForeignKey(m => m.ParentModuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
