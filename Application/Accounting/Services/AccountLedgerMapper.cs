using POSSystem.Application.Accounting.DTOs;
using POSSystem.Application.Accounting.Services;
using POSSystem.Application.CashFlow.DTOs;
using POSSystem.Application.Ledger.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Accounting.Services;

internal static class AccountLedgerMapper
{
    public static PartyLedgerPageDto ToPartyLedgerPage(
        AccountLedgerPageDto ledger, int partyId, string partyName, DateTime? fromDate)
    {
        var entries = ledger.Entries.Select(e => MapPartyEntry(e, ledger.AccountType)).ToList();

        var displayClosing = AccountLedgerService.ToDisplayBalance(ledger.AccountType, ledger.ClosingBalance);
        var openingExtra = fromDate.HasValue ? 1 : 0;
        var totalRecords = ledger.TotalRecords + openingExtra;
        var pageSize = ledger.PageSize > 0 ? ledger.PageSize : 50;
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);

        return new PartyLedgerPageDto
        {
            PartyId = partyId,
            PartyName = partyName,
            CurrentBalance = displayClosing,
            PeriodClosingBalance = displayClosing,
            EffectiveClosingBalance = AccountLedgerService.ToDisplayBalance(ledger.AccountType, ledger.EffectiveClosingBalance),
            AuditView = ledger.AuditView,
            Entries = entries,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = ledger.CurrentPage,
            TotalDebit = ledger.TotalDebit,
            TotalCredit = ledger.TotalCredit,
            TotalIn = ledger.TotalCredit,
            TotalOut = ledger.TotalDebit,
        };
    }

    public static CashFlowLedgerPageDto ToCashFlowLedgerPage(AccountLedgerPageDto ledger, int branchId)
    {
        var transactions = ledger.Entries.Select(e => new CashFlowTransactionDto
        {
            Id = e.Id,
            BranchId = branchId,
            TransactionType = e.IsOpeningBalance ? "OpeningBalance" : e.ReferenceType,
            PaymentMethod = "Cash",
            Amount = e.Debit > 0 ? e.Debit : e.Credit,
            Description = e.Description,
            AccountName = e.AccountName,
            TransactionDate = e.Date,
            RunningBalance = e.RunningBalance,
            Debit = e.Debit,
            Credit = e.Credit,
            IsInflow = e.Debit > 0,
            DisplayAmount = e.Debit > 0 ? e.Debit : e.Credit,
        }).ToList();

        return new CashFlowLedgerPageDto
        {
            AccountName = ledger.AccountName,
            Transactions = transactions,
            TotalRecords = ledger.TotalRecords + (ledger.Entries.Any(e => e.IsOpeningBalance) ? 1 : 0),
            TotalPages = ledger.TotalPages,
            CurrentPage = ledger.CurrentPage,
            PageSize = ledger.PageSize,
            TotalIn = ledger.TotalDebit,
            TotalOut = ledger.TotalCredit,
            NetTotal = ledger.PeriodNet,
            PeriodOpeningBalance = ledger.OpeningBalance,
            TotalDebit = ledger.TotalDebit,
            TotalCredit = ledger.TotalCredit,
        };
    }

    private static PartyLedgerEntryDto MapPartyEntry(AccountLedgerEntryDto entry, AccountType accountType)
    {
        var isPayment = entry.ReferenceType is nameof(GlTransactionType.Payment)
            or nameof(GlTransactionType.Receipt);

        var displayBalance = AccountLedgerService.ToDisplayBalance(accountType, entry.RunningBalance);

        return new PartyLedgerEntryDto
        {
            Id = entry.Id,
            Date = entry.Date,
            Type = entry.IsOpeningBalance ? "OpeningBalance" : entry.ReferenceType,
            Description = entry.Description,
            Debit = entry.Debit,
            Credit = entry.Credit,
            In = entry.Credit,
            Out = entry.Debit,
            RunningBalance = displayBalance,
            ReferenceId = entry.ReferenceId ?? 0,
            PaymentId = isPayment ? entry.ReferenceId : null,
            CanReverse = false,
            HasInvoiceBreakdown = false,
            IsActive = entry.IsActive,
            IsReversal = entry.IsReversal,
            IsSuperseded = entry.IsSuperseded,
            IsReplacement = entry.IsReplacement,
            GroupId = entry.GroupId == Guid.Empty ? null : entry.GroupId.ToString(),
            OriginalGroupId = entry.OriginalGroupId?.ToString(),
        };
    }
}
