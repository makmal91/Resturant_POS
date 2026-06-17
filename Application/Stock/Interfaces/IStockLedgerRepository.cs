using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Stock.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Stock.Interfaces;

public interface IStockLedgerRepository
{
    Task AddAsync(StockLedger entry);
    Task AddRangeAsync(IEnumerable<StockLedger> entries);
    Task<PagedResultDto<StockLedger>> GetPagedAsync(StockLedgerFilterDto filter);
    Task<List<StockBalanceDto>> GetStockBalancesAsync(
        int businessId, int branchId, int? warehouseId = null, int? productId = null, int? variantId = null, bool variantWise = false);
    Task<decimal> GetCurrentStockAsync(int businessId, int branchId, int productId, int? variantId, int warehouseId);
    /// <summary>
    /// Returns total stock keyed as "productId:variantId" (variantId = 0 for no variant).
    /// Sums across all warehouses unless warehouseId is provided.
    /// </summary>
    Task<Dictionary<string, decimal>> GetStockForProductsAsync(
        int businessId, int branchId, IEnumerable<int> productIds, int? warehouseId);
    Task<List<StockLedger>> GetByReferenceAsync(
        int referenceId, int businessId, int branchId, params StockLedgerType[] types);
    Task<bool> HasOpeningEntryAsync(int productId, int businessId, int branchId);
    Task<List<StockLedger>> GetOpeningEntriesAsync(int productId, int businessId, int branchId);
    Task<Dictionary<int, (bool AllowNegativeStock, bool EnableLowStockAlert, decimal? LowStockAlertLevel)>> GetProductStockSettingsAsync(
        int businessId, int branchId, IEnumerable<int> productIds);
    Task SaveChangesAsync();
}
