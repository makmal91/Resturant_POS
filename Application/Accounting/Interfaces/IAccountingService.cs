using POSSystem.Application.Accounting.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Accounting.Interfaces;

public interface IAccountingService
{
    Task<Guid> CreateDoubleEntryAsync(IReadOnlyList<AccountingTransactionDto> entries);
    Task<Guid?> ReverseByReferenceAsync(int referenceId, GlTransactionType transactionType, string? descriptionPrefix = null);
    AccountingTransactionDto CreateEntry(
        int accountId,
        int branchId,
        decimal debit,
        decimal credit,
        Guid groupId,
        int? referenceId = null,
        string? description = null);
}
