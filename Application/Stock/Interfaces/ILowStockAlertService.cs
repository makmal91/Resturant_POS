using POSSystem.Application.Stock.DTOs;

namespace POSSystem.Application.Stock.Interfaces;

public interface ILowStockAlertService
{
    Task EvaluateAfterStockChangeAsync(
        int businessId,
        int branchId,
        IEnumerable<StockChangeItem> items);

    Task<List<LowStockAlertDto>> GetActiveAlertsAsync(int businessId, int branchId, int? warehouseId = null);
}
