using POSSystem.Application.CashFlow.DTOs;
using POSSystem.Application.Common.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.CashFlow.Interfaces;

public interface ICashFlowRepository
{
    // ─── Transactions ──────────────────────────────────────────────────────────
    Task<CashFlowTransaction> AddTransactionAsync(CashFlowTransaction transaction);
    Task<PagedResultDto<CashFlowTransactionDto>> GetLedgerPagedAsync(CashFlowLedgerFilterDto filter);

    // ─── Cash Register ─────────────────────────────────────────────────────────
    Task<CashRegister?> GetRegisterAsync(int businessId, int branchId, DateTime date);
    Task<CashRegister> AddRegisterAsync(CashRegister register);
    Task UpdateRegisterAsync(CashRegister register);

    // ─── Summaries ─────────────────────────────────────────────────────────────
    Task<DailyCashSummaryDto> GetDailySummaryAsync(int businessId, int branchId, DateTime date);
    Task<MonthlyCashSummaryDto> GetMonthlySummaryAsync(int businessId, int branchId, int year, int month);
    Task<List<BranchCashSummaryDto>> GetBranchSummariesAsync(int businessId, DateTime date);

    // ─── Integration helpers ───────────────────────────────────────────────────
    Task<decimal> GetOpeningCashAsync(int businessId, int branchId, DateTime date);
    Task<decimal> GetTotalByTypeAsync(int businessId, int branchId, DateTime date, CashFlowTransactionType type, CashFlowPaymentMethod? paymentMethod = null);
}
