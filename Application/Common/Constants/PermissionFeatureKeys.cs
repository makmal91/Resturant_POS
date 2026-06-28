namespace POSSystem.Application.Common.Constants;

public static class PermissionFeatureKeys
{
    public const string UnitEnable = "product.unit.enable";
    public const string VariantEnable = "product.variant.enable";
    public const string StockEnable = "product.stock.enable";
    public const string BarcodeEnable = "product.barcode.enable";

    public static readonly IReadOnlyList<string> All =
    [
        UnitEnable,
        VariantEnable,
        StockEnable,
        BarcodeEnable,
    ];

    public static bool IsFeatureKey(string formCode) =>
        !string.IsNullOrWhiteSpace(formCode) &&
        formCode.EndsWith(".enable", StringComparison.OrdinalIgnoreCase);

    public static string? MapToModule(string featureKey) =>
        featureKey switch
        {
            _ when string.Equals(featureKey, UnitEnable, StringComparison.OrdinalIgnoreCase) => PermissionModules.Units,
            _ when string.Equals(featureKey, VariantEnable, StringComparison.OrdinalIgnoreCase) => PermissionModules.Variants,
            _ when string.Equals(featureKey, StockEnable, StringComparison.OrdinalIgnoreCase) => PermissionModules.Stock,
            _ when string.Equals(featureKey, BarcodeEnable, StringComparison.OrdinalIgnoreCase) => PermissionModules.Barcodes,
            _ => null
        };
}
