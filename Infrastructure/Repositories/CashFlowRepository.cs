using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Application.CashFlow.DTOs;
using POSSystem.Application.CashFlow.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

/// <summary>
/// Cash register operations; financial movements read from GL Transactions.
/// </summary>
public class CashFlowRepository : ICashFlowRepository
{
    private readonly POSDbContext _db;
    private readonly IGlReportingRepository _glReporting;

    public CashFlowRepository(POSDbContext db, IGlReportingRepository glReporting)
    {
        _db = db;
        _glReporting = glReporting;
    }

    public async Task<CashRegister?> GetRegisterAsync(int businessId, int branchId, DateTime date)
    {
        var dateOnly = date.Date;
        return await _db.CashRegisters
            .Include(r => r.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.BusinessId == businessId
                && r.BranchId == branchId
                && r.RegisterDate == dateOnly);
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

    public async Task<DailyCashSummaryDto> GetDailySummaryAsync(int businessId, int branchId, DateTime date)
    {
        var dateOnly = date.Date;
        var register = await GetRegisterAsync(businessId, branchId, dateOnly);
        var gl = await _glReporting.GetGlCashDaySummaryAsync(branchId, dateOnly);

        var branchName = register?.Branch?.Name
            ?? await _db.Branches.AsNoTracking().Where(b => b.Id == branchId).Select(b => b.Name).FirstOrDefaultAsync()
            ?? "Unknown";

        var opening = register?.OpeningCash ?? 0;
        var expected = opening + gl.NetMovement;

        return new DailyCashSummaryDto
        {
            BranchId = branchId,
            BranchName = branchName,
            Date = dateOnly,
            OpeningCash = opening,
            TotalCashSales = gl.CashSales,
            TotalCardSales = gl.CardSales,
            TotalExpensesCash = gl.Expenses,
            TotalCashIn = gl.CashIn,
            TotalCashOut = gl.CashOut,
            TotalBankTransfers = 0,
            ExpectedClosingCash = expected,
            ActualClosingCash = register?.ActualCash,
            Difference = register?.Difference,
            IsRegistered = register != null,
            IsClosed = register?.IsClosed ?? false,
        };
    }

    public async Task<MonthlyCashSummaryDto> GetMonthlySummaryAsync(int businessId, int branchId, int year, int month)
    {
        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1);

        var trendRows = await _glReporting.GetDailyCashMovementsAsync(branchId, from, to.AddDays(-1));
        var trend = trendRows.Select(r => new DailyTrendDto
        {
            Date = r.Date,
            CashIn = r.CashIn,
            CashOut = r.CashOut,
            Net = r.CashIn - r.CashOut,
        }).ToList();

        var totalIn = trend.Sum(t => t.CashIn);
        var totalOut = trend.Sum(t => t.CashOut);

        // `to` is the first day of the next month; use last day of selected month for GL period filters.
        var periodEnd = to.AddDays(-1);
        var salesNet = await _glReporting.GetAccountTypeNetAsync(branchId, AccountType.Income, from, periodEnd);
        var expenseNet = await _glReporting.GetAccountTypeNetAsync(branchId, AccountType.Expense, from, periodEnd);

        var branchName = await _db.Branches.AsNoTracking()
            .Where(b => b.Id == branchId)
            .Select(b => b.Name)
            .FirstOrDefaultAsync() ?? "Unknown";

        return new MonthlyCashSummaryDto
        {
            BranchId = branchId,
            BranchName = branchName,
            Year = year,
            Month = month,
            TotalCashIn = totalIn,
            TotalCashOut = totalOut,
            TotalSales = salesNet < 0 ? -salesNet : salesNet,
            TotalExpenses = expenseNet,
            NetCashFlow = totalIn - totalOut,
            DailyTrend = trend,
        };
    }

    public async Task<List<BranchCashSummaryDto>> GetBranchSummariesAsync(int businessId, DateTime date)
    {
        var dateOnly = date.Date;
        var nextDay = dateOnly.AddDays(1);
        var now = DateTime.UtcNow.AddSeconds(1);

        var branches = await _db.Branches.AsNoTracking()
            .Where(b => b.BusinessId == businessId && b.IsActive && !b.IsDeleted)
            .Select(b => new { b.Id, b.Name })
            .ToListAsync();

        // Register sessions relevant to "today": currently open (may span days) or opened/dated today.
        var sessions = await _db.RegisterSessions.AsNoTracking()
            .Where(s => !s.IsDeleted && s.BusinessId == businessId)
            .Where(s => !s.IsClosed
                        || (s.OpenedAt >= dateOnly && s.OpenedAt < nextDay)
                        || s.SessionDate == dateOnly)
            .Join(
                _db.PosRegisters.AsNoTracking(),
                s => s.PosRegisterId,
                r => r.Id,
                (s, r) => new
                {
                    s.BranchId,
                    s.OpeningBalance,
                    s.IsClosed,
                    s.OpenedAt,
                    r.LinkedCashAccountId,
                })
            .ToListAsync();

        var results = new List<BranchCashSummaryDto>(branches.Count);
        foreach (var branch in branches)
        {
            var branchSessions = sessions.Where(s => s.BranchId == branch.Id).ToList();
            var openSessions = branchSessions.Where(s => !s.IsClosed).ToList();

            decimal opening = 0, cashIn = 0, cashOut = 0;
            foreach (var s in openSessions)
            {
                // Session-scoped GL (CreatedAt based) so it matches the register dashboard exactly.
                var gl = await _glReporting.GetGlCashAccountSessionSummaryAsync(
                    s.LinkedCashAccountId, branch.Id, s.OpenedAt, now);
                opening += s.OpeningBalance;
                cashIn += gl.CashIn + gl.CashSales;
                cashOut += gl.CashOut + gl.Expenses;
            }

            var status = openSessions.Count > 0
                ? "Open"
                : branchSessions.Count > 0 ? "Closed" : "NotStarted";

            results.Add(new BranchCashSummaryDto
            {
                BranchId = branch.Id,
                BranchName = branch.Name,
                OpeningCash = opening,
                TodayCashIn = cashIn,
                TodayCashOut = cashOut,
                NetPosition = opening + cashIn - cashOut,
                IsOpenForDay = openSessions.Count > 0,
                Status = status,
            });
        }

        return results;
    }

    public async Task<JournalVoucher> AddJournalVoucherAsync(JournalVoucher voucher)
    {
        _db.JournalVouchers.Add(voucher);
        await _db.SaveChangesAsync();
        return voucher;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public async Task<(IReadOnlyList<JournalVoucher> Items, int Total)> ListJournalVouchersAsync(
        int businessId,
        int branchId,
        DateTime? fromDate,
        DateTime? toDate,
        CashFlowTransactionType? transactionType,
        int page,
        int pageSize)
    {
        var query = _db.JournalVouchers
            .AsNoTracking()
            .Where(v => !v.IsDeleted && v.BusinessId == businessId && v.BranchId == branchId);

        if (fromDate.HasValue)
            query = query.Where(v => v.VoucherDate >= fromDate.Value.Date);

        if (toDate.HasValue)
        {
            var end = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(v => v.VoucherDate <= end);
        }

        if (transactionType.HasValue)
            query = query.Where(v => v.TransactionType == transactionType.Value);

        var total = await query.CountAsync();
        var pageNumber = page < 1 ? 1 : page;
        var size = pageSize < 1 ? 25 : pageSize;

        var items = await query
            .OrderByDescending(v => v.VoucherDate)
            .ThenByDescending(v => v.Id)
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }
}
