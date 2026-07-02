using POSSystem.Application.Accounting.DTOs;

namespace POSSystem.Application.Accounting.Interfaces;

public interface ITrialBalanceRepository
{
    Task<IReadOnlyList<GlAccountListItemDto>> GetActiveAccountsAsync();
    Task<IReadOnlyList<AccountPeriodTotalsRow>> GetAccountPeriodTotalsAsync(
        int? branchId, DateTime? fromDate, DateTime? toDate);
}
