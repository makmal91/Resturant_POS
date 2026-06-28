using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.ProductCode).IsRequired().HasMaxLength(50);
        builder.Property(p => p.SKU).HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Status).HasDefaultValue(true);
        builder.Property(p => p.CostPrice).HasPrecision(18, 2);
        builder.Property(p => p.SellingPrice).HasPrecision(18, 2);
        builder.Property(p => p.WholesalePrice).HasPrecision(18, 2);
        builder.Property(p => p.UseAutoUnitPricing).HasDefaultValue(true);
        builder.Property(p => p.DiscountType).HasConversion<int?>();
        builder.Property(p => p.DiscountValue).HasPrecision(18, 2);
        builder.Property(p => p.AllowNegativeStock).HasDefaultValue(false);
        builder.Property(p => p.EnableLowStockAlert).HasDefaultValue(false);
        builder.Property(p => p.LowStockAlertLevel).HasPrecision(18, 4);
        builder.Property(p => p.OpeningStock).HasPrecision(18, 4).HasDefaultValue(0m);
        builder.Property(p => p.OpeningStockVariantWise).HasDefaultValue(false);

        builder.HasOne(p => p.BaseUnit)
            .WithMany()
            .HasForeignKey(p => p.BaseUnitId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(p => p.BaseUnitId).HasDatabaseName("idx_product_base_unit_id");

        builder.HasIndex(p => new { p.BusinessId, p.BranchId, p.ProductCode })
            .IsUnique()
            .HasDatabaseName("idx_product_business_branch_code");

        builder.HasIndex(p => new { p.BusinessId, p.BranchId, p.SKU })
            .HasDatabaseName("idx_product_business_branch_sku");

        builder.HasIndex(p => new { p.BusinessId, p.BranchId, p.CategoryId })
            .HasDatabaseName("idx_product_business_branch_category");

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.SubCategory)
            .WithMany()
            .HasForeignKey(p => p.SubCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Brand)
            .WithMany()
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Units)
            .WithOne(u => u.Product)
            .HasForeignKey(u => u.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Barcodes)
            .WithOne(b => b.Product)
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Images)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductUnitConfiguration : IEntityTypeConfiguration<ProductUnit>
{
    public void Configure(EntityTypeBuilder<ProductUnit> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.UnitName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.ConversionFactor).HasPrecision(18, 4);
        builder.Property(u => u.CostPrice).HasPrecision(18, 2);
        builder.Property(u => u.SellingPrice).HasPrecision(18, 2);
        builder.Property(u => u.WholesalePrice).HasPrecision(18, 2);
        builder.Property(u => u.IsPriceOverridden).HasDefaultValue(false);
        builder.HasIndex(u => new { u.ProductId, u.UnitName }).HasDatabaseName("idx_productunit_product_name");
        builder.HasIndex(u => u.UnitId).HasDatabaseName("idx_productunit_unit_id");

        builder.HasOne(u => u.Unit)
            .WithMany()
            .HasForeignKey(u => u.UnitId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.VariantName).IsRequired().HasMaxLength(150);
        builder.Property(v => v.Size).HasMaxLength(50);
        builder.Property(v => v.Color).HasMaxLength(50);
        builder.Property(v => v.SKU).HasMaxLength(100);
        builder.Property(v => v.AdditionalPrice).HasPrecision(18, 2);
        builder.Property(v => v.CostPriceOverride).HasPrecision(18, 2);
        builder.Property(v => v.SellingPriceOverride).HasPrecision(18, 2);
        builder.Property(v => v.Status).HasDefaultValue(true);
        builder.HasIndex(v => new { v.ProductId, v.SKU }).HasDatabaseName("idx_productvariant_product_sku");
    }
}

public class ProductBarcodeConfiguration : IEntityTypeConfiguration<ProductBarcode>
{
    public void Configure(EntityTypeBuilder<ProductBarcode> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BarcodeValue).IsRequired().HasMaxLength(100);
        builder.HasIndex(b => b.BarcodeValue)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("idx_productbarcode_value");

        builder.HasOne(b => b.ProductUnit)
            .WithMany()
            .HasForeignKey(b => b.ProductUnitId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(b => b.ProductVariant)
            .WithMany()
            .HasForeignKey(b => b.ProductVariantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.FileName).HasMaxLength(255);
        builder.Property(i => i.ContentType).HasMaxLength(100);
        builder.Property(i => i.ImageData).IsRequired();
        builder.HasIndex(i => new { i.ProductId, i.IsPrimary }).HasDatabaseName("idx_productimage_product_primary");
    }
}
