using POSSystem.Application.Accounting.DTOs;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Accounting.Services;

public class AccountingService : IAccountingService
{
    private readonly IAccountingRepository _repository;

    public AccountingService(IAccountingRepository repository) => _repository = repository;

    public async Task<Guid> CreateDoubleEntryAsync(IReadOnlyList<AccountingTransactionDto> entries)
    {
        var first = entries[0];
        if (first.IsActive
            && first.ReferenceId is > 0
            && first.TransactionType != GlTransactionType.Reversal
            && await _repository.ExistsForReferenceAsync(first.ReferenceId.Value, first.TransactionType))
        {
            throw new InvalidOperationException(
                $"An active journal already exists for {first.TransactionType} reference {first.ReferenceId}.");
        }

        return await PersistDoubleEntryAsync(entries, saveChanges: true);
    }

    /// <summary>
    /// Deactivates the current active journal for a source document and posts an inactive reversal.
    /// Call <see cref="CreateDoubleEntryAsync"/> afterward to post the replacement (edit flow).
    /// </summary>
    public async Task<Guid?> ReverseByReferenceAsync(
        int referenceId,
        GlTransactionType transactionType,
        string? descriptionPrefix = null)
    {
        var originalLines = await _repository.GetActiveLinesForReferenceAsync(referenceId, transactionType);
        if (originalLines.Count == 0)
            return null;

        var chainId = originalLines[0].OriginalGroupId ?? originalLines[0].GroupId;
        var reversalGroupId = Guid.NewGuid();
        var reversalDate = DateTime.UtcNow;
        var prefix = string.IsNullOrWhiteSpace(descriptionPrefix) ? "Reversal" : descriptionPrefix.Trim();

        // Step 1: deactivate superseded originals
        foreach (var line in originalLines)
        {
            line.IsActive = false;
            if (!line.OriginalGroupId.HasValue)
                line.OriginalGroupId = line.GroupId;
        }

        // Step 2: post inactive reversal journal (debit/credit swapped) — single SaveChanges
        var reversalEntries = originalLines.Select(line =>
        {
            var entry = CreateEntry(
                line.AccountId,
                line.BranchId,
                line.CreditAmount,
                line.DebitAmount,
                reversalGroupId,
                referenceId,
                $"{prefix} — {line.Description}".Trim(' ', '—'));
            entry.TransactionType = GlTransactionType.Reversal;
            entry.Date = reversalDate;
            entry.ReversalOfGroupId = line.GroupId;
            entry.OriginalGroupId = chainId;
            entry.IsReversal = true;
            entry.IsActive = false;
            return entry;
        }).ToList();

        await PersistDoubleEntryAsync(reversalEntries, saveChanges: true);
        return reversalGroupId;
    }

    public AccountingTransactionDto CreateEntry(
        int accountId,
        int branchId,
        decimal debit,
        decimal credit,
        Guid groupId,
        int? referenceId = null,
        string? description = null) =>
        new()
        {
            AccountId = accountId,
            BranchId = branchId,
            DebitAmount = debit,
            CreditAmount = credit,
            GroupId = groupId,
            ReferenceId = referenceId,
            Description = description,
            IsActive = true,
        };

    private async Task<Guid> PersistDoubleEntryAsync(IReadOnlyList<AccountingTransactionDto> entries, bool saveChanges)
    {
        ValidateEntries(entries);

        var accountIds = entries.Select(e => e.AccountId).Distinct().ToList();
        if (!await _repository.AllAccountsExistAsync(accountIds))
            throw new InvalidOperationException("One or more GL accounts were not found.");

        var journalDate = entries[0].Date ?? DateTime.UtcNow;
        var groupId = entries[0].GroupId;

        var transactions = entries.Select(e => new GlTransaction
        {
            Date = e.Date ?? journalDate,
            AccountId = e.AccountId,
            BranchId = e.BranchId,
            DebitAmount = e.DebitAmount,
            CreditAmount = e.CreditAmount,
            TransactionType = e.TransactionType,
            ReferenceId = e.ReferenceId,
            GroupId = groupId,
            Description = e.Description?.Trim(),
            ReversalOfGroupId = e.ReversalOfGroupId,
            OriginalGroupId = e.OriginalGroupId ?? groupId,
            IsActive = e.IsActive,
            IsReversal = e.IsReversal,
        }).ToList();

        await _repository.AddRangeAsync(transactions);
        if (saveChanges)
            await _repository.SaveChangesAsync();

        return groupId;
    }

    private static void ValidateEntries(IReadOnlyList<AccountingTransactionDto> entries)
    {
        if (entries.Count < 2)
            throw new InvalidOperationException("A double-entry journal requires at least two lines.");

        var groupId = entries[0].GroupId;
        if (entries.Any(e => e.GroupId != groupId))
            throw new InvalidOperationException("All journal lines must share the same GroupId.");

        if (groupId == Guid.Empty)
            throw new InvalidOperationException("GroupId is required.");

        var isActive = entries[0].IsActive;
        if (entries.Any(e => e.IsActive != isActive))
            throw new InvalidOperationException("All journal lines in one posting must share the same IsActive value.");

        var branchId = entries[0].BranchId;
        if (branchId <= 0)
            throw new InvalidOperationException("BranchId is required on every journal line.");
        if (entries.Any(e => e.BranchId != branchId))
            throw new InvalidOperationException("All journal lines in one posting must share the same BranchId.");

        decimal totalDebit = 0;
        decimal totalCredit = 0;

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            if (entry.AccountId <= 0)
                throw new InvalidOperationException($"Line {i + 1}: AccountId is required.");

            if (entry.DebitAmount < 0 || entry.CreditAmount < 0)
                throw new InvalidOperationException($"Line {i + 1}: Debit and credit amounts cannot be negative.");

            if (entry.DebitAmount == 0 && entry.CreditAmount == 0)
                throw new InvalidOperationException($"Line {i + 1}: Either debit or credit must be greater than zero.");

            if (entry.DebitAmount > 0 && entry.CreditAmount > 0)
                throw new InvalidOperationException($"Line {i + 1}: A line cannot have both debit and credit amounts.");

            totalDebit += entry.DebitAmount;
            totalCredit += entry.CreditAmount;
        }

        if (totalDebit != totalCredit)
            throw new InvalidOperationException(
                $"Journal is not balanced. Total debit ({totalDebit:N2}) must equal total credit ({totalCredit:N2}).");
    }
}
