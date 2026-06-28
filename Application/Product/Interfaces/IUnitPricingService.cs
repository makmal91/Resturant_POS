using POSSystem.Application.Product.DTOs;
using POSSystem.Domain;
using ProductEntity = POSSystem.Domain.Product;

namespace POSSystem.Application.Product.Interfaces;

public interface IUnitPricingService
{
    /// <summary>Auto price = BasePrice ÷ ConversionFactor (child units in 1 base unit).</summary>
    decimal CalculateAutoPrice(decimal baseUnitPrice, decimal conversionFactor, bool isBaseUnit);

    decimal GetEffectiveSellingPrice(ProductEntity product, ProductUnit unit, ProductVariant? variant, PricingType pricingType);
    decimal GetEffectiveWholesalePrice(ProductEntity product, ProductUnit unit);
    decimal GetEffectiveCostPrice(ProductEntity product, ProductUnit unit);

    void RecalculateAutoPrices(ProductEntity product, bool forceAll = false);

    Task<ProductUnitPricingDto?> GetProductUnitPricingAsync(int productId, int businessId, int branchId);
    Task<CalculateUnitPriceResponseDto> CalculateUnitPriceAsync(int productId, CalculateUnitPriceRequestDto request);
    Task<ProductUnitPricingDto> SaveUnitPriceOverrideAsync(int productId, int productUnitId, SaveUnitPriceOverrideDto dto);
    Task<ProductUnitPricingDto> UpdateBasePriceAndRecalculateAsync(int productId, UpdateBasePriceDto dto);
}
