using POSSystem.Application.Product.Interfaces;
using POSSystem.Application.Stock.Interfaces;

namespace POSSystem.Application.Stock.Services;

public class StockValidationService : IStockValidationService
{
    private readonly IStockLedgerRepository _ledgerRepository;
    private readonly IProductRepository _productRepository;

    public StockValidationService(
        IStockLedgerRepository ledgerRepository,
        IProductRepository productRepository)
    {
        _ledgerRepository = ledgerRepository;
        _productRepository = productRepository;
    }

    public async Task ValidateAvailabilityAsync(
        int businessId,
        int branchId,
        int warehouseId,
        IEnumerable<StockRequirement> requirements)
    {
        var requirementList = requirements.ToList();
        if (requirementList.Count == 0)
            return;

        var productIds = requirementList.Select(r => r.ProductId).Distinct().ToList();
        var settings = await _productRepository.GetStockSettingsByIdsAsync(businessId, branchId, productIds);

        var requiredByKey = new Dictionary<string, (decimal Qty, StockRequirement Sample)>();
        foreach (var item in requirementList)
        {
            if (settings.TryGetValue(item.ProductId, out var s) && s.AllowNegativeStock)
                continue;

            var key = $"{item.ProductId}:{item.VariantId ?? 0}";
            if (requiredByKey.TryGetValue(key, out var existing))
                requiredByKey[key] = (existing.Qty + item.BaseQuantity, existing.Sample);
            else
                requiredByKey[key] = (item.BaseQuantity, item);
        }

        if (requiredByKey.Count == 0)
            return;

        var insufficient = new List<string>();
        foreach (var (key, (requiredQty, sample)) in requiredByKey)
        {
            var parts = key.Split(':');
            var productId = int.Parse(parts[0]);
            var variantId = int.Parse(parts[1]);
            int? variant = variantId == 0 ? null : variantId;

            var available = await _ledgerRepository.GetCurrentStockAsync(
                businessId, branchId, productId, variant, warehouseId);

            if (requiredQty > available)
            {
                var label = sample.VariantName != null
                    ? $"{sample.ProductName} ({sample.VariantName})"
                    : sample.ProductName;
                insufficient.Add(
                    $"{label}: required {requiredQty:N2} {sample.BaseUnitName}, available {available:N2} {sample.BaseUnitName}");
            }
        }

        if (insufficient.Count == 0)
            return;

        var message = insufficient.Count == 1
            ? $"Insufficient stock — {insufficient[0]}."
            : "Insufficient stock:\n" + string.Join("\n", insufficient.Select((line, i) => $"{i + 1}. {line}"));

        throw new InvalidOperationException(message);
    }

    public Task ExecuteWithStockLockAsync(Func<Task> action)
        => _ledgerRepository.RunInSerializableTransactionAsync(action);
}
