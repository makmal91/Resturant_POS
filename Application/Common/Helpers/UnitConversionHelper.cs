namespace POSSystem.Application.Common.Helpers;

/// <summary>
/// Unit conversion for stock and display.
/// The BASE unit is always the SMALLEST sellable unit and stock is stored in it.
/// ConversionFactor = number of BASE units contained in 1 of this unit (base unit = 1).
/// Example: base = PCS, 1 Package = 3 PCS → Package factor = 3;
/// selling/buying 1 Package moves 3 base units (PCS).
/// </summary>
public static class UnitConversionHelper
{
    /// <summary>Converts an entered quantity (in the given unit) to base-unit stock quantity.
    /// baseQty = quantity × factor (1 Package × 3 = 3 base PCS).</summary>
    public static decimal ToBaseQuantity(decimal quantity, decimal conversionFactor)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero.");

        if (conversionFactor <= 0)
            throw new InvalidOperationException("ConversionFactor must be greater than zero.");

        return quantity * conversionFactor;
    }

    /// <summary>Converts base-unit stock to the equivalent quantity in another unit.
    /// unitQty = baseQuantity ÷ factor (3 base PCS ÷ 3 = 1 Package).</summary>
    public static decimal FromBaseQuantity(decimal baseQuantity, decimal conversionFactor)
    {
        if (conversionFactor <= 0)
            throw new InvalidOperationException("ConversionFactor must be greater than zero.");

        return baseQuantity / conversionFactor;
    }

    /// <summary>
    /// Expresses base-unit stock in every product unit (qty = baseStock ÷ factor; base factor = 1).
    /// Example: 100 PCS (base), Package factor 3 → PCS 100, Package 33.33.
    /// </summary>
    public static List<StockUnitDisplay> ConvertStockToAllUnits(
        decimal baseStock,
        IEnumerable<StockUnitDisplayInput> units)
    {
        var unitList = units
            .Where(u => u.IsBaseUnit || u.ConversionFactor > 0)
            .ToList();

        if (unitList.Count == 0)
            return [];

        return unitList
            .OrderByDescending(u => u.IsBaseUnit)
            .ThenBy(u => u.UnitName)
            .Select(u =>
            {
                var factor = u.IsBaseUnit ? 1m : u.ConversionFactor;
                return new StockUnitDisplay
                {
                    UnitId = u.UnitId,
                    UnitName = u.UnitName,
                    Quantity = u.IsBaseUnit ? baseStock : baseStock / factor,
                    ConversionFactor = factor,
                    IsBaseUnit = u.IsBaseUnit,
                    IsRemainder = false
                };
            })
            .ToList();
    }

    /// <summary>
    /// Resolves effective conversion factor: product override first, then unit master default.
    /// Base unit is always 1.
    /// </summary>
    public static decimal ResolveConversionFactor(
        bool isBaseUnit,
        decimal productConversionFactor,
        decimal masterDefaultConversionFactor,
        string unitName)
    {
        if (isBaseUnit)
            return 1m;

        if (productConversionFactor > 0)
            return productConversionFactor;

        if (masterDefaultConversionFactor > 0)
            return masterDefaultConversionFactor;

        throw new InvalidOperationException(
            $"ConversionFactor is required for unit '{unitName}'. Set it on the product or define DefaultConversionFactor in Unit Master.");
    }

    public static void ValidateConversionFactor(bool isBaseUnit, decimal conversionFactor, string unitName)
    {
        if (conversionFactor <= 0)
            throw new InvalidOperationException($"ConversionFactor must be greater than zero for unit '{unitName}'.");

        if (isBaseUnit && conversionFactor != 1m)
            throw new InvalidOperationException(
                $"Base unit '{unitName}' must have ConversionFactor = 1.");
    }

    /// <summary>
    /// Breaks base-unit stock into whole units of the largest pack plus a base-unit remainder.
    /// Example: 7 PCS (base), Package factor 3 → 2 Package + 1 PCS.
    /// </summary>
    public static List<StockUnitDisplay> BreakDownStock(
        decimal stockInBaseUnit,
        IEnumerable<StockUnitDisplayInput> units)
    {
        var unitList = units
            .Where(u => u.IsBaseUnit || u.ConversionFactor > 0)
            .ToList();

        var baseUnit = unitList.FirstOrDefault(u => u.IsBaseUnit) ?? unitList.FirstOrDefault();
        if (baseUnit == null)
            return [];

        // Largest pack = highest number of base units per unit.
        var largestPack = unitList
            .Where(u => !u.IsBaseUnit && u.ConversionFactor > 1)
            .OrderByDescending(u => u.ConversionFactor)
            .FirstOrDefault();

        var result = new List<StockUnitDisplay>();

        if (largestPack != null)
        {
            var wholePacks = Math.Floor(stockInBaseUnit / largestPack.ConversionFactor);
            var remainderBase = stockInBaseUnit - wholePacks * largestPack.ConversionFactor;

            if (wholePacks > 0)
            {
                result.Add(new StockUnitDisplay
                {
                    UnitId = largestPack.UnitId,
                    UnitName = largestPack.UnitName,
                    Quantity = wholePacks,
                    ConversionFactor = largestPack.ConversionFactor,
                    IsBaseUnit = false,
                    IsRemainder = false
                });
            }

            if (remainderBase > 0 || wholePacks == 0)
            {
                result.Add(new StockUnitDisplay
                {
                    UnitId = baseUnit.UnitId,
                    UnitName = baseUnit.UnitName,
                    Quantity = remainderBase,
                    ConversionFactor = 1m,
                    IsBaseUnit = true,
                    IsRemainder = wholePacks > 0
                });
            }
        }
        else
        {
            result.Add(new StockUnitDisplay
            {
                UnitId = baseUnit.UnitId,
                UnitName = baseUnit.UnitName,
                Quantity = stockInBaseUnit,
                ConversionFactor = 1m,
                IsBaseUnit = true,
                IsRemainder = false
            });
        }

        return result;
    }
}

public sealed class StockUnitDisplayInput
{
    public int UnitId { get; init; }
    public string UnitName { get; init; } = string.Empty;
    public decimal ConversionFactor { get; init; }
    public bool IsBaseUnit { get; init; }
}

public sealed class StockUnitDisplay
{
    public int UnitId { get; init; }
    public string UnitName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal ConversionFactor { get; init; }
    public bool IsBaseUnit { get; init; }
    public bool IsRemainder { get; init; }
}
