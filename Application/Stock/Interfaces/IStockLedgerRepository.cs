using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Stock.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Stock.Interfaces;

public interface IStockLedgerRepository
{
    Task AddAsync(StockLedger entry);
    Task AddRangeAsync(IEnumerable<StockLedger> entries);
    Task<PagedResultDto<StockLedger>> GetPagedAsync(StockLedgerFilterDto filter);
    Task<List<StockBalanceDto>> GetStockBalancesAsync(int businessId, int branchId, int? warehouseId = null, int? productId = null, int? variantId = null);
    Task<decimal> GetCurrentStockAsync(int businessId, int branchId, int productId, int? variantId, int warehouseId);
    Task SaveChangesAsync();
}
