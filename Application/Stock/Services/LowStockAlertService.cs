using POSSystem.Application.Stock.DTOs;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Stock.Services;

public class LowStockAlertService : ILowStockAlertService
{
    private readonly ILowStockAlertRepository _alertRepository;
    private readonly IStockLedgerRepository _ledgerRepository;

    public LowStockAlertService(
        ILowStockAlertRepository alertRepository,
        IStockLedgerRepository ledgerRepository)
    {
        _alertRepository = alertRepository;
        _ledgerRepository = ledgerRepository;
    }

    public async Task EvaluateAfterStockChangeAsync(
        int businessId,
        int branchId,
        IEnumerable<StockChangeItem> items)
    {
        var affected = items
            .Where(i => i.ProductId > 0 && i.WarehouseId > 0)
            .DistinctBy(i => (i.ProductId, i.VariantId, i.WarehouseId))
            .ToList();

        if (affected.Count == 0)
            return;

        var productIds = affected.Select(i => i.ProductId).Distinct().ToList();
        var products = await _alertRepository.GetProductAlertSettingsAsync(businessId, branchId, productIds);
        var existingAlerts = await _alertRepository.GetAlertsForProductsAsync(businessId, branchId, productIds);
        var now = DateTime.UtcNow;

        foreach (var item in affected)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                continue;

            var currentStock = await _ledgerRepository.GetCurrentStockAsync(
                businessId, branchId, item.ProductId, item.VariantId, item.WarehouseId);

            var alert = existingAlerts.FirstOrDefault(a =>
                a.ProductId == item.ProductId
                && a.VariantId == item.VariantId
                && a.WarehouseId == item.WarehouseId);

            var shouldAlert = product.EnableLowStockAlert
                              && product.LowStockAlertLevel.HasValue
                              && currentStock <= product.LowStockAlertLevel.Value;

            if (shouldAlert)
            {
                if (alert == null)
                {
                    alert = new LowStockAlert
                    {
                        BusinessId = businessId,
                        BranchId = branchId,
                        ProductId = item.ProductId,
                        VariantId = item.VariantId,
                        WarehouseId = item.WarehouseId,
                        IsActive = true
                    };
                    await _alertRepository.AddAsync(alert);
                    existingAlerts.Add(alert);
                }

                alert.CurrentStock = currentStock;
                alert.AlertLevel = product.LowStockAlertLevel!.Value;
                alert.IsActive = true;
                alert.LastTriggeredAt = now;
            }
            else if (alert != null)
            {
                alert.IsActive = false;
                alert.CurrentStock = currentStock;
            }
        }

        await _alertRepository.SaveChangesAsync();
    }

    public async Task<List<LowStockAlertDto>> GetActiveAlertsAsync(
        int businessId, int branchId, int? warehouseId = null)
    {
        var alerts = await _alertRepository.GetActiveAlertsAsync(businessId, branchId, warehouseId);
        return alerts.Select(a => new LowStockAlertDto
        {
            Id = a.Id,
            ProductId = a.ProductId,
            ProductName = a.Product?.ProductName ?? string.Empty,
            ProductCode = a.Product?.ProductCode ?? string.Empty,
            VariantId = a.VariantId,
            VariantName = a.Variant?.VariantName,
            WarehouseId = a.WarehouseId,
            WarehouseName = a.Warehouse?.Name ?? string.Empty,
            CurrentStock = a.CurrentStock,
            AlertLevel = a.AlertLevel,
            LastTriggeredAt = a.LastTriggeredAt
        }).ToList();
    }
}
