using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class AdjustmentTypeConfiguration : IEntityTypeConfiguration<AdjustmentType>
{
    public void Configure(EntityTypeBuilder<AdjustmentType> builder)
    {
        builder.ToTable("AdjustmentTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.Name }).IsUnique();
        builder.HasOne(x => x.ExpenseAccount).WithMany().HasForeignKey(x => x.ExpenseAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.IncomeAccount).WithMany().HasForeignKey(x => x.IncomeAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AdjustmentNo).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.IsReversed).HasDefaultValue(false);
        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.AdjustmentNo }).IsUnique();
        builder.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AdjustmentType).WithMany().HasForeignKey(x => x.AdjustmentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockAdjustmentLineConfiguration : IEntityTypeConfiguration<StockAdjustmentLine>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentLine> builder)
    {
        builder.ToTable("StockAdjustmentDetails");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UnitQuantity).HasPrecision(18, 4);
        builder.Property(x => x.ConversionFactor).HasPrecision(18, 4);
        builder.Property(x => x.BaseQuantity).HasPrecision(18, 4);
        builder.Property(x => x.CostPrice).HasPrecision(18, 4);
        builder.Property(x => x.TotalCost).HasPrecision(18, 2);
        builder.HasOne(x => x.StockAdjustment).WithMany(a => a.Lines).HasForeignKey(x => x.StockAdjustmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}
