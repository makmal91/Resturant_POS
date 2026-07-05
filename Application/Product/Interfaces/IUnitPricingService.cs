using POSSystem.Domain;
using ProductEntity = POSSystem.Domain.Product;

namespace POSSystem.Application.Product.Interfaces;

/// <summary>
/// Resolves the price to use for a product unit. Prices are fully manual per unit
/// (no auto-calculation from a base price or conversion factor). These helpers simply
/// return the stored unit price, falling back to the product-level price only when a
/// unit price is missing (legacy safety).
/// </summary>
public interface IUnitPricingService
{
    decimal GetEffectiveSellingPrice(ProductEntity product, ProductUnit unit, ProductVariant? variant, PricingType pricingType);
    decimal GetEffectiveWholesalePrice(ProductEntity product, ProductUnit unit);
    decimal GetEffectiveCostPrice(ProductEntity product, ProductUnit unit);
}
