using System.Data;
using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class AccountingRepository : IAccountingRepository
{
    private readonly POSDbContext _db;

    public AccountingRepository(POSDbContext db) => _db = db;

    public async Task<bool> AllAccountsExistAsync(IReadOnlyCollection<int> accountIds)
    {
        if (accountIds.Count == 0)
            return true;

        var existingCount = await _db.GlAccounts
            .IgnoreQueryFilters()
            .CountAsync(a => accountIds.Contains(a.Id) && !a.IsDeleted && a.IsActive);

        return existingCount == accountIds.Count;
    }

    public Task<bool> ExistsForReferenceAsync(int referenceId, GlTransactionType transactionType) =>
        _db.GlTransactions.AnyAsync(t =>
            t.ReferenceId == referenceId
            && t.TransactionType == transactionType
            && t.IsActive);

    public async Task<bool> HasCompleteBalancedJournalAsync(int referenceId, GlTransactionType transactionType)
    {
        var lines = await GetActiveLinesForReferenceAsync(referenceId, transactionType);
        if (lines.Count < 2)
            return false;

        var totalDebit = lines.Sum(l => l.DebitAmount);
        var totalCredit = lines.Sum(l => l.CreditAmount);
        return totalDebit > 0 && totalDebit == totalCredit;
    }

    public Task<List<GlTransaction>> GetActiveLinesForReferenceAsync(int referenceId, GlTransactionType transactionType) =>
        _db.GlTransactions
            .Where(t =>
                t.ReferenceId == referenceId
                && t.TransactionType == transactionType
                && t.IsActive)
            .OrderBy(t => t.Id)
            .ToListAsync();

    public async Task<Guid?> GetLedgerChainIdForReferenceAsync(int referenceId, GlTransactionType transactionType)
    {
        var chainId = await _db.GlTransactions
            .Where(t => t.ReferenceId == referenceId && t.TransactionType == transactionType)
            .OrderBy(t => t.Id)
            .Select(t => (Guid?)(t.OriginalGroupId ?? t.GroupId))
            .FirstOrDefaultAsync();

        return chainId;
    }

    public async Task AddRangeAsync(IEnumerable<GlTransaction> transactions) =>
        await _db.GlTransactions.AddRangeAsync(transactions);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public async Task RunInTransactionAsync(Func<Task> action)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await action();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
}
