using POSSystem.Domain;

namespace POSSystem.Application.Product.DTOs;

/// <summary>Full unit pricing view for a product (maps to Product + ProductUnits).</summary>
public class ProductUnitPricingDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? BaseUnitId { get; set; }
    public string BaseUnitName { get; set; } = string.Empty;
    public decimal BaseCostPrice { get; set; }
    public decimal BaseSellingPrice { get; set; }
    public decimal BaseWholesalePrice { get; set; }
    public bool UseAutoUnitPricing { get; set; } = true;
    public List<ProductUnitPricingLineDto> Units { get; set; } = new();
}

public class ProductUnitPricingLineDto
{
    public int ProductUnitId { get; set; }
    public int? UnitMasterId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public bool IsBaseUnit { get; set; }
    public decimal ConversionFactor { get; set; }
    public decimal CalculatedSellingPrice { get; set; }
    public decimal CalculatedWholesalePrice { get; set; }
    public decimal CalculatedCostPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? WholesalePrice { get; set; }
    public decimal? CostPrice { get; set; }
    public bool IsPriceOverridden { get; set; }
    public decimal EffectiveSellingPrice { get; set; }
    public decimal EffectiveWholesalePrice { get; set; }
}

public class CalculateUnitPriceRequestDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public int ProductUnitId { get; set; }
    public PricingType PricingType { get; set; } = PricingType.Retail;
    public decimal? BaseSellingPrice { get; set; }
    public decimal? BaseWholesalePrice { get; set; }
    public decimal? BaseCostPrice { get; set; }
}

public class CalculateUnitPriceResponseDto
{
    public int ProductUnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; }
    public decimal CalculatedPrice { get; set; }
    public decimal? OverridePrice { get; set; }
    public bool IsPriceOverridden { get; set; }
    public decimal EffectivePrice { get; set; }
    public bool UseAutoUnitPricing { get; set; }
}

public class SaveUnitPriceOverrideDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public decimal CustomSellingPrice { get; set; }
    public decimal? CustomWholesalePrice { get; set; }
    public decimal? CustomCostPrice { get; set; }
    public bool IsOverride { get; set; } = true;
}

public class UpdateBasePriceDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public bool RecalculateNonOverriddenUnits { get; set; } = true;
}
