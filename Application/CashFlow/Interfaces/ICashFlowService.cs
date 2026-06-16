using POSSystem.Application.CashFlow.DTOs;
using POSSystem.Application.Common.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.CashFlow.Interfaces;

public interface ICashFlowService
{
    // ─── Daily register ────────────────────────────────────────────────────────
    Task<CashRegisterDto> OpenCashAsync(OpeningCashDto dto);
    Task<CashRegisterDto> CloseCashAsync(ClosingCashDto dto);
    Task<CashRegisterDto?> GetTodayRegisterAsync(int businessId, int branchId);

    // ─── Manual transactions ───────────────────────────────────────────────────
    Task<CashFlowTransactionDto> RecordTransactionAsync(RecordCashTransactionDto dto);
    Task<PagedResultDto<CashFlowTransactionDto>> GetLedgerAsync(CashFlowLedgerFilterDto filter);

    // ─── Summaries ─────────────────────────────────────────────────────────────
    Task<DailyCashSummaryDto> GetDailySummaryAsync(int businessId, int branchId, DateTime? date = null);
    Task<MonthlyCashSummaryDto> GetMonthlySummaryAsync(int businessId, int branchId, int? year = null, int? month = null);
    Task<List<BranchCashSummaryDto>> GetAllBranchesSummaryAsync(int businessId, DateTime? date = null);

    // ─── Integration (called by Sales/Expense services) ────────────────────────
    Task RecordSaleAsync(int businessId, int branchId, int saleId, string invoiceNo, decimal cashAmount, decimal cardAmount);
    Task RecordExpenseAsync(int businessId, int branchId, int expenseId, string description, decimal amount, CashFlowPaymentMethod paymentMethod);
}
