using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class ModuleFormConfiguration : IEntityTypeConfiguration<ModuleForm>
{
    public void Configure(EntityTypeBuilder<ModuleForm> builder)
    {
        builder.ToTable("ModuleForms");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.FormName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.FormCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.Route)
            .HasMaxLength(200);

        builder.HasIndex(f => f.FormCode)
            .IsUnique()
            .HasDatabaseName("idx_module_form_code");

        builder.HasQueryFilter(f => !f.IsDeleted);

        builder.HasOne(f => f.Module)
            .WithMany(m => m.Forms)
            .HasForeignKey(f => f.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
