using POSSystem.Application.Common.DTOs;
using POSSystem.Application.StockAdjustment.DTOs;
using POSSystem.Domain;
using StockAdjustmentEntity = POSSystem.Domain.StockAdjustment;

namespace POSSystem.Application.StockAdjustment.Interfaces;

public interface IStockAdjustmentRepository
{
    Task<PagedResultDto<StockAdjustmentEntity>> GetPagedAsync(StockAdjustmentFilterDto filter);
    Task<StockAdjustmentEntity?> GetByIdWithLinesAsync(int id, int businessId, int branchId);
    Task<bool> AdjustmentNoExistsAsync(string adjustmentNo, int businessId, int branchId, int? excludeId = null);
    Task AddAsync(StockAdjustmentEntity adjustment);
    Task<List<AdjustmentType>> GetActiveAdjustmentTypesAsync(int businessId, int branchId);
    Task<AdjustmentType?> GetAdjustmentTypeAsync(int id, int businessId, int branchId);
    Task SaveChangesAsync();
}
