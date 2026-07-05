using POSSystem.Application.Common.DTOs;
using POSSystem.Application.StockAdjustment.DTOs;

namespace POSSystem.Application.StockAdjustment.Interfaces;

public interface IStockAdjustmentService
{
    Task<PagedResultDto<StockAdjustmentDto>> GetPagedAsync(StockAdjustmentFilterDto filter);
    Task<StockAdjustmentDetailDto?> GetByIdAsync(int id, int businessId, int branchId);
    Task<IReadOnlyList<AdjustmentTypeDto>> GetAdjustmentTypesAsync(int businessId, int branchId);
    Task<StockAdjustmentDetailDto> CreateAsync(CreateStockAdjustmentDto dto);
    Task<StockAdjustmentDetailDto> UpdateAsync(int id, UpdateStockAdjustmentDto dto);
    Task DeleteAsync(int id, int businessId, int branchId, int? deletedBy);
    Task<StockAdjustmentDetailDto> ReverseAsync(int id, int businessId, int branchId, int? reversedBy, string? reason = null);
    Task<IReadOnlyList<StockAdjustmentReportRowDto>> GetReportAsync(StockAdjustmentFilterDto filter);
}
