using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Accounting.DTOs;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class TrialBalanceRepository : ITrialBalanceRepository
{
    private readonly POSDbContext _db;

    public TrialBalanceRepository(POSDbContext db) => _db = db;

    public async Task<IReadOnlyList<GlAccountListItemDto>> GetActiveAccountsAsync()
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

    public async Task<IReadOnlyList<AccountPeriodTotalsRow>> GetAccountPeriodTotalsAsync(
        int? branchId, DateTime? fromDate, DateTime? toDate)
    {
        var query = _db.GlTransactions
            .AsNoTracking()
            .Where(t => t.IsActive);

        if (branchId is > 0)
            query = query.Where(t => t.BranchId == branchId.Value);

        if (fromDate.HasValue)
            query = query.Where(t => t.Date >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(t => t.Date < toDate.Value.Date.AddDays(1));

        return await query
            .GroupBy(t => t.AccountId)
            .Select(g => new AccountPeriodTotalsRow
            {
                AccountId = g.Key,
                TotalDebit = g.Sum(t => t.DebitAmount),
                TotalCredit = g.Sum(t => t.CreditAmount),
            })
            .ToListAsync();
    }
}
