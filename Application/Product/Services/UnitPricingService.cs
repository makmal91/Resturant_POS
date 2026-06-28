using POSSystem.Application.Product.DTOs;
using POSSystem.Application.Product.Interfaces;
using POSSystem.Domain;
using ProductEntity = POSSystem.Domain.Product;

namespace POSSystem.Application.Product.Services;

/// <summary>
/// Smart unit pricing:
/// - User sets base unit price on Product (SellingPrice / WholesalePrice / CostPrice).
/// - Child unit auto price = BasePrice ÷ ConversionFactor
///   (ConversionFactor = child units in 1 base unit; e.g. 50 KG per Bori → 1250 ÷ 50 = 25).
/// - Manual override on ProductUnit stops auto calculation for that unit.
/// </summary>
public class UnitPricingService : IUnitPricingService
{
    private readonly IProductRepository _productRepository;

    public UnitPricingService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public decimal CalculateAutoPrice(decimal baseUnitPrice, decimal conversionFactor, bool isBaseUnit)
    {
        if (isBaseUnit)
            return baseUnitPrice;

        if (conversionFactor <= 0)
            throw new InvalidOperationException("Conversion factor must be greater than zero.");

        return Math.Round(baseUnitPrice / conversionFactor, 2, MidpointRounding.AwayFromZero);
    }

    public decimal GetEffectiveSellingPrice(
        ProductEntity product, ProductUnit unit, ProductVariant? variant, PricingType pricingType)
    {
        if (pricingType == PricingType.Wholesale)
            return GetEffectiveWholesalePrice(product, unit);

        if (unit.IsPriceOverridden && unit.SellingPrice.HasValue)
            return unit.SellingPrice.Value;

        if (!product.UseAutoUnitPricing && unit.SellingPrice.HasValue)
            return unit.SellingPrice.Value;

        var auto = CalculateAutoPrice(product.SellingPrice, unit.ConversionFactor, unit.IsBaseUnit);

        if (variant?.SellingPriceOverride.HasValue == true && unit.IsBaseUnit)
            return variant.SellingPriceOverride.Value;

        if (variant != null && unit.IsBaseUnit)
            return product.SellingPrice + variant.AdditionalPrice;

        return auto;
    }

    public decimal GetEffectiveWholesalePrice(ProductEntity product, ProductUnit unit)
    {
        if (unit.IsPriceOverridden && unit.WholesalePrice.HasValue)
            return unit.WholesalePrice.Value;

        if (!product.UseAutoUnitPricing && unit.WholesalePrice.HasValue)
            return unit.WholesalePrice.Value;

        return CalculateAutoPrice(product.WholesalePrice, unit.ConversionFactor, unit.IsBaseUnit);
    }

    public decimal GetEffectiveCostPrice(ProductEntity product, ProductUnit unit)
    {
        if (unit.IsPriceOverridden && unit.CostPrice.HasValue)
            return unit.CostPrice.Value;

        if (!product.UseAutoUnitPricing && unit.CostPrice.HasValue)
            return unit.CostPrice.Value;

        return CalculateAutoPrice(product.CostPrice, unit.ConversionFactor, unit.IsBaseUnit);
    }

    public void RecalculateAutoPrices(ProductEntity product, bool forceAll = false)
    {
        foreach (var unit in product.Units.Where(u => !u.IsDeleted))
        {
            if (unit.IsBaseUnit)
            {
                unit.SellingPrice = product.SellingPrice;
                unit.WholesalePrice = product.WholesalePrice;
                unit.CostPrice = product.CostPrice;
                unit.IsPriceOverridden = false;
                continue;
            }

            if (!product.UseAutoUnitPricing)
                continue;

            if (unit.IsPriceOverridden && !forceAll)
                continue;

            unit.CostPrice = CalculateAutoPrice(product.CostPrice, unit.ConversionFactor, false);
            unit.SellingPrice = CalculateAutoPrice(product.SellingPrice, unit.ConversionFactor, false);
            unit.WholesalePrice = CalculateAutoPrice(product.WholesalePrice, unit.ConversionFactor, false);
            unit.IsPriceOverridden = false;
        }
    }

    public async Task<ProductUnitPricingDto?> GetProductUnitPricingAsync(int productId, int businessId, int branchId)
    {
        var product = await _productRepository.GetByIdAsync(productId, businessId, branchId);
        return product == null ? null : MapPricingDto(product);
    }

    public async Task<CalculateUnitPriceResponseDto> CalculateUnitPriceAsync(
        int productId, CalculateUnitPriceRequestDto request)
    {
        var product = await _productRepository.GetByIdAsync(productId, request.BusinessId, request.BranchId)
            ?? throw new InvalidOperationException("Product not found.");

        var unit = product.Units.FirstOrDefault(u => u.Id == request.ProductUnitId && !u.IsDeleted)
            ?? throw new InvalidOperationException("Product unit not found.");

        var baseSelling = request.BaseSellingPrice ?? product.SellingPrice;
        var baseWholesale = request.BaseWholesalePrice ?? product.WholesalePrice;

        var calculated = request.PricingType == PricingType.Wholesale
            ? CalculateAutoPrice(baseWholesale, unit.ConversionFactor, unit.IsBaseUnit)
            : CalculateAutoPrice(baseSelling, unit.ConversionFactor, unit.IsBaseUnit);

        var effective = request.PricingType == PricingType.Wholesale
            ? GetEffectiveWholesalePrice(product, unit)
            : GetEffectiveSellingPrice(product, unit, null, request.PricingType);

        return new CalculateUnitPriceResponseDto
        {
            ProductUnitId = unit.Id,
            UnitName = unit.UnitName,
            ConversionFactor = unit.ConversionFactor,
            CalculatedPrice = calculated,
            OverridePrice = unit.IsPriceOverridden
                ? (request.PricingType == PricingType.Wholesale ? unit.WholesalePrice : unit.SellingPrice)
                : null,
            IsPriceOverridden = unit.IsPriceOverridden,
            EffectivePrice = effective,
            UseAutoUnitPricing = product.UseAutoUnitPricing
        };
    }

    public async Task<ProductUnitPricingDto> SaveUnitPriceOverrideAsync(
        int productId, int productUnitId, SaveUnitPriceOverrideDto dto)
    {
        var product = await _productRepository.GetByIdAsync(productId, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Product not found.");

        var unit = product.Units.FirstOrDefault(u => u.Id == productUnitId && !u.IsDeleted)
            ?? throw new InvalidOperationException("Product unit not found.");

        if (unit.IsBaseUnit)
            throw new InvalidOperationException("Base unit price is controlled by product base price.");

        if (dto.IsOverride)
        {
            if (dto.CustomSellingPrice < 0)
                throw new InvalidOperationException("Custom selling price cannot be negative.");

            unit.SellingPrice = dto.CustomSellingPrice;
            unit.WholesalePrice = dto.CustomWholesalePrice ?? dto.CustomSellingPrice;
            unit.CostPrice = dto.CustomCostPrice;
            unit.IsPriceOverridden = true;
        }
        else
        {
            unit.IsPriceOverridden = false;
            if (product.UseAutoUnitPricing)
            {
                unit.CostPrice = CalculateAutoPrice(product.CostPrice, unit.ConversionFactor, false);
                unit.SellingPrice = CalculateAutoPrice(product.SellingPrice, unit.ConversionFactor, false);
                unit.WholesalePrice = CalculateAutoPrice(product.WholesalePrice, unit.ConversionFactor, false);
            }
        }

        await _productRepository.SaveChangesAsync();
        return MapPricingDto(product);
    }

    public async Task<ProductUnitPricingDto> UpdateBasePriceAndRecalculateAsync(
        int productId, UpdateBasePriceDto dto)
    {
        var product = await _productRepository.GetByIdAsync(productId, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Product not found.");

        product.CostPrice = Math.Max(0, dto.CostPrice);
        product.SellingPrice = Math.Max(0, dto.SellingPrice);
        product.WholesalePrice = Math.Max(0, dto.WholesalePrice);

        if (dto.RecalculateNonOverriddenUnits)
            RecalculateAutoPrices(product);

        await _productRepository.SaveChangesAsync();
        return MapPricingDto(product);
    }

    private ProductUnitPricingDto MapPricingDto(ProductEntity product)
    {
        var baseUnit = product.Units.FirstOrDefault(u => u.IsBaseUnit && !u.IsDeleted)
            ?? product.Units.FirstOrDefault(u => !u.IsDeleted);

        return new ProductUnitPricingDto
        {
            ProductId = product.Id,
            ProductName = product.ProductName,
            BaseUnitId = product.BaseUnitId ?? baseUnit?.Id,
            BaseUnitName = baseUnit?.UnitName ?? string.Empty,
            BaseCostPrice = product.CostPrice,
            BaseSellingPrice = product.SellingPrice,
            BaseWholesalePrice = product.WholesalePrice,
            UseAutoUnitPricing = product.UseAutoUnitPricing,
            Units = product.Units
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.IsBaseUnit)
                .ThenBy(u => u.UnitName)
                .Select(u => new ProductUnitPricingLineDto
                {
                    ProductUnitId = u.Id,
                    UnitMasterId = u.UnitId,
                    UnitName = u.UnitName,
                    IsBaseUnit = u.IsBaseUnit,
                    ConversionFactor = u.ConversionFactor,
                    CalculatedCostPrice = CalculateAutoPrice(product.CostPrice, u.ConversionFactor, u.IsBaseUnit),
                    CalculatedSellingPrice = CalculateAutoPrice(product.SellingPrice, u.ConversionFactor, u.IsBaseUnit),
                    CalculatedWholesalePrice = CalculateAutoPrice(product.WholesalePrice, u.ConversionFactor, u.IsBaseUnit),
                    CostPrice = u.CostPrice,
                    SellingPrice = u.SellingPrice,
                    WholesalePrice = u.WholesalePrice,
                    IsPriceOverridden = u.IsPriceOverridden,
                    EffectiveSellingPrice = GetEffectiveSellingPrice(product, u, null, PricingType.Retail),
                    EffectiveWholesalePrice = GetEffectiveWholesalePrice(product, u)
                })
                .ToList()
        };
    }
}
