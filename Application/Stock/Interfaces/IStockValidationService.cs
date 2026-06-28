namespace POSSystem.Application.Stock.Interfaces;

public sealed class StockRequirement
{
    public int ProductId { get; init; }
    public int? VariantId { get; init; }
    public decimal BaseQuantity { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public string BaseUnitName { get; init; } = "base unit";
}

public interface IStockValidationService
{
    Task ValidateAvailabilityAsync(
        int businessId,
        int branchId,
        int warehouseId,
        IEnumerable<StockRequirement> requirements);

    Task ExecuteWithStockLockAsync(Func<Task> action);
}
