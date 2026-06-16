using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class CodeSequenceConfiguration : IEntityTypeConfiguration<CodeSequence>
{
    public void Configure(EntityTypeBuilder<CodeSequence> builder)
    {
        builder.ToTable("CodeSequences");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ModuleName).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Prefix).IsRequired().HasMaxLength(20);
        builder.Property(s => s.LastNumber).IsRequired();
        builder.Property(s => s.ResetType).HasConversion<int>();

        builder.HasIndex(s => new { s.ModuleName, s.BranchId })
            .IsUnique()
            .HasDatabaseName("idx_codesequence_module_branch");
    }
}
