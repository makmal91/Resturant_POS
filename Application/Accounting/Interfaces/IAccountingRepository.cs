using POSSystem.Domain;

namespace POSSystem.Application.Accounting.Interfaces;

public interface IAccountingRepository
{
    Task<bool> AllAccountsExistAsync(IReadOnlyCollection<int> accountIds);
    Task<bool> ExistsForReferenceAsync(int referenceId, GlTransactionType transactionType);
    Task<bool> HasCompleteBalancedJournalAsync(int referenceId, GlTransactionType transactionType);
    Task<Guid?> GetLedgerChainIdForReferenceAsync(int referenceId, GlTransactionType transactionType);
    Task<List<GlTransaction>> GetActiveLinesForReferenceAsync(int referenceId, GlTransactionType transactionType);
    Task AddRangeAsync(IEnumerable<GlTransaction> transactions);
    Task SaveChangesAsync();
    Task RunInTransactionAsync(Func<Task> action);
}
