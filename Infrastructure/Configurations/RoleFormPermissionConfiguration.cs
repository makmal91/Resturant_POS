using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class RoleFormPermissionConfiguration : IEntityTypeConfiguration<RoleFormPermission>
{
    public void Configure(EntityTypeBuilder<RoleFormPermission> builder)
    {
        builder.ToTable("RoleFormPermissions");
        builder.HasKey(rfp => rfp.Id);

        builder.HasIndex(rfp => new { rfp.RoleId, rfp.FormId })
            .IsUnique()
            .HasDatabaseName("idx_role_form_permission");

        builder.HasQueryFilter(rfp => !rfp.IsDeleted);

        builder.HasOne(rfp => rfp.Role)
            .WithMany()
            .HasForeignKey(rfp => rfp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rfp => rfp.Form)
            .WithMany(f => f.RoleFormPermissions)
            .HasForeignKey(rfp => rfp.FormId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
