using POSSystem.Domain;

namespace POSSystem.Application.Stock.Interfaces;

public interface ILowStockAlertRepository
{
    Task<Dictionary<int, (bool EnableLowStockAlert, decimal? LowStockAlertLevel)>> GetProductAlertSettingsAsync(
        int businessId, int branchId, IEnumerable<int> productIds);

    Task<List<LowStockAlert>> GetAlertsForProductsAsync(
        int businessId, int branchId, IEnumerable<int> productIds);

    Task AddAsync(LowStockAlert alert);
    Task<List<LowStockAlert>> GetActiveAlertsAsync(int businessId, int branchId, int? warehouseId = null);
    Task SaveChangesAsync();
}
