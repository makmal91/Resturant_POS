using Microsoft.EntityFrameworkCore;
using POSSystem.Application.CashFlow.DTOs;
using POSSystem.Application.CashFlow.Interfaces;
using POSSystem.Application.Common.DTOs;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class CashFlowRepository : ICashFlowRepository
{
    private readonly POSDbContext _db;

    public CashFlowRepository(POSDbContext db) => _db = db;

    // ─── Transactions ──────────────────────────────────────────────────────────

    public async Task<CashFlowTransaction> AddTransactionAsync(CashFlowTransaction transaction)
    {
        _db.CashFlowTransactions.Add(transaction);
        await _db.SaveChangesAsync();
        return transaction;
    }

    public async Task<CashFlowLedgerPageDto> GetLedgerPagedAsync(CashFlowLedgerFilterDto filter)
    {
        var query = _db.CashFlowTransactions
            .Include(t => t.Branch)
            .AsNoTracking()
            .Where(t => t.BusinessId == filter.BusinessId);

        if (filter.BranchId > 0)
            query = query.Where(t => t.BranchId == filter.BranchId);

        if (filter.FromDate.HasValue)
            query = query.Where(t => t.TransactionDate >= filter.FromDate.Value.Date);

        if (filter.ToDate.HasValue)
            query = query.Where(t => t.TransactionDate < filter.ToDate.Value.Date.AddDays(1));

        if (filter.TransactionType.HasValue)
            query = query.Where(t => t.TransactionType == filter.TransactionType.Value);

        if (filter.PaymentMethod.HasValue)
            query = query.Where(t => t.PaymentMethod == filter.PaymentMethod.Value);

        var inflowTypes = new[]
        {
            CashFlowTransactionType.Sale,
            CashFlowTransactionType.CashIn,
            CashFlowTransactionType.OpeningBalance,
        };

        var totalIn = await query
            .Where(t => inflowTypes.Contains(t.TransactionType))
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        var totalOut = await query
            .Where(t => !inflowTypes.Contains(t.TransactionType))
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        var totalRecords = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new CashFlowTransactionDto
            {
                Id              = t.Id,
                BranchId        = t.BranchId,
                BranchName      = t.Branch.Name,
                TransactionType = t.TransactionType.ToString(),
                PaymentMethod   = t.PaymentMethod.ToString(),
                Amount          = t.Amount,
                ReferenceNo     = t.ReferenceNo,
                Description     = t.Description,
                TransactionDate = t.TransactionDate,
                CreatedBy       = t.CreatedBy,
                CreatedAt       = t.CreatedAt,
            })
            .ToListAsync();

        return new CashFlowLedgerPageDto
        {
            Transactions = items,
            TotalRecords = totalRecords,
            TotalPages   = (int)Math.Ceiling(totalRecords / (double)filter.PageSize),
            CurrentPage  = filter.Page,
            PageSize     = filter.PageSize,
            TotalIn      = totalIn,
            TotalOut     = totalOut,
            NetTotal     = totalIn - totalOut,
        };
    }

    // ─── Cash Register ─────────────────────────────────────────────────────────

    public async Task<CashRegister?> GetRegisterAsync(int businessId, int branchId, DateTime date)
    {
        var dateOnly = date.Date;
        return await _db.CashRegisters
            .Include(r => r.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.BusinessId == businessId &&
                r.BranchId   == branchId   &&
                r.RegisterDate == dateOnly);
    }

    public async Task<CashRegister> AddRegisterAsync(CashRegister register)
    {
        _db.CashRegisters.Add(register);
        await _db.SaveChangesAsync();
        return register;
    }

    public async Task UpdateRegisterAsync(CashRegister register)
    {
        _db.CashRegisters.Update(register);
        await _db.SaveChangesAsync();
    }

    // ─── Summaries ─────────────────────────────────────────────────────────────

    public async Task<DailyCashSummaryDto> GetDailySummaryAsync(int businessId, int branchId, DateTime date)
    {
        var dateOnly  = date.Date;
        var datePlus1 = dateOnly.AddDays(1);

        var register = await _db.CashRegisters
            .Include(r => r.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.BusinessId   == businessId &&
                r.BranchId     == branchId   &&
                r.RegisterDate == dateOnly);

        var txQuery = _db.CashFlowTransactions
            .AsNoTracking()
            .Where(t => t.BusinessId == businessId
                     && t.BranchId   == branchId
                     && t.TransactionDate >= dateOnly
                     && t.TransactionDate < datePlus1);

        var cashSales   = await txQuery.Where(t => t.TransactionType == CashFlowTransactionType.Sale && t.PaymentMethod == CashFlowPaymentMethod.Cash).SumAsync(t => (decimal?)t.Amount) ?? 0;
        var cardSales   = await txQuery.Where(t => t.TransactionType == CashFlowTransactionType.Sale && t.PaymentMethod == CashFlowPaymentMethod.Bank).SumAsync(t => (decimal?)t.Amount) ?? 0;
        var expenses    = await txQuery.Where(t => t.TransactionType == CashFlowTransactionType.Expense && t.PaymentMethod == CashFlowPaymentMethod.Cash).SumAsync(t => (decimal?)t.Amount) ?? 0;
        var cashIn      = await txQuery.Where(t => t.TransactionType == CashFlowTransactionType.CashIn).SumAsync(t => (decimal?)t.Amount) ?? 0;
        var cashOut     = await txQuery.Where(t => t.TransactionType == CashFlowTransactionType.CashOut).SumAsync(t => (decimal?)t.Amount) ?? 0;
        var bankTx      = await txQuery.Where(t => t.TransactionType == CashFlowTransactionType.BankTransfer).SumAsync(t => (decimal?)t.Amount) ?? 0;

        var opening = register?.OpeningCash ?? 0;
        var expected = opening + cashSales + cashIn - expenses - cashOut - bankTx;

        var branchName = register?.Branch?.Name
            ?? await _db.Branches.AsNoTracking().Where(b => b.Id == branchId).Select(b => b.Name).FirstOrDefaultAsync()
            ?? "Unknown";

        return new DailyCashSummaryDto
        {
            BranchId           = branchId,
            BranchName         = branchName,
            Date               = dateOnly,
            OpeningCash        = opening,
            TotalCashSales     = cashSales,
            TotalCardSales     = cardSales,
            TotalExpensesCash  = expenses,
            TotalCashIn        = cashIn,
            TotalCashOut       = cashOut,
            TotalBankTransfers = bankTx,
            ExpectedClosingCash = expected,
            ActualClosingCash  = register?.ActualCash,
            Difference         = register?.Difference,
            IsRegistered       = register != null,
            IsClosed           = register?.IsClosed ?? false,
        };
    }

    public async Task<MonthlyCashSummaryDto> GetMonthlySummaryAsync(int businessId, int branchId, int year, int month)
    {
        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to   = from.AddMonths(1);

        var txQuery = _db.CashFlowTransactions
            .AsNoTracking()
            .Where(t => t.BusinessId == businessId
                     && t.BranchId   == branchId
                     && t.TransactionDate >= from
                     && t.TransactionDate < to);

        var inTypes  = new[] { CashFlowTransactionType.Sale, CashFlowTransactionType.CashIn, CashFlowTransactionType.OpeningBalance };
        var outTypes = new[] { CashFlowTransactionType.Expense, CashFlowTransactionType.CashOut, CashFlowTransactionType.BankTransfer };

        var totalIn      = await txQuery.Where(t => inTypes.Contains(t.TransactionType)).SumAsync(t => (decimal?)t.Amount) ?? 0;
        var totalOut     = await txQuery.Where(t => outTypes.Contains(t.TransactionType)).SumAsync(t => (decimal?)t.Amount) ?? 0;
        var totalSales   = await txQuery.Where(t => t.TransactionType == CashFlowTransactionType.Sale).SumAsync(t => (decimal?)t.Amount) ?? 0;
        var totalExpenses = await txQuery.Where(t => t.TransactionType == CashFlowTransactionType.Expense).SumAsync(t => (decimal?)t.Amount) ?? 0;

        var branchName = await _db.Branches.AsNoTracking().Where(b => b.Id == branchId).Select(b => b.Name).FirstOrDefaultAsync() ?? "Unknown";

        // Project to a flat row first so EF Core doesn't need to translate
        // complex nested expressions inside GroupBy (avoids SQL translation issues).
        var rawRows = await txQuery
            .Select(t => new
            {
                Day             = t.TransactionDate.Date,
                t.TransactionType,
                t.Amount,
            })
            .ToListAsync();

        var trend = rawRows
            .GroupBy(r => r.Day)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var cashIn  = g.Where(r => inTypes.Contains(r.TransactionType)).Sum(r => r.Amount);
                var cashOut = g.Where(r => outTypes.Contains(r.TransactionType)).Sum(r => r.Amount);
                return new DailyTrendDto
                {
                    Date    = g.Key,
                    CashIn  = cashIn,
                    CashOut = cashOut,
                    Net     = cashIn - cashOut,
                };
            })
            .ToList();

        return new MonthlyCashSummaryDto
        {
            BranchId      = branchId,
            BranchName    = branchName,
            Year          = year,
            Month         = month,
            TotalCashIn   = totalIn,
            TotalCashOut  = totalOut,
            TotalSales    = totalSales,
            TotalExpenses = totalExpenses,
            NetCashFlow   = totalIn - totalOut,
            DailyTrend    = trend,
        };
    }

    public async Task<List<BranchCashSummaryDto>> GetBranchSummariesAsync(int businessId, DateTime date)
    {
        var dateOnly  = date.Date;
        var datePlus1 = dateOnly.AddDays(1);

        var branches = await _db.Branches
            .AsNoTracking()
            .Where(b => b.BusinessId == businessId && b.IsActive && !b.IsDeleted)
            .ToListAsync();

        var registers = await _db.CashRegisters
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId && r.RegisterDate == dateOnly)
            .ToListAsync();

        var txGroups = await _db.CashFlowTransactions
            .AsNoTracking()
            .Where(t => t.BusinessId == businessId
                     && t.TransactionDate >= dateOnly
                     && t.TransactionDate < datePlus1)
            .GroupBy(t => t.BranchId)
            .Select(g => new
            {
                BranchId = g.Key,
                CashIn   = g.Where(t =>
                    t.TransactionType == CashFlowTransactionType.Sale ||
                    t.TransactionType == CashFlowTransactionType.CashIn).Sum(t => (decimal?)t.Amount) ?? 0,
                CashOut  = g.Where(t =>
                    t.TransactionType == CashFlowTransactionType.Expense ||
                    t.TransactionType == CashFlowTransactionType.CashOut ||
                    t.TransactionType == CashFlowTransactionType.BankTransfer).Sum(t => (decimal?)t.Amount) ?? 0,
            })
            .ToListAsync();

        return branches.Select(b =>
        {
            var reg = registers.FirstOrDefault(r => r.BranchId == b.Id);
            var tx  = txGroups.FirstOrDefault(t => t.BranchId == b.Id);
            var opening = reg?.OpeningCash ?? 0;
            var cashIn  = tx?.CashIn ?? 0;
            var cashOut = tx?.CashOut ?? 0;
            return new BranchCashSummaryDto
            {
                BranchId     = b.Id,
                BranchName   = b.Name,
                TodayCashIn  = cashIn,
                TodayCashOut = cashOut,
                NetPosition  = opening + cashIn - cashOut,
                OpeningCash  = opening,
                IsOpenForDay = reg != null,
            };
        }).ToList();
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    public async Task<decimal> GetOpeningCashAsync(int businessId, int branchId, DateTime date)
    {
        var reg = await GetRegisterAsync(businessId, branchId, date);
        return reg?.OpeningCash ?? 0;
    }

    public async Task<decimal> GetTotalByTypeAsync(int businessId, int branchId, DateTime date, CashFlowTransactionType type, CashFlowPaymentMethod? paymentMethod = null)
    {
        var dateOnly = date.Date;
        var q = _db.CashFlowTransactions
            .AsNoTracking()
            .Where(t => t.BusinessId == businessId
                     && t.BranchId   == branchId
                     && t.TransactionDate >= dateOnly
                     && t.TransactionDate < dateOnly.AddDays(1)
                     && t.TransactionType == type);

        if (paymentMethod.HasValue)
            q = q.Where(t => t.PaymentMethod == paymentMethod.Value);

        return await q.SumAsync(t => (decimal?)t.Amount) ?? 0;
    }

    public async Task<List<SaleInvoiceCashFlowDto>> GetCompletedInvoicesMissingCashFlowAsync(int businessId, int branchId, DateTime date)
    {
        var dateOnly  = date.Date;
        var datePlus1 = dateOnly.AddDays(1);

        var recordedQuery = _db.CashFlowTransactions
            .AsNoTracking()
            .Where(t => t.BusinessId == businessId
                     && t.TransactionType == CashFlowTransactionType.Sale
                     && t.ReferenceId != null
                     && t.TransactionDate >= dateOnly
                     && t.TransactionDate < datePlus1);

        if (branchId > 0)
            recordedQuery = recordedQuery.Where(t => t.BranchId == branchId);

        var recordedSaleIds = await recordedQuery
            .Select(t => t.ReferenceId!.Value)
            .Distinct()
            .ToListAsync();

        var invoiceQuery = _db.SaleInvoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId
                     && i.Status == SaleInvoiceStatus.Completed
                     && i.SaleDate >= dateOnly
                     && i.SaleDate < datePlus1
                     && !recordedSaleIds.Contains(i.Id));

        if (branchId > 0)
            invoiceQuery = invoiceQuery.Where(i => i.BranchId == branchId);

        return await invoiceQuery
            .Select(i => new SaleInvoiceCashFlowDto
            {
                Id            = i.Id,
                BranchId      = i.BranchId,
                InvoiceNo     = i.InvoiceNo,
                CashAmount    = i.CashAmount,
                CardAmount    = i.CardAmount,
                PaidAmount    = i.PaidAmount,
                PaymentMethod = i.PaymentMethod,
                SaleDate      = i.SaleDate,
            })
            .ToListAsync();
    }

    public async Task<List<ExpenseCashFlowDto>> GetExpensesMissingCashFlowAsync(int businessId, int branchId, DateTime date)
    {
        var dateOnly  = date.Date;
        var datePlus1 = dateOnly.AddDays(1);

        var recordedQuery = _db.CashFlowTransactions
            .AsNoTracking()
            .Where(t => t.BusinessId == businessId
                     && t.TransactionType == CashFlowTransactionType.Expense
                     && t.ReferenceId != null
                     && t.TransactionDate >= dateOnly
                     && t.TransactionDate < datePlus1);

        if (branchId > 0)
            recordedQuery = recordedQuery.Where(t => t.BranchId == branchId);

        var recordedExpenseIds = await recordedQuery
            .Select(t => t.ReferenceId!.Value)
            .Distinct()
            .ToListAsync();

        var expenseQuery = _db.Expenses
            .AsNoTracking()
            .Include(e => e.ExpenseCategory)
            .Where(e => e.BusinessId == businessId
                     && !e.IsDeleted
                     && e.ExpenseDate >= dateOnly
                     && e.ExpenseDate < datePlus1
                     && !recordedExpenseIds.Contains(e.Id));

        if (branchId > 0)
            expenseQuery = expenseQuery.Where(e => e.BranchId == branchId);

        return await expenseQuery
            .Select(e => new ExpenseCashFlowDto
            {
                Id            = e.Id,
                BranchId      = e.BranchId,
                Description   = e.Description,
                CategoryName  = e.ExpenseCategory.Name,
                Amount        = e.Amount,
                PaymentMethod = e.PaymentMethod,
                ExpenseDate   = e.ExpenseDate,
            })
            .ToListAsync();
    }
}
