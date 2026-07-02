using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class GlReportingRepository : IGlReportingRepository
{
    private readonly POSDbContext _db;
    private readonly IGlAccountRepository _glAccounts;

    public GlReportingRepository(POSDbContext db, IGlAccountRepository glAccounts)
    {
        _db = db;
        _glAccounts = glAccounts;
    }

    public async Task<decimal> GetAccountBalanceAsync(int accountId, int? branchId, DateTime? asOfDate = null)
    {
        var query = FilterByBranch(ActiveLines().Where(t => t.AccountId == accountId), branchId);
        if (asOfDate.HasValue)
            query = query.Where(t => t.Date < asOfDate.Value.Date.AddDays(1));

        return await query.SumAsync(t => t.DebitAmount - t.CreditAmount);
    }

    public async Task<IReadOnlyDictionary<int, decimal>> GetAccountBalancesAsync(
        IEnumerable<int> accountIds, bool creditNormal, int? branchId, DateTime? asOfDate = null)
    {
        var ids = accountIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, decimal>();

        var query = FilterByBranch(ActiveLines().Where(t => ids.Contains(t.AccountId)), branchId);
        if (asOfDate.HasValue)
            query = query.Where(t => t.Date < asOfDate.Value.Date.AddDays(1));

        var rows = await query
            .GroupBy(t => t.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Balance = creditNormal
                    ? g.Sum(t => t.CreditAmount - t.DebitAmount)
                    : g.Sum(t => t.DebitAmount - t.CreditAmount),
            })
            .ToListAsync();

        return rows.ToDictionary(r => r.AccountId, r => r.Balance);
    }

    public async Task<decimal> GetSubtreeBalanceAsync(
        int? branchId, string rootAccountName, bool creditNormal, DateTime? asOfDate = null)
    {
        var rootId = await _glAccounts.GetAccountIdByNameAsync(rootAccountName);
        if (!rootId.HasValue)
            return 0;

        var descendantIds = await _glAccounts.GetDescendantAccountIdsAsync(rootId.Value);
        var balances = await GetAccountBalancesAsync(descendantIds, creditNormal, branchId, asOfDate);
        return balances.Values.Sum();
    }

    public async Task<decimal> GetAccountTypeNetAsync(
        int? branchId, AccountType accountType, DateTime? from, DateTime? to)
    {
        var query = FilterByBranch(
            ActiveLines().Where(t => t.Account.Type == accountType && !t.Account.IsDeleted),
            branchId);

        if (from.HasValue)
            query = query.Where(t => t.Date >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(t => t.Date < to.Value.Date.AddDays(1));

        var debitMinusCredit = await query.SumAsync(t => t.DebitAmount - t.CreditAmount);
        return accountType switch
        {
            AccountType.Liability or AccountType.Income or AccountType.Equity => -debitMinusCredit,
            _ => debitMinusCredit,
        };
    }

    public async Task<decimal> GetSubtreePeriodNetAsync(
        int? branchId, string rootAccountName, bool creditNormal, DateTime? from, DateTime? to)
    {
        var rootId = await _glAccounts.GetAccountIdByNameAsync(rootAccountName);
        if (!rootId.HasValue)
            return 0;

        var descendantIds = await _glAccounts.GetDescendantAccountIdsAsync(rootId.Value);
        var query = FilterByBranch(
            ActiveLines().Where(t => descendantIds.Contains(t.AccountId)),
            branchId);

        if (from.HasValue)
            query = query.Where(t => t.Date >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(t => t.Date < to.Value.Date.AddDays(1));

        var debitMinusCredit = await query.SumAsync(t => t.DebitAmount - t.CreditAmount);
        return creditNormal ? -debitMinusCredit : debitMinusCredit;
    }

    public async Task<decimal> GetTransactionTypeDebitTotalAsync(
        int? branchId, GlTransactionType transactionType, DateTime? from, DateTime? to)
    {
        var query = FilterByBranch(
            ActiveLines().Where(t => t.TransactionType == transactionType),
            branchId);

        if (from.HasValue)
            query = query.Where(t => t.Date >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(t => t.Date < to.Value.Date.AddDays(1));

        return await query.SumAsync(t => t.DebitAmount);
    }

    public async Task<IReadOnlyDictionary<int, decimal>> GetSubtreePeriodNetByBranchAsync(
        string rootAccountName, bool creditNormal, DateTime from, DateTime to)
    {
        var rootId = await _glAccounts.GetAccountIdByNameAsync(rootAccountName);
        if (!rootId.HasValue)
            return new Dictionary<int, decimal>();

        var descendantIds = await _glAccounts.GetDescendantAccountIdsAsync(rootId.Value);
        var rows = await ActiveLines()
            .Where(t => descendantIds.Contains(t.AccountId))
            .Where(t => t.Date >= from.Date && t.Date < to.Date.AddDays(1))
            .GroupBy(t => t.BranchId)
            .Select(g => new
            {
                BranchId = g.Key,
                Net = g.Sum(t => t.DebitAmount - t.CreditAmount),
            })
            .ToListAsync();

        return rows.ToDictionary(
            r => r.BranchId,
            r => creditNormal ? -r.Net : r.Net);
    }

    public async Task<IReadOnlyDictionary<int, decimal>> GetAccountTypeNetByBranchAsync(
        AccountType accountType, DateTime from, DateTime to)
    {
        var rows = await ActiveLines()
            .Where(t => t.Account.Type == accountType && !t.Account.IsDeleted)
            .Where(t => t.Date >= from.Date && t.Date < to.Date.AddDays(1))
            .GroupBy(t => t.BranchId)
            .Select(g => new
            {
                BranchId = g.Key,
                Net = g.Sum(t => t.DebitAmount - t.CreditAmount),
            })
            .ToListAsync();

        return rows.ToDictionary(
            r => r.BranchId,
            r => accountType is AccountType.Liability or AccountType.Income or AccountType.Equity
                ? -r.Net
                : r.Net);
    }

    public async Task<decimal> GetPartySubAccountsNetAsync(
        int businessId, int? branchId, AccountType partyType, DateTime? asOfDate = null)
    {
        List<int> accountIds = partyType == AccountType.Asset
            ? await _db.Customers.AsNoTracking()
                .Where(c => c.BusinessId == businessId && !c.IsDeleted && c.AccountId != null)
                .Where(c => branchId == null || branchId <= 0 || c.BranchId == branchId)
                .Select(c => c.AccountId!.Value)
                .ToListAsync()
            : await _db.Suppliers.AsNoTracking()
                .Where(s => s.BusinessId == businessId && !s.IsDeleted && s.AccountId != null)
                .Where(s => branchId == null || branchId <= 0 || s.BranchId == branchId)
                .Select(s => s.AccountId!.Value)
                .ToListAsync();

        if (accountIds.Count == 0)
            return 0;

        var creditNormal = partyType == AccountType.Liability;
        var balances = await GetAccountBalancesAsync(accountIds, creditNormal, branchId, asOfDate);
        return balances.Values.Sum();
    }

    public async Task<IReadOnlyDictionary<int, DateTime>> GetAccountLastActivityDatesAsync(
        IEnumerable<int> accountIds, int? branchId, DateTime? asOfDate = null)
    {
        var ids = accountIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, DateTime>();

        var query = FilterByBranch(ActiveLines().Where(t => ids.Contains(t.AccountId)), branchId);
        if (asOfDate.HasValue)
            query = query.Where(t => t.Date < asOfDate.Value.Date.AddDays(1));

        var rows = await query
            .GroupBy(t => t.AccountId)
            .Select(g => new { AccountId = g.Key, LastDate = g.Max(t => t.Date) })
            .ToListAsync();

        return rows.ToDictionary(r => r.AccountId, r => r.LastDate);
    }

    public async Task<IReadOnlyDictionary<int, decimal>> GetPartyDocumentChargesAsync(
        IEnumerable<int> partyAccountIds, GlTransactionType transactionType, int? branchId, DateTime? asOfDate = null)
    {
        var ids = partyAccountIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, decimal>();

        var query = FilterByBranch(
            ActiveLines()
                .Where(t => ids.Contains(t.AccountId))
                .Where(t => t.TransactionType == transactionType)
                .Where(t => t.ReferenceId != null),
            branchId);

        if (asOfDate.HasValue)
            query = query.Where(t => t.Date < asOfDate.Value.Date.AddDays(1));

        var useCredit = transactionType == GlTransactionType.Purchase;

        var rows = await query
            .GroupBy(t => t.ReferenceId!.Value)
            .Select(g => new
            {
                ReferenceId = g.Key,
                Amount = useCredit
                    ? g.Sum(t => t.CreditAmount)
                    : g.Sum(t => t.DebitAmount),
            })
            .ToListAsync();

        return rows.ToDictionary(r => r.ReferenceId, r => r.Amount);
    }

    public async Task<IReadOnlyList<DailyGlCashMovementRow>> GetDailyCashMovementsAsync(
        int? branchId, DateTime from, DateTime to)
    {
        var cashAccountIds = await ResolveCashAndBankAccountIdsAsync();
        if (cashAccountIds.Count == 0)
            return Array.Empty<DailyGlCashMovementRow>();

        var query = FilterByBranch(
            ActiveLines()
                .Where(t => cashAccountIds.Contains(t.AccountId))
                .Where(t => t.Date >= from.Date && t.Date < to.Date.AddDays(1)),
            branchId);

        var rows = await query
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.Date.Day })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                Net = g.Sum(t => t.DebitAmount - t.CreditAmount),
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.Day)
            .ToListAsync();

        return rows.Select(r => new DailyGlCashMovementRow
        {
            Date = new DateTime(r.Year, r.Month, r.Day),
            CashIn = r.Net > 0 ? r.Net : 0,
            CashOut = r.Net < 0 ? -r.Net : 0,
        }).ToList();
    }

    public async Task<GlCashDaySummary> GetGlCashDaySummaryAsync(int branchId, DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);
        var cashId = await _glAccounts.GetAccountIdByNameAsync(GlAccountDefaults.Cash);
        var bankId = await _glAccounts.GetAccountIdByNameAsync(GlAccountDefaults.Bank);

        var dayLines = await ActiveLines()
            .Where(t => t.BranchId == branchId)
            .Where(t => t.Date >= dayStart && t.Date < dayEnd)
            .Select(t => new { t.AccountId, t.TransactionType, t.DebitAmount, t.CreditAmount })
            .ToListAsync();

        var cashSales = cashId.HasValue
            ? dayLines.Where(t => t.AccountId == cashId && t.TransactionType == GlTransactionType.Sale).Sum(t => t.DebitAmount)
            : 0;
        var cardSales = bankId.HasValue
            ? dayLines.Where(t => t.AccountId == bankId && t.TransactionType == GlTransactionType.Sale).Sum(t => t.DebitAmount)
            : 0;

        // Cash outflows for expenses/purchases (category GL debits are on expense accounts, not Cash).
        var expenses = cashId.HasValue
            ? dayLines.Where(t => t.AccountId == cashId && t.TransactionType == GlTransactionType.Expense).Sum(t => t.CreditAmount)
            : 0;
        if (cashId.HasValue)
            expenses += dayLines.Where(t => t.AccountId == cashId && t.TransactionType == GlTransactionType.Purchase).Sum(t => t.CreditAmount);

        var cashIn = dayLines.Where(t => cashId.HasValue && t.AccountId == cashId && t.TransactionType == GlTransactionType.Receipt)
            .Sum(t => t.DebitAmount);
        if (cashId.HasValue)
            cashIn += dayLines.Where(t => t.AccountId == cashId && t.TransactionType == GlTransactionType.Adjustment).Sum(t => t.DebitAmount);

        var cashOut = dayLines.Where(t => cashId.HasValue && t.AccountId == cashId && t.TransactionType == GlTransactionType.Payment)
            .Sum(t => t.CreditAmount);
        if (cashId.HasValue)
            cashOut += dayLines.Where(t => t.AccountId == cashId && t.TransactionType == GlTransactionType.Adjustment).Sum(t => t.CreditAmount);
        var netCash = cashId.HasValue
            ? dayLines.Where(t => t.AccountId == cashId).Sum(t => t.DebitAmount - t.CreditAmount)
            : 0;
        var netBank = bankId.HasValue
            ? dayLines.Where(t => t.AccountId == bankId).Sum(t => t.DebitAmount - t.CreditAmount)
            : 0;

        return new GlCashDaySummary
        {
            CashSales = cashSales,
            CardSales = cardSales,
            Expenses = expenses,
            CashIn = cashIn,
            CashOut = cashOut,
            NetMovement = netCash + netBank,
        };
    }

    private static IQueryable<GlTransaction> FilterByBranch(IQueryable<GlTransaction> query, int? branchId)
    {
        if (branchId is > 0)
            return query.Where(t => t.BranchId == branchId.Value);
        return query;
    }

    private IQueryable<GlTransaction> ActiveLines() =>
        _db.GlTransactions.AsNoTracking().Where(t => t.IsActive);

    private async Task<List<int>> ResolveCashAndBankAccountIdsAsync()
    {
        var cashId = await _glAccounts.GetAccountIdByNameAsync(GlAccountDefaults.Cash);
        var bankId = await _glAccounts.GetAccountIdByNameAsync(GlAccountDefaults.Bank);
        return new[] { cashId, bankId }.Where(id => id.HasValue).Select(id => id!.Value).ToList();
    }
}
