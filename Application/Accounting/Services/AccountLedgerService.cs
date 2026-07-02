using POSSystem.Application.Accounting.DTOs;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Accounting.Services;

public class AccountLedgerService : IAccountLedgerService
{
    private readonly IAccountLedgerRepository _repository;
    private readonly IGlAccountRepository _glAccounts;

    public AccountLedgerService(IAccountLedgerRepository repository, IGlAccountRepository glAccounts)
    {
        _repository = repository;
        _glAccounts = glAccounts;
    }

    public async Task<AccountLedgerPageDto> GetAccountLedgerAsync(AccountLedgerFilterDto filter)
    {
        if (filter.AccountId <= 0)
            throw new InvalidOperationException("AccountId is required.");

        var account = await _repository.GetAccountAsync(filter.AccountId)
            ?? throw new InvalidOperationException("Account not found.");

        var accountIds = await _glAccounts.GetDescendantAccountIdsAsync(filter.AccountId);
        var includesSubAccounts = accountIds.Count > 1;

        var page = filter.Page > 0 ? filter.Page : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 50;
        var fromDate = filter.FromDate?.Date;
        var auditView = filter.AuditView;
        var groupByChain = filter.GroupByChain && auditView;

        var cleanOpening = fromDate.HasValue
            ? await _repository.GetOpeningBalanceAsync(accountIds, fromDate.Value, filter.BranchId, auditView: false)
            : 0m;

        var viewOpening = fromDate.HasValue
            ? await _repository.GetOpeningBalanceAsync(accountIds, fromDate.Value, filter.BranchId, auditView)
            : 0m;

        var (cleanDebit, cleanCredit, cleanCount) = await _repository.GetPeriodTotalsAsync(
            accountIds, filter.FromDate, filter.ToDate, filter.BranchId, auditView: false);

        var (viewDebit, viewCredit, viewCount) = await _repository.GetPeriodTotalsAsync(
            accountIds, filter.FromDate, filter.ToDate, filter.BranchId, auditView);

        var effectiveClosing = cleanOpening + (cleanDebit - cleanCredit);
        var viewClosing = viewOpening + (viewDebit - viewCredit);

        if (Math.Abs(effectiveClosing - viewClosing) > 0.01m)
        {
            throw new InvalidOperationException(
                "Ledger validation failed: clean and audit views produce different effective closing balances.");
        }

        var lines = await _repository.GetLedgerLinesPagedAsync(
            accountIds,
            filter.FromDate,
            filter.ToDate,
            filter.BranchId,
            viewOpening,
            page,
            pageSize,
            auditView,
            groupByChain);

        var entries = lines.ToList();

        if (page == 1 && fromDate.HasValue)
        {
            entries.Insert(0, new AccountLedgerEntryDto
            {
                Id = 0,
                Date = fromDate.Value,
                Description = "Opening Balance",
                RunningBalance = viewOpening,
                IsOpeningBalance = true,
            });
        }

        var totalRecords = viewCount;
        var totalPages = TotalPages(totalRecords, pageSize);

        if (entries.Count > 0 && page == totalPages && Math.Abs(entries[^1].RunningBalance - viewClosing) > 0.01m)
        {
            throw new InvalidOperationException(
                "Ledger validation failed: period movements do not reconcile with the closing balance.");
        }

        var accountLabel = includesSubAccounts
            ? $"{account.Name} (incl. sub-accounts)"
            : account.Name;

        return new AccountLedgerPageDto
        {
            AccountId = account.Id,
            AccountName = accountLabel,
            AccountType = Enum.Parse<AccountType>(account.Type),
            OpeningBalance = viewOpening,
            ClosingBalance = viewClosing,
            EffectiveClosingBalance = effectiveClosing,
            TotalDebit = viewDebit,
            TotalCredit = viewCredit,
            PeriodNet = viewDebit - viewCredit,
            Entries = entries,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page,
            PageSize = pageSize,
            AuditView = auditView,
            IncludesSubAccounts = includesSubAccounts,
        };
    }

    public Task<decimal> GetSignedBalanceAsync(int accountId, int businessId, int? branchId, DateTime? asOfDate = null)
    {
        if (accountId <= 0)
            throw new InvalidOperationException("AccountId is required.");

        return GetSignedBalanceForAccountTreeAsync(accountId, branchId, asOfDate);
    }

    public async Task<decimal> GetDisplayBalanceAsync(int accountId, int businessId, int? branchId, DateTime? asOfDate = null)
    {
        var account = await _repository.GetAccountAsync(accountId)
            ?? throw new InvalidOperationException("Account not found.");

        var signed = await GetSignedBalanceForAccountTreeAsync(accountId, branchId, asOfDate);
        return ToDisplayBalance(Enum.Parse<AccountType>(account.Type), signed);
    }

    public Task<IReadOnlyList<GlAccountListItemDto>> ListAccountsAsync()
        => _repository.ListAccountsAsync();

    private async Task<decimal> GetSignedBalanceForAccountTreeAsync(int accountId, int? branchId, DateTime? asOfDate)
    {
        var accountIds = await _glAccounts.GetDescendantAccountIdsAsync(accountId);
        return await _repository.GetSignedBalanceAsync(accountIds, asOfDate, branchId);
    }

    internal static decimal ToDisplayBalance(AccountType accountType, decimal signedBalance)
        => accountType is AccountType.Liability or AccountType.Income
            ? -signedBalance
            : signedBalance;

    private static int TotalPages(int totalRecords, int pageSize)
        => totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);
}
