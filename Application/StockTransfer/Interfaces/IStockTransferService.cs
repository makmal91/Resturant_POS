using POSSystem.Application.Common.DTOs;
using POSSystem.Application.StockTransfer.DTOs;

namespace POSSystem.Application.StockTransfer.Interfaces;

public interface IStockTransferService
{
    Task<PagedResultDto<StockTransferVoucherDto>> GetPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null);

    Task<StockTransferVoucherDetailDto?> GetByIdAsync(int id, int businessId, int branchId);
    Task<StockTransferVoucherDetailDto> CreateAsync(CreateStockTransferVoucherDto dto);
    Task<StockTransferVoucherDetailDto> UpdateAsync(int id, UpdateStockTransferVoucherDto dto);
    Task<StockTransferVoucherDetailDto> ReverseAsync(int id, ReverseStockTransferVoucherDto dto);
}
