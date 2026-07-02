using POSSystem.Application.Accounting.DTOs;

namespace POSSystem.Application.Accounting.Interfaces;

public interface IAccountLedgerRepository
{
    Task<GlAccountListItemDto?> GetAccountAsync(int accountId);
    Task<IReadOnlyList<GlAccountListItemDto>> ListAccountsAsync();
    Task<decimal> GetOpeningBalanceAsync(IReadOnlyList<int> accountIds, DateTime fromDate, int? branchId, bool auditView);
    Task<(decimal TotalDebit, decimal TotalCredit, int TotalRecords)> GetPeriodTotalsAsync(
        IReadOnlyList<int> accountIds, DateTime? fromDate, DateTime? toDate, int? branchId, bool auditView);
    Task<IReadOnlyList<AccountLedgerEntryDto>> GetLedgerLinesPagedAsync(
        IReadOnlyList<int> accountIds, DateTime? fromDate, DateTime? toDate, int? branchId,
        decimal openingBalance, int page, int pageSize, bool auditView, bool groupByChain);
    Task<decimal> GetSignedBalanceAsync(IReadOnlyList<int> accountIds, DateTime? asOfDate, int? branchId);
}
