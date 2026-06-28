using POSSystem.Domain;
using ProductEntity = POSSystem.Domain.Product;

namespace POSSystem.Application.Barcode.Helpers;

public static class BarcodeValueHelper
{
    public static string Generate(int productId, int productUnitId, int? variantId)
        => $"AKH{productId:D5}{productUnitId:D2}{(variantId ?? 0):D2}";

    public static string Resolve(
        ProductEntity product,
        int productUnitId,
        int? variantId,
        IEnumerable<ProductBarcode> barcodes)
    {
        var activeBarcodes = barcodes.Where(b => !b.IsDeleted && !string.IsNullOrWhiteSpace(b.BarcodeValue)).ToList();

        var exact = activeBarcodes.FirstOrDefault(b =>
            MatchesUnit(b, productUnitId, product) &&
            MatchesVariant(b, variantId, product));

        if (exact != null)
            return exact.BarcodeValue;

        var unitOnly = activeBarcodes.FirstOrDefault(b =>
            MatchesUnit(b, productUnitId, product) && b.ProductVariantId == null);
        if (unitOnly != null)
            return unitOnly.BarcodeValue;

        var variantOnly = variantId.HasValue
            ? activeBarcodes.FirstOrDefault(b =>
                b.ProductUnitId == null && MatchesVariant(b, variantId, product))
            : null;
        if (variantOnly != null)
            return variantOnly.BarcodeValue;

        var primary = activeBarcodes.FirstOrDefault(b => b.IsPrimary);
        if (primary != null)
            return primary.BarcodeValue;

        return Generate(product.Id, productUnitId, variantId);
    }

    private static bool MatchesUnit(ProductBarcode barcode, int productUnitId, ProductEntity product)
    {
        if (barcode.ProductUnitId.HasValue)
            return barcode.ProductUnitId.Value == productUnitId;

        if (barcode.ProductUnit != null)
            return barcode.ProductUnit.Id == productUnitId;

        return productUnitId == ResolveDefaultUnitId(product);
    }

    private static bool MatchesVariant(ProductBarcode barcode, int? variantId, ProductEntity product)
    {
        if (!variantId.HasValue)
            return barcode.ProductVariantId == null;

        if (barcode.ProductVariantId.HasValue)
            return barcode.ProductVariantId.Value == variantId.Value;

        if (barcode.ProductVariant != null)
            return barcode.ProductVariant.Id == variantId.Value;

        return false;
    }

    public static int ResolveDefaultUnitId(ProductEntity product)
    {
        var units = product.Units.Where(u => !u.IsDeleted).ToList();
        if (units.Count == 0)
            return 0;

        var baseUnit = units.FirstOrDefault(u => u.IsBaseUnit) ?? units.First();
        return baseUnit.Id;
    }
}
