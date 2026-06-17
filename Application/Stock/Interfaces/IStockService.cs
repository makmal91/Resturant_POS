using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Stock.DTOs;

namespace POSSystem.Application.Stock.Interfaces;

public interface IStockService
{
    Task<PagedResultDto<StockLedgerDto>> GetLedgerAsync(StockLedgerFilterDto filter);
    Task<List<StockBalanceDto>> GetStockBalancesAsync(
        int businessId, int branchId, int? warehouseId = null, int? productId = null, int? variantId = null, bool variantWise = false);
    Task<decimal> GetCurrentStockAsync(int businessId, int branchId, int productId, int? variantId, int warehouseId);
    Task TransferStockAsync(StockTransferDto dto);
}
