using POSSystem.Application.Product.Interfaces;
using POSSystem.Domain;
using ProductEntity = POSSystem.Domain.Product;

namespace POSSystem.Application.Product.Services;

/// <summary>
/// Unit pricing with a base price that scales by the conversion factor.
/// - Product-level Cost/Selling/Wholesale prices are the BASE (smallest) unit prices.
/// - A unit may store its own manual Cost/Selling/Wholesale price (an override the user can edit).
/// - When a unit has no manual price, the effective price is auto-derived as
///   base price × ConversionFactor (e.g. base = PCS at 100, Package factor 3 → 300).
/// - Variant pricing (override / additional price) applies to the base unit and scales with it.
/// </summary>
public class UnitPricingService : IUnitPricingService
{
    public decimal GetEffectiveSellingPrice(
        ProductEntity product, ProductUnit unit, ProductVariant? variant, PricingType pricingType)
    {
        if (pricingType == PricingType.Wholesale)
            return GetEffectiveWholesalePrice(product, unit);

        var factor = ResolveFactor(unit);

        // Selling price for ONE base unit (variant override wins, else base/product price + variant add-on).
        decimal perBaseSelling;
        if (variant?.SellingPriceOverride.HasValue == true)
            perBaseSelling = variant.SellingPriceOverride.Value;
        else
        {
            var baseUnitPrice = unit.IsBaseUnit && unit.SellingPrice.HasValue
                ? unit.SellingPrice.Value
                : product.SellingPrice;
            perBaseSelling = baseUnitPrice + (variant?.AdditionalPrice ?? 0);
        }

        if (unit.IsBaseUnit)
            return perBaseSelling;

        // Non-base unit: a manual per-unit price wins; otherwise scale the base price by the factor.
        if (unit.SellingPrice.HasValue)
            return unit.SellingPrice.Value;

        return perBaseSelling * factor;
    }

    public decimal GetEffectiveWholesalePrice(ProductEntity product, ProductUnit unit)
    {
        if (unit.IsBaseUnit)
            return unit.WholesalePrice ?? product.WholesalePrice;

        if (unit.WholesalePrice.HasValue)
            return unit.WholesalePrice.Value;

        return product.WholesalePrice * ResolveFactor(unit);
    }

    public decimal GetEffectiveCostPrice(ProductEntity product, ProductUnit unit)
    {
        if (unit.IsBaseUnit)
            return unit.CostPrice ?? product.CostPrice;

        if (unit.CostPrice.HasValue)
            return unit.CostPrice.Value;

        return product.CostPrice * ResolveFactor(unit);
    }

    private static decimal ResolveFactor(ProductUnit unit)
        => unit.IsBaseUnit ? 1m : (unit.ConversionFactor > 0 ? unit.ConversionFactor : 1m);
}
