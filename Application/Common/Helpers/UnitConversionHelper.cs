namespace POSSystem.Application.Common.Helpers;

/// <summary>
/// Unit conversion for stock and display.
/// ConversionFactor = number of smaller (child) units in 1 base unit.
/// Example: 1 Pipe = 20 Feet → Feet factor = 20; 20 Feet sold → 1 base unit deducted.
/// </summary>
public static class UnitConversionHelper
{
    /// <summary>Converts entered quantity to base-unit stock quantity.</summary>
    public static decimal ToBaseQuantity(decimal quantity, decimal conversionFactor)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero.");

        if (conversionFactor <= 0)
            throw new InvalidOperationException("ConversionFactor must be greater than zero.");

        return quantity / conversionFactor;
    }

    /// <summary>Converts base-unit stock to equivalent quantity in another unit.</summary>
    public static decimal FromBaseQuantity(decimal baseQuantity, decimal conversionFactor)
    {
        if (conversionFactor <= 0)
            throw new InvalidOperationException("ConversionFactor must be greater than zero.");

        return baseQuantity * conversionFactor;
    }

    /// <summary>
    /// Expresses base-unit stock in every product unit (qty = baseStock × factor; base factor = 1).
    /// Example: 100 Pipe, Feet factor 20 → Pipe 100, Feet 2000.
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
                    Quantity = u.IsBaseUnit ? baseStock : baseStock * factor,
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
    /// Breaks stock (in base units) into whole base units plus remainder in the primary child unit.
    /// Example: 2.5 Pipe (base), Feet factor 20 → 2 Pipe + 10 Feet.
    /// </summary>
    public static List<StockUnitDisplay> BreakDownStock(
        decimal stockInBaseUnit,
        IEnumerable<StockUnitDisplayInput> units)
    {
        var unitList = units
            .Where(u => u.ConversionFactor > 0)
            .ToList();

        var baseUnit = unitList.FirstOrDefault(u => u.IsBaseUnit) ?? unitList.FirstOrDefault();
        if (baseUnit == null)
            return [];

        var result = new List<StockUnitDisplay>();
        var wholeBase = Math.Floor(stockInBaseUnit);
        var fractionalBase = stockInBaseUnit - wholeBase;

        if (wholeBase > 0)
        {
            result.Add(new StockUnitDisplay
            {
                UnitId = baseUnit.UnitId,
                UnitName = baseUnit.UnitName,
                Quantity = wholeBase,
                ConversionFactor = 1m,
                IsBaseUnit = true,
                IsRemainder = false
            });
        }

        var primaryChild = unitList
            .Where(u => !u.IsBaseUnit)
            .OrderByDescending(u => u.ConversionFactor)
            .FirstOrDefault();

        if (primaryChild != null && fractionalBase > 0)
        {
            var childQty = fractionalBase * primaryChild.ConversionFactor;
            if (childQty > 0)
            {
                result.Add(new StockUnitDisplay
                {
                    UnitId = primaryChild.UnitId,
                    UnitName = primaryChild.UnitName,
                    Quantity = childQty,
                    ConversionFactor = primaryChild.ConversionFactor,
                    IsBaseUnit = false,
                    IsRemainder = true
                });
            }
        }
        else if (wholeBase == 0 && stockInBaseUnit > 0)
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
