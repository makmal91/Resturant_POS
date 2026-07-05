using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Accounting.DTOs;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class AccountLedgerRepository : IAccountLedgerRepository
{
    private readonly POSDbContext _db;

    public AccountLedgerRepository(POSDbContext db) => _db = db;

    public async Task<GlAccountListItemDto?> GetAccountAsync(int accountId)
    {
        return await _db.GlAccounts
            .AsNoTracking()
            .Where(a => a.Id == accountId && !a.IsDeleted)
            .Select(a => new GlAccountListItemDto
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type.ToString(),
                ParentId = a.ParentId,
                IsActive = a.IsActive,
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<GlAccountListItemDto>> ListAccountsAsync()
    {
        return await _db.GlAccounts
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.IsActive)
            .OrderBy(a => a.Type)
            .ThenBy(a => a.Name)
            .Select(a => new GlAccountListItemDto
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type.ToString(),
                ParentId = a.ParentId,
                IsActive = a.IsActive,
            })
            .ToListAsync();
    }

    public Task<decimal> GetOpeningBalanceAsync(
        IReadOnlyList<int> accountIds, DateTime fromDate, int? branchId, bool auditView) =>
        SumMovementsAsync(accountIds, branchId, auditView, t => t.Date < fromDate);

    public async Task<(decimal TotalDebit, decimal TotalCredit, int TotalRecords)> GetPeriodTotalsAsync(
        IReadOnlyList<int> accountIds, DateTime? fromDate, DateTime? toDate, int? branchId, bool auditView)
    {
        var query = ApplyDateRange(BaseLineQuery(accountIds, branchId, auditView), fromDate, toDate);

        var totals = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalDebit = g.Sum(t => t.DebitAmount),
                TotalCredit = g.Sum(t => t.CreditAmount),
                Count = g.Count(),
            })
            .FirstOrDefaultAsync();

        return (totals?.TotalDebit ?? 0, totals?.TotalCredit ?? 0, totals?.Count ?? 0);
    }

    public async Task<IReadOnlyList<AccountLedgerEntryDto>> GetLedgerLinesPagedAsync(
        IReadOnlyList<int> accountIds, DateTime? fromDate, DateTime? toDate, int? branchId,
        decimal openingBalance, int page, int pageSize, bool auditView, bool groupByChain)
    {
        var ledgerAccountIds = accountIds.Distinct().ToList();
        var showLineAccount = ledgerAccountIds.Count > 1;

        var query = ApplyDateRange(BaseLineQuery(ledgerAccountIds, branchId, auditView), fromDate, toDate);

        // Chronological: business date, then posting time, then stable id tie-break.
        var orderedQuery = groupByChain && auditView
            ? query.OrderBy(t => t.OriginalGroupId ?? t.GroupId).ThenBy(t => t.Date.Date).ThenBy(t => t.CreatedAt).ThenBy(t => t.Id)
            : query.OrderBy(t => t.Date.Date).ThenBy(t => t.CreatedAt).ThenBy(t => t.Id);

        var offset = Math.Max(0, (page - 1) * pageSize);

        var movementBeforePage = offset == 0
            ? 0m
            : await orderedQuery
                .Take(offset)
                .SumAsync(t => t.DebitAmount - t.CreditAmount);

        var pageRows = await orderedQuery
            .Skip(offset)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.AccountId,
                t.Date,
                t.TransactionType,
                t.ReferenceId,
                t.Description,
                t.DebitAmount,
                t.CreditAmount,
                t.GroupId,
                t.OriginalGroupId,
                t.IsActive,
                t.IsReversal,
            })
            .ToListAsync();

        var lineAccountNames = showLineAccount
            ? await _db.GlAccounts
                .AsNoTracking()
                .Where(a => ledgerAccountIds.Contains(a.Id) && !a.IsDeleted)
                .ToDictionaryAsync(a => a.Id, a => a.Name)
            : new Dictionary<int, string>();

        var contraNames = await GetContraAccountNamesAsync(
            ledgerAccountIds,
            pageRows.Select(r => r.GroupId),
            branchId,
            auditView);

        var running = openingBalance + movementBeforePage;
        var entries = new List<AccountLedgerEntryDto>(pageRows.Count);

        foreach (var row in pageRows)
        {
            running += row.DebitAmount - row.CreditAmount;
            var originalGroupId = row.OriginalGroupId ?? row.GroupId;
            entries.Add(new AccountLedgerEntryDto
            {
                Id = row.Id,
                Date = row.Date.Date,
                ReferenceType = row.TransactionType.ToString(),
                ReferenceId = row.ReferenceId,
                Description = row.Description ?? string.Empty,
                AccountName = contraNames.GetValueOrDefault(row.GroupId, string.Empty),
                LineAccountName = showLineAccount
                    ? lineAccountNames.GetValueOrDefault(row.AccountId, string.Empty)
                    : string.Empty,
                Debit = row.DebitAmount,
                Credit = row.CreditAmount,
                RunningBalance = running,
                IsOpeningBalance = false,
                GroupId = row.GroupId,
                OriginalGroupId = originalGroupId,
                IsActive = row.IsActive,
                IsReversal = row.IsReversal,
                IsSuperseded = !row.IsActive && !row.IsReversal,
                IsReplacement = row.IsActive && originalGroupId != row.GroupId,
            });
        }

        return entries;
    }

    public Task<decimal> GetSignedBalanceAsync(
        IReadOnlyList<int> accountIds, DateTime? asOfDate, int? branchId) =>
        SumMovementsAsync(accountIds, branchId, auditView: false, asOfDate.HasValue
            ? t => t.Date < asOfDate.Value.Date.AddDays(1)
            : null);

    private Task<decimal> SumMovementsAsync(
        IReadOnlyList<int> accountIds,
        int? branchId,
        bool auditView,
        System.Linq.Expressions.Expression<Func<GlTransaction, bool>>? predicate)
    {
        var query = BaseLineQuery(accountIds, branchId, auditView);
        if (predicate != null)
            query = query.Where(predicate);

        return query.SumAsync(t => t.DebitAmount - t.CreditAmount);
    }

    private IQueryable<GlTransaction> BaseLineQuery(IReadOnlyList<int> accountIds, int? branchId, bool auditView)
    {
        var ids = accountIds.Distinct().ToList();
        var query = _db.GlTransactions
            .AsNoTracking()
            .Where(t => ids.Contains(t.AccountId));

        if (branchId is > 0)
            query = query.Where(t => t.BranchId == branchId.Value);

        if (!auditView)
            query = query.Where(t => t.IsActive);

        return query;
    }

    private static IQueryable<GlTransaction> ApplyDateRange(
        IQueryable<GlTransaction> query, DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue)
            query = query.Where(t => t.Date >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(t => t.Date < toDate.Value.Date.AddDays(1));

        return query;
    }

    private async Task<Dictionary<Guid, string>> GetContraAccountNamesAsync(
        IReadOnlyList<int> ledgerAccountIds,
        IEnumerable<Guid> groupIds,
        int? branchId,
        bool auditView)
    {
        var ids = groupIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        var ledgerIds = ledgerAccountIds.Distinct().ToList();

        var query = _db.GlTransactions
            .AsNoTracking()
            .Where(t => ids.Contains(t.GroupId) && !ledgerIds.Contains(t.AccountId));

        if (branchId is > 0)
            query = query.Where(t => t.BranchId == branchId.Value);

        if (!auditView)
            query = query.Where(t => t.IsActive);

        var rows = await query
            .Join(
                _db.GlAccounts.AsNoTracking().Where(a => !a.IsDeleted),
                t => t.AccountId,
                a => a.Id,
                (t, a) => new { t.GroupId, a.Name })
            .ToListAsync();

        return rows
            .GroupBy(r => r.GroupId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.Select(x => x.Name).Distinct().OrderBy(n => n)));
    }
}

