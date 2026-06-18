using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Ledger.DTOs;
using POSSystem.Application.Ledger.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class PartyLedgerRepository : IPartyLedgerRepository
{
    private readonly POSDbContext _db;

    public PartyLedgerRepository(POSDbContext db) => _db = db;

    public Task<CustomerLedgerTransaction> AddCustomerEntryAsync(CustomerLedgerTransaction entry)
    {
        _db.CustomerLedgerTransactions.Add(entry);
        return Task.FromResult(entry);
    }

    public Task<SupplierLedgerTransaction> AddSupplierEntryAsync(SupplierLedgerTransaction entry)
    {
        _db.SupplierLedgerTransactions.Add(entry);
        return Task.FromResult(entry);
    }

    public async Task<decimal> GetCustomerRunningBalanceAsync(int customerId, int businessId, int branchId)
    {
        var openingBalance = await GetCustomerOpeningBalanceAsync(customerId, businessId, branchId);
        var entries = await GetCustomerLedgerTotalsAsync(customerId, businessId, branchId);
        return CalculateBalance(openingBalance, entries);
    }

    public async Task<decimal> GetSupplierRunningBalanceAsync(int supplierId, int businessId, int branchId)
    {
        var entries = await GetSupplierLedgerTotalsAsync(supplierId, businessId, branchId);
        return CalculateBalance(0m, entries);
    }

    public Task<List<CustomerLedgerTransaction>> GetCustomerEntriesByReferenceAsync(
        int referenceId, int businessId, int branchId, CustomerLedgerTransactionType type)
    {
        return _db.CustomerLedgerTransactions
            .AsNoTracking()
            .Where(t => t.ReferenceId == referenceId
                        && t.BusinessId == businessId
                        && t.BranchId == branchId
                        && t.Type == type)
            .ToListAsync();
    }

    public Task<List<SupplierLedgerTransaction>> GetSupplierEntriesByReferenceAsync(
        int referenceId, int businessId, int branchId, SupplierLedgerTransactionType type)
    {
        return _db.SupplierLedgerTransactions
            .AsNoTracking()
            .Where(t => t.ReferenceId == referenceId
                        && t.BusinessId == businessId
                        && t.BranchId == branchId
                        && t.Type == type)
            .ToListAsync();
    }

    public Task<Customer?> GetCustomerAsync(int customerId, int businessId, int branchId)
    {
        return _db.Customers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c =>
                c.Id == customerId &&
                !c.IsDeleted &&
                c.BusinessId == businessId &&
                c.BranchId == branchId);
    }

    public Task<Supplier?> GetSupplierAsync(int supplierId, int businessId, int branchId)
    {
        return _db.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId && s.BusinessId == businessId && s.BranchId == branchId);
    }

    public async Task<PartyLedgerPageDto> GetCustomerLedgerPagedAsync(PartyLedgerFilterDto filter)
    {
        var customer = await GetCustomerAsync(filter.PartyId, filter.BusinessId, filter.BranchId)
            ?? throw new InvalidOperationException("Customer not found.");

        var query = _db.CustomerLedgerTransactions
            .AsNoTracking()
            .Where(t => t.CustomerId == filter.PartyId
                        && t.BusinessId == filter.BusinessId
                        && t.BranchId == filter.BranchId);

        if (filter.FromDate.HasValue)
            query = query.Where(t => t.Date >= filter.FromDate.Value.Date);

        if (filter.ToDate.HasValue)
            query = query.Where(t => t.Date < filter.ToDate.Value.Date.AddDays(1));

        var transactions = await query
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Id)
            .ToListAsync();

        var openingBalance = await GetCustomerOpeningBalanceAsync(filter.PartyId, filter.BusinessId, filter.BranchId);
        var entries = BuildCustomerLedgerEntries(SortCustomerTransactions(transactions), openingBalance);

        var totalRecords = entries.Count;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 50;
        var page = filter.Page > 0 ? filter.Page : 1;
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);

        var pagedEntries = entries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var currentBalance = await GetCustomerRunningBalanceAsync(filter.PartyId, filter.BusinessId, filter.BranchId);

        return new PartyLedgerPageDto
        {
            PartyId = filter.PartyId,
            PartyName = customer.Name,
            CurrentBalance = currentBalance,
            Entries = pagedEntries,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page
        };
    }

    public async Task<PartyLedgerPageDto> GetSupplierLedgerPagedAsync(PartyLedgerFilterDto filter)
    {
        var supplier = await GetSupplierAsync(filter.PartyId, filter.BusinessId, filter.BranchId)
            ?? throw new InvalidOperationException("Supplier not found.");

        var query = _db.SupplierLedgerTransactions
            .AsNoTracking()
            .Where(t => t.SupplierId == filter.PartyId
                        && t.BusinessId == filter.BusinessId
                        && t.BranchId == filter.BranchId);

        if (filter.FromDate.HasValue)
            query = query.Where(t => t.Date >= filter.FromDate.Value.Date);

        if (filter.ToDate.HasValue)
            query = query.Where(t => t.Date < filter.ToDate.Value.Date.AddDays(1));

        var transactions = await query
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Id)
            .ToListAsync();

        var entries = BuildSupplierLedgerEntries(SortSupplierTransactions(transactions));

        var totalRecords = entries.Count;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 50;
        var page = filter.Page > 0 ? filter.Page : 1;
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);

        var pagedEntries = entries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var currentBalance = await GetSupplierRunningBalanceAsync(filter.PartyId, filter.BusinessId, filter.BranchId);

        return new PartyLedgerPageDto
        {
            PartyId = filter.PartyId,
            PartyName = supplier.Name,
            CurrentBalance = currentBalance,
            Entries = pagedEntries,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page
        };
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    private async Task<decimal> GetCustomerOpeningBalanceAsync(int customerId, int businessId, int branchId)
    {
        var hasOpeningEntry = await _db.CustomerLedgerTransactions
            .AsNoTracking()
            .AnyAsync(t => t.CustomerId == customerId
                           && t.BusinessId == businessId
                           && t.BranchId == branchId
                           && t.Type == CustomerLedgerTransactionType.OpeningBalance);

        if (hasOpeningEntry)
            return 0m;

        var openingBalance = await _db.Customers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => c.Id == customerId && !c.IsDeleted && c.BusinessId == businessId && c.BranchId == branchId)
            .Select(c => (decimal?)c.OpeningBalance)
            .FirstOrDefaultAsync();

        return openingBalance ?? 0m;
    }

    private async Task<List<LedgerAmountRow>> GetCustomerLedgerTotalsAsync(int customerId, int businessId, int branchId)
    {
        var rows = await _db.CustomerLedgerTransactions
            .AsNoTracking()
            .Where(t => t.CustomerId == customerId && t.BusinessId == businessId && t.BranchId == branchId)
            .Select(t => new CustomerLedgerSortRow
            {
                Id = t.Id,
                Date = t.Date,
                Type = t.Type,
                Debit = t.Debit,
                Credit = t.Credit
            })
            .ToListAsync();

        return SortCustomerAmountRows(rows);
    }

    private async Task<List<LedgerAmountRow>> GetSupplierLedgerTotalsAsync(int supplierId, int businessId, int branchId)
    {
        var rows = await _db.SupplierLedgerTransactions
            .AsNoTracking()
            .Where(t => t.SupplierId == supplierId && t.BusinessId == businessId && t.BranchId == branchId)
            .Select(t => new SupplierLedgerSortRow
            {
                Id = t.Id,
                Date = t.Date,
                Type = t.Type,
                Debit = t.Debit,
                Credit = t.Credit
            })
            .ToListAsync();

        return SortSupplierAmountRows(rows);
    }

    private static List<CustomerLedgerTransaction> SortCustomerTransactions(IReadOnlyList<CustomerLedgerTransaction> transactions)
    {
        return transactions
            .OrderBy(t => GetEffectiveCustomerLedgerDate(t.Date, t.Type))
            .ThenBy(t => t.Id)
            .ToList();
    }

    private static List<SupplierLedgerTransaction> SortSupplierTransactions(IReadOnlyList<SupplierLedgerTransaction> transactions)
    {
        return transactions
            .OrderBy(t => GetEffectiveSupplierLedgerDate(t.Date, t.Type))
            .ThenBy(t => t.Id)
            .ToList();
    }

    private static List<LedgerAmountRow> SortCustomerAmountRows(IReadOnlyList<CustomerLedgerSortRow> rows)
    {
        return rows
            .OrderBy(t => GetEffectiveCustomerLedgerDate(t.Date, t.Type))
            .ThenBy(t => t.Id)
            .Select(t => new LedgerAmountRow { Debit = t.Debit, Credit = t.Credit })
            .ToList();
    }

    private static List<LedgerAmountRow> SortSupplierAmountRows(IReadOnlyList<SupplierLedgerSortRow> rows)
    {
        return rows
            .OrderBy(t => GetEffectiveSupplierLedgerDate(t.Date, t.Type))
            .ThenBy(t => t.Id)
            .Select(t => new LedgerAmountRow { Debit = t.Debit, Credit = t.Credit })
            .ToList();
    }

    private static DateTime GetEffectiveCustomerLedgerDate(DateTime date, CustomerLedgerTransactionType type)
    {
        if (type == CustomerLedgerTransactionType.PaymentReceived && date.TimeOfDay == TimeSpan.Zero)
            return date.Date.AddDays(1).AddTicks(-1);

        return date;
    }

    private static DateTime GetEffectiveSupplierLedgerDate(DateTime date, SupplierLedgerTransactionType type)
    {
        if (type == SupplierLedgerTransactionType.PaymentMade && date.TimeOfDay == TimeSpan.Zero)
            return date.Date.AddDays(1).AddTicks(-1);

        return date;
    }

    private static decimal CalculateBalance(decimal openingBalance, IReadOnlyList<LedgerAmountRow> entries)
    {
        var balance = openingBalance;
        foreach (var entry in entries)
            balance += entry.Debit - entry.Credit;
        return balance;
    }

    private static List<PartyLedgerEntryDto> BuildCustomerLedgerEntries(
        IReadOnlyList<CustomerLedgerTransaction> transactions, decimal openingBalance)
    {
        var entries = new List<PartyLedgerEntryDto>(transactions.Count);
        var running = openingBalance;

        foreach (var transaction in transactions)
        {
            running += transaction.Debit - transaction.Credit;
            entries.Add(new PartyLedgerEntryDto
            {
                Id = transaction.Id,
                Date = transaction.Date,
                Type = transaction.Type.ToString(),
                Description = transaction.Remarks,
                Debit = transaction.Debit,
                Credit = transaction.Credit,
                RunningBalance = running,
                ReferenceId = transaction.ReferenceId
            });
        }

        return entries;
    }

    private static List<PartyLedgerEntryDto> BuildSupplierLedgerEntries(
        IReadOnlyList<SupplierLedgerTransaction> transactions)
    {
        var entries = new List<PartyLedgerEntryDto>(transactions.Count);
        var running = 0m;

        foreach (var transaction in transactions)
        {
            running += transaction.Debit - transaction.Credit;
            entries.Add(new PartyLedgerEntryDto
            {
                Id = transaction.Id,
                Date = transaction.Date,
                Type = transaction.Type.ToString(),
                Description = transaction.Remarks,
                Debit = transaction.Debit,
                Credit = transaction.Credit,
                RunningBalance = running,
                ReferenceId = transaction.ReferenceId
            });
        }

        return entries;
    }

    private sealed class LedgerAmountRow
    {
        public decimal Debit { get; init; }
        public decimal Credit { get; init; }
    }

    private sealed class CustomerLedgerSortRow
    {
        public int Id { get; init; }
        public DateTime Date { get; init; }
        public CustomerLedgerTransactionType Type { get; init; }
        public decimal Debit { get; init; }
        public decimal Credit { get; init; }
    }

    private sealed class SupplierLedgerSortRow
    {
        public int Id { get; init; }
        public DateTime Date { get; init; }
        public SupplierLedgerTransactionType Type { get; init; }
        public decimal Debit { get; init; }
        public decimal Credit { get; init; }
    }
}
