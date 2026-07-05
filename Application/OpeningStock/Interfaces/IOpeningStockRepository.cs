using POSSystem.Application.Common.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.OpeningStock.Interfaces;

public interface IOpeningStockRepository
{
    Task<PagedResultDto<OpeningStockVoucher>> GetPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null);

    Task<OpeningStockVoucher?> GetByIdWithLinesAsync(int id, int businessId, int branchId);
    Task<bool> VoucherNoExistsAsync(string voucherNo, int businessId, int branchId, int? excludeId = null);
    Task AddAsync(OpeningStockVoucher voucher);
    Task SaveChangesAsync();
}
