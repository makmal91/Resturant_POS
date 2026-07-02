using POSSystem.Application.Accounting.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Accounting.Interfaces;

public interface IAccountLedgerService
{
    Task<AccountLedgerPageDto> GetAccountLedgerAsync(AccountLedgerFilterDto filter);
    Task<decimal> GetSignedBalanceAsync(int accountId, int businessId, int? branchId, DateTime? asOfDate = null);
    Task<decimal> GetDisplayBalanceAsync(int accountId, int businessId, int? branchId, DateTime? asOfDate = null);
    Task<IReadOnlyList<GlAccountListItemDto>> ListAccountsAsync();
}
