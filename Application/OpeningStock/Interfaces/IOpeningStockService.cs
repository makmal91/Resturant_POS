using POSSystem.Application.Common.DTOs;
using POSSystem.Application.OpeningStock.DTOs;

namespace POSSystem.Application.OpeningStock.Interfaces;

public interface IOpeningStockService
{
    Task<PagedResultDto<OpeningStockVoucherDto>> GetPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null);

    Task<OpeningStockVoucherDetailDto?> GetByIdAsync(int id, int businessId, int branchId);
    Task<OpeningStockVoucherDetailDto> CreateAsync(CreateOpeningStockVoucherDto dto);
    Task<OpeningStockVoucherDetailDto> UpdateAsync(int id, UpdateOpeningStockVoucherDto dto);
    Task<OpeningStockVoucherDetailDto> ReverseAsync(int id, ReverseOpeningStockVoucherDto dto);
}
