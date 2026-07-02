using POSSystem.Application.CashFlow.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.CashFlow.Interfaces;

public interface ICashFlowService
{
    Task<CashRegisterDto> OpenCashAsync(OpeningCashDto dto);
    Task<CashRegisterDto> CloseCashAsync(ClosingCashDto dto);
    Task<CashRegisterDto?> GetTodayRegisterAsync(int businessId, int branchId);
    Task<CashFlowTransactionDto> RecordTransactionAsync(RecordCashTransactionDto dto);
    Task<JournalVoucherListPageDto> ListJournalVouchersAsync(JournalVoucherListFilterDto filter);
    Task<CashFlowLedgerPageDto> GetLedgerAsync(CashFlowLedgerFilterDto filter);
    Task<DailyCashSummaryDto> GetDailySummaryAsync(int businessId, int branchId, DateTime? date = null);
    Task<MonthlyCashSummaryDto> GetMonthlySummaryAsync(int businessId, int branchId, int? year = null, int? month = null);
    Task<List<BranchCashSummaryDto>> GetAllBranchesSummaryAsync(int businessId, DateTime? date = null);
}
