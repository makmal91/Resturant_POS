using POSSystem.Application.Common.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.StockTransfer.Interfaces;

public interface IStockTransferRepository
{
    Task<PagedResultDto<StockTransferVoucher>> GetPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null);

    Task<StockTransferVoucher?> GetByIdWithLinesAsync(int id, int businessId, int branchId);
    Task<bool> TransferNoExistsAsync(string transferNo, int businessId, int branchId, int? excludeId = null);
    Task AddAsync(StockTransferVoucher voucher);
    Task SaveChangesAsync();
}
