using POSSystem.Application.Ledger.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Ledger.Services;

/// <summary>
/// Builds customer/supplier ledger pages from purchase/sale/payment documents.
/// Cash transactions appear for history; only credit documents affect running payable/receivable balance.
/// </summary>
internal static class PartyLedgerBuilder
{
    public static PartyLedgerPageDto Build(
        int partyId,
        string partyName,
        bool isCustomer,
        IReadOnlyList<PartyLedgerSourceDto> sources,
        decimal currentBalance,
        PartyLedgerFilterDto filter)
    {
        var page = filter.Page > 0 ? filter.Page : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 50;
        var fromDate = filter.FromDate?.Date;
        var toDate = filter.ToDate?.Date;

        var ordered = sources
            .OrderBy(s => s.Date)
            .ThenBy(s => s.Id)
            .ToList();

        var openingBalance = 0m;
        if (fromDate.HasValue)
        {
            foreach (var source in ordered.Where(s => s.Date.Date < fromDate.Value))
            {
                if (source.AffectsBalance)
                    openingBalance += BalanceDelta(source, isCustomer);
            }
        }

        var periodSources = ordered
            .Where(s => (!fromDate.HasValue || s.Date.Date >= fromDate.Value)
                        && (!toDate.HasValue || s.Date.Date <= toDate.Value))
            .ToList();

        var totalRecords = periodSources.Count + (fromDate.HasValue ? 1 : 0);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));
        var offset = Math.Max(0, (page - 1) * pageSize);

        var runningBeforePage = openingBalance;
        foreach (var source in periodSources.Take(offset))
        {
            if (source.AffectsBalance)
                runningBeforePage += BalanceDelta(source, isCustomer);
        }

        var entries = new List<PartyLedgerEntryDto>();
        if (page == 1 && fromDate.HasValue)
        {
            entries.Add(new PartyLedgerEntryDto
            {
                Date = fromDate.Value,
                Type = "OpeningBalance",
                Description = "Opening Balance",
                RunningBalance = ToDisplayBalance(openingBalance, isCustomer),
                AffectsPayableBalance = false,
            });
        }

        var running = runningBeforePage;
        var pageSources = periodSources.Skip(offset).Take(pageSize).ToList();
        foreach (var source in pageSources)
        {
            if (source.AffectsBalance)
                running += BalanceDelta(source, isCustomer);

            var (debit, credit) = MapAmounts(source, isCustomer);
            entries.Add(new PartyLedgerEntryDto
            {
                Id = source.Id,
                Date = source.Date,
                Type = source.Type,
                Description = source.Description,
                Debit = debit,
                Credit = credit,
                In = isCustomer ? credit : credit,
                Out = isCustomer ? debit : debit,
                RunningBalance = ToDisplayBalance(running, isCustomer),
                ReferenceId = source.ReferenceId,
                PaymentId = source.PaymentId,
                HasInvoiceBreakdown = source.HasInvoiceBreakdown,
                InvoiceAllocations = source.InvoiceAllocations,
                IsReversal = source.IsReversal,
                AffectsPayableBalance = source.AffectsBalance,
            });
        }

        var periodClosing = openingBalance;
        foreach (var source in periodSources)
        {
            if (source.AffectsBalance)
                periodClosing += BalanceDelta(source, isCustomer);
        }

        var totalDebit = periodSources.Sum(s => MapAmounts(s, isCustomer).Debit);
        var totalCredit = periodSources.Sum(s => MapAmounts(s, isCustomer).Credit);

        return new PartyLedgerPageDto
        {
            PartyId = partyId,
            PartyName = partyName,
            CurrentBalance = ToDisplayBalance(currentBalance, isCustomer),
            PeriodClosingBalance = ToDisplayBalance(periodClosing, isCustomer),
            EffectiveClosingBalance = ToDisplayBalance(currentBalance, isCustomer),
            AuditView = filter.AuditView,
            Entries = entries,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            TotalIn = isCustomer ? totalCredit : totalCredit,
            TotalOut = isCustomer ? totalDebit : totalDebit,
        };
    }

    public static decimal ComputeBalanceFromActivity(IReadOnlyList<PartyLedgerSourceDto> sources, bool isCustomer)
    {
        decimal balance = 0;
        foreach (var source in sources.Where(s => s.AffectsBalance))
            balance += BalanceDelta(source, isCustomer);
        return balance;
    }

    private static decimal BalanceDelta(PartyLedgerSourceDto source, bool isCustomer)
    {
        if (isCustomer)
        {
            return source.Type switch
            {
                nameof(CustomerLedgerTransactionType.CreditSale) or nameof(CustomerLedgerTransactionType.CashSale) => source.AffectsBalance ? source.Amount : 0,
                nameof(CustomerLedgerTransactionType.PaymentReceived) => -source.Amount,
                nameof(CustomerLedgerTransactionType.Reversal) => source.Amount,
                _ => 0,
            };
        }

        return source.Type switch
        {
            nameof(SupplierLedgerTransactionType.CreditPurchase) => source.Amount,
            nameof(SupplierLedgerTransactionType.CashPurchase) => 0,
            nameof(SupplierLedgerTransactionType.PaymentMade) => -source.Amount,
            nameof(SupplierLedgerTransactionType.Reversal) => source.Amount,
            _ => 0,
        };
    }

    private static (decimal Debit, decimal Credit) MapAmounts(PartyLedgerSourceDto source, bool isCustomer)
    {
        if (isCustomer)
        {
        return source.Type switch
        {
            nameof(CustomerLedgerTransactionType.CreditSale) or nameof(CustomerLedgerTransactionType.CashSale)
                => (source.Amount, 0),
            nameof(CustomerLedgerTransactionType.PaymentReceived)
                => (0, source.Amount),
            nameof(CustomerLedgerTransactionType.Reversal)
                => (source.Amount, 0),
            _ => source.Amount > 0 ? (source.Amount, 0) : (0, Math.Abs(source.Amount)),
        };
        }

        return source.Type switch
        {
            nameof(SupplierLedgerTransactionType.CreditPurchase) or nameof(SupplierLedgerTransactionType.CashPurchase)
                => (0, source.Amount),
            nameof(SupplierLedgerTransactionType.PaymentMade)
                => (source.Amount, 0),
            nameof(SupplierLedgerTransactionType.Reversal)
                => (0, source.Amount),
            _ => source.Amount > 0 ? (source.Amount, 0) : (0, Math.Abs(source.Amount)),
        };
    }

    private static decimal ToDisplayBalance(decimal rawBalance, bool isCustomer) =>
        isCustomer ? rawBalance : Math.Max(0, rawBalance);
}
