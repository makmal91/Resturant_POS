using POSSystem.Application.CashFlow.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.CashFlow.Interfaces;

public interface ICashFlowRepository
{
    Task<CashRegister?> GetRegisterAsync(int businessId, int branchId, DateTime date);
    Task<CashRegister> AddRegisterAsync(CashRegister register);
    Task UpdateRegisterAsync(CashRegister register);
    Task<DailyCashSummaryDto> GetDailySummaryAsync(int businessId, int branchId, DateTime date);
    Task<MonthlyCashSummaryDto> GetMonthlySummaryAsync(int businessId, int branchId, int year, int month);
    Task<List<BranchCashSummaryDto>> GetBranchSummariesAsync(int businessId, DateTime date);

    Task<JournalVoucher> AddJournalVoucherAsync(JournalVoucher voucher);
    Task SaveChangesAsync();
    Task<(IReadOnlyList<JournalVoucher> Items, int Total)> ListJournalVouchersAsync(
        int businessId,
        int branchId,
        DateTime? fromDate,
        DateTime? toDate,
        CashFlowTransactionType? transactionType,
        int page,
        int pageSize);
}
