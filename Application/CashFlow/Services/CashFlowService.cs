using POSSystem.Application.CashFlow.DTOs;
using POSSystem.Application.CashFlow.Interfaces;
using POSSystem.Application.Common.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.CashFlow.Services;

public class CashFlowService : ICashFlowService
{
    private readonly ICashFlowRepository _repo;

    public CashFlowService(ICashFlowRepository repo) => _repo = repo;

    // ─── Daily register ────────────────────────────────────────────────────────

    public async Task<CashRegisterDto> OpenCashAsync(OpeningCashDto dto)
    {
        var date = (dto.Date ?? DateTime.UtcNow).Date;

        var existing = await _repo.GetRegisterAsync(dto.BusinessId, dto.BranchId, date);
        if (existing != null)
            throw new InvalidOperationException($"Cash register already opened for {date:yyyy-MM-dd}.");

        var register = new CashRegister
        {
            BusinessId   = dto.BusinessId,
            BranchId     = dto.BranchId,
            RegisterDate = date,
            OpeningCash  = dto.Amount,
            Notes        = dto.Notes,
        };

        await _repo.AddRegisterAsync(register);

        // Record as an OpeningBalance transaction so the ledger is complete
        await _repo.AddTransactionAsync(new CashFlowTransaction
        {
            BusinessId       = dto.BusinessId,
            BranchId         = dto.BranchId,
            TransactionType  = CashFlowTransactionType.OpeningBalance,
            PaymentMethod    = CashFlowPaymentMethod.Cash,
            Amount           = dto.Amount,
            Description      = "Opening cash balance",
            TransactionDate  = date,
        });

        return MapRegister(register, string.Empty);
    }

    public async Task<CashRegisterDto> CloseCashAsync(ClosingCashDto dto)
    {
        var date = (dto.Date ?? DateTime.UtcNow).Date;

        var register = await _repo.GetRegisterAsync(dto.BusinessId, dto.BranchId, date)
            ?? throw new InvalidOperationException("No open cash register found for today. Please open the cash register first.");

        if (register.IsClosed)
            throw new InvalidOperationException("Cash register is already closed for today.");

        var summary = await _repo.GetDailySummaryAsync(dto.BusinessId, dto.BranchId, date);
        var expected = summary.ExpectedClosingCash;
        var actual   = dto.ActualCash;
        var diff     = actual - expected;

        // Reload the tracked entity for update
        var tracked = new CashRegister
        {
            Id           = register.Id,
            BusinessId   = register.BusinessId,
            BranchId     = register.BranchId,
            RegisterDate = register.RegisterDate,
            OpeningCash  = register.OpeningCash,
            ClosingCash  = actual,
            ExpectedCash = expected,
            ActualCash   = actual,
            Difference   = diff,
            IsClosed     = true,
            Notes        = dto.Notes ?? register.Notes,
            ClosedAt     = DateTime.UtcNow,
        };

        await _repo.UpdateRegisterAsync(tracked);

        // Record closing balance entry
        await _repo.AddTransactionAsync(new CashFlowTransaction
        {
            BusinessId      = dto.BusinessId,
            BranchId        = dto.BranchId,
            TransactionType = CashFlowTransactionType.ClosingBalance,
            PaymentMethod   = CashFlowPaymentMethod.Cash,
            Amount          = actual,
            Description     = $"Closing cash — expected: {expected:F2}, actual: {actual:F2}, diff: {diff:+0.00;-0.00;0}",
            TransactionDate = date,
        });

        return MapRegister(tracked, string.Empty);
    }

    public async Task<CashRegisterDto?> GetTodayRegisterAsync(int businessId, int branchId)
    {
        var reg = await _repo.GetRegisterAsync(businessId, branchId, DateTime.UtcNow.Date);
        return reg == null ? null : MapRegister(reg, string.Empty);
    }

    // ─── Manual transactions ───────────────────────────────────────────────────

    public async Task<CashFlowTransactionDto> RecordTransactionAsync(RecordCashTransactionDto dto)
    {
        if (dto.Amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero.");

        var tx = new CashFlowTransaction
        {
            BusinessId      = dto.BusinessId,
            BranchId        = dto.BranchId,
            TransactionType = dto.TransactionType,
            PaymentMethod   = dto.PaymentMethod,
            Amount          = dto.Amount,
            ReferenceId     = dto.ReferenceId,
            ReferenceNo     = dto.ReferenceNo,
            Description     = dto.Description,
            TransactionDate = dto.TransactionDate ?? DateTime.UtcNow,
        };

        await _repo.AddTransactionAsync(tx);

        return new CashFlowTransactionDto
        {
            Id              = tx.Id,
            BranchId        = tx.BranchId,
            TransactionType = tx.TransactionType.ToString(),
            PaymentMethod   = tx.PaymentMethod.ToString(),
            Amount          = tx.Amount,
            ReferenceNo     = tx.ReferenceNo,
            Description     = tx.Description,
            TransactionDate = tx.TransactionDate,
            CreatedAt       = tx.CreatedAt,
        };
    }

    public async Task<CashFlowLedgerPageDto> GetLedgerAsync(CashFlowLedgerFilterDto filter)
    {
        await SyncLedgerRangeAsync(filter.BusinessId, filter.BranchId, filter.FromDate, filter.ToDate);
        return await _repo.GetLedgerPagedAsync(filter);
    }

    // ─── Summaries ─────────────────────────────────────────────────────────────

    public async Task<DailyCashSummaryDto> GetDailySummaryAsync(int businessId, int branchId, DateTime? date = null)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;
        await SyncDayAsync(businessId, branchId, targetDate);
        return await _repo.GetDailySummaryAsync(businessId, branchId, targetDate);
    }

    public Task<MonthlyCashSummaryDto> GetMonthlySummaryAsync(int businessId, int branchId, int? year = null, int? month = null)
    {
        var now = DateTime.UtcNow;
        return _repo.GetMonthlySummaryAsync(businessId, branchId, year ?? now.Year, month ?? now.Month);
    }

    public async Task<List<BranchCashSummaryDto>> GetAllBranchesSummaryAsync(int businessId, DateTime? date = null)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;
        await SyncDayAsync(businessId, 0, targetDate);
        return await _repo.GetBranchSummariesAsync(businessId, targetDate);
    }

    // ─── Integration ───────────────────────────────────────────────────────────

    public async Task RecordSaleAsync(int businessId, int branchId, int saleId, string invoiceNo, decimal cashAmount, decimal cardAmount, DateTime? transactionDate = null)
    {
        var txDate = transactionDate ?? DateTime.UtcNow;

        if (cashAmount > 0)
        {
            await _repo.AddTransactionAsync(new CashFlowTransaction
            {
                BusinessId      = businessId,
                BranchId        = branchId,
                TransactionType = CashFlowTransactionType.Sale,
                PaymentMethod   = CashFlowPaymentMethod.Cash,
                Amount          = cashAmount,
                ReferenceId     = saleId,
                ReferenceNo     = invoiceNo,
                Description     = $"Cash sale — {invoiceNo}",
                TransactionDate = txDate,
            });
        }

        if (cardAmount > 0)
        {
            await _repo.AddTransactionAsync(new CashFlowTransaction
            {
                BusinessId      = businessId,
                BranchId        = branchId,
                TransactionType = CashFlowTransactionType.Sale,
                PaymentMethod   = CashFlowPaymentMethod.Bank,
                Amount          = cardAmount,
                ReferenceId     = saleId,
                ReferenceNo     = invoiceNo,
                Description     = $"Card sale — {invoiceNo}",
                TransactionDate = txDate,
            });
        }
    }

    public async Task ReverseSaleAsync(int businessId, int branchId, int saleId, string invoiceNo, decimal cashAmount, decimal cardAmount, DateTime? transactionDate = null, string? reason = null)
    {
        var txDate = transactionDate ?? DateTime.UtcNow;
        var suffix = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" | {reason}";

        if (cashAmount > 0)
        {
            await _repo.AddTransactionAsync(new CashFlowTransaction
            {
                BusinessId      = businessId,
                BranchId        = branchId,
                TransactionType = CashFlowTransactionType.Sale,
                PaymentMethod   = CashFlowPaymentMethod.Cash,
                Amount          = -cashAmount,
                ReferenceId     = saleId,
                ReferenceNo     = invoiceNo,
                Description     = $"Sale reversal (cash) — {invoiceNo}{suffix}",
                TransactionDate = txDate,
            });
        }

        if (cardAmount > 0)
        {
            await _repo.AddTransactionAsync(new CashFlowTransaction
            {
                BusinessId      = businessId,
                BranchId        = branchId,
                TransactionType = CashFlowTransactionType.Sale,
                PaymentMethod   = CashFlowPaymentMethod.Bank,
                Amount          = -cardAmount,
                ReferenceId     = saleId,
                ReferenceNo     = invoiceNo,
                Description     = $"Sale reversal (card) — {invoiceNo}{suffix}",
                TransactionDate = txDate,
            });
        }
    }

    public async Task RecordExpenseAsync(int businessId, int branchId, int expenseId, string description, decimal amount, CashFlowPaymentMethod paymentMethod, DateTime? transactionDate = null)
    {
        if (amount <= 0) return;

        await _repo.AddTransactionAsync(new CashFlowTransaction
        {
            BusinessId      = businessId,
            BranchId        = branchId,
            TransactionType = CashFlowTransactionType.Expense,
            PaymentMethod   = paymentMethod,
            Amount          = amount,
            ReferenceId     = expenseId,
            Description     = description,
            TransactionDate = transactionDate ?? DateTime.UtcNow,
        });
    }

    public async Task ReverseExpenseAsync(int businessId, int branchId, int expenseId, string description, decimal amount, CashFlowPaymentMethod paymentMethod, DateTime? transactionDate = null, string? reason = null)
    {
        if (amount <= 0) return;

        var suffix = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" | {reason}";

        await _repo.AddTransactionAsync(new CashFlowTransaction
        {
            BusinessId      = businessId,
            BranchId        = branchId,
            TransactionType = CashFlowTransactionType.Expense,
            PaymentMethod   = paymentMethod,
            Amount          = -amount,
            ReferenceId     = expenseId,
            Description     = $"Expense reversal — {description}{suffix}",
            TransactionDate = transactionDate ?? DateTime.UtcNow,
        });
    }

    public async Task RecordCustomerPaymentAsync(
        int businessId, int branchId, int paymentId, string? referenceNo, string description,
        decimal amount, PartyPaymentType paymentType, DateTime? transactionDate = null)
    {
        if (amount <= 0) return;

        await _repo.AddTransactionAsync(new CashFlowTransaction
        {
            BusinessId      = businessId,
            BranchId        = branchId,
            TransactionType = CashFlowTransactionType.CashIn,
            PaymentMethod   = MapPartyPaymentType(paymentType),
            Amount          = amount,
            ReferenceId     = paymentId,
            ReferenceNo     = referenceNo,
            Description     = description,
            TransactionDate = transactionDate ?? DateTime.UtcNow,
        });
    }

    public async Task RecordSupplierPaymentAsync(
        int businessId, int branchId, int paymentId, string? referenceNo, string description,
        decimal amount, PartyPaymentType paymentType, DateTime? transactionDate = null)
    {
        if (amount <= 0) return;

        await _repo.AddTransactionAsync(new CashFlowTransaction
        {
            BusinessId      = businessId,
            BranchId        = branchId,
            TransactionType = CashFlowTransactionType.CashOut,
            PaymentMethod   = MapPartyPaymentType(paymentType),
            Amount          = amount,
            ReferenceId     = paymentId,
            ReferenceNo     = referenceNo,
            Description     = description,
            TransactionDate = transactionDate ?? DateTime.UtcNow,
        });
    }

    // ─── Mapping helpers ───────────────────────────────────────────────────────

    private async Task SyncLedgerRangeAsync(int businessId, int branchId, DateTime? fromDate, DateTime? toDate)
    {
        var start = (fromDate ?? DateTime.UtcNow).Date;
        var end = (toDate ?? start).Date;
        if (end < start)
            (start, end) = (end, start);

        for (var day = start; day <= end; day = day.AddDays(1))
            await SyncDayAsync(businessId, branchId, day);
    }

    private async Task SyncDayAsync(int businessId, int branchId, DateTime date)
    {
        await _repo.RemoveCreditSaleCashFlowAsync(businessId, branchId, date);
        await SyncMissingSalesAsync(businessId, branchId, date);
        await SyncMissingExpensesAsync(businessId, branchId, date);
        await SyncMissingPartyPaymentsAsync(businessId, branchId, date);
    }

    private async Task SyncMissingPartyPaymentsAsync(int businessId, int branchId, DateTime date)
    {
        var missing = await _repo.GetPartyPaymentsMissingCashFlowAsync(businessId, branchId, date);
        foreach (var payment in missing)
        {
            if (payment.Amount <= 0) continue;

            var referenceNo = string.IsNullOrWhiteSpace(payment.ReferenceNo)
                ? payment.InvoiceNo
                : payment.ReferenceNo;

            if (payment.Module == InvoicePaymentModule.Sale)
            {
                var description = string.IsNullOrWhiteSpace(payment.InvoiceNo)
                    ? $"Customer payment received — {payment.PartyName ?? "Customer"}"
                    : $"Customer payment — {payment.InvoiceNo}";

                if (!string.IsNullOrWhiteSpace(payment.Notes))
                    description = $"{description} | {payment.Notes}";

                await RecordCustomerPaymentAsync(
                    businessId, payment.BranchId, payment.Id, referenceNo, description,
                    payment.Amount, payment.PaymentType, payment.PaymentDate);
            }
            else
            {
                var description = string.IsNullOrWhiteSpace(payment.InvoiceNo)
                    ? $"Supplier payment — {payment.PartyName ?? "Supplier"}"
                    : $"Supplier payment — {payment.InvoiceNo}";

                if (!string.IsNullOrWhiteSpace(payment.Notes))
                    description = $"{description} | {payment.Notes}";

                await RecordSupplierPaymentAsync(
                    businessId, payment.BranchId, payment.Id, referenceNo, description,
                    payment.Amount, payment.PaymentType, payment.PaymentDate);
            }
        }
    }

    private async Task SyncMissingSalesAsync(int businessId, int branchId, DateTime date)
    {
        var missing = await _repo.GetCompletedInvoicesMissingCashFlowAsync(businessId, branchId, date);
        foreach (var inv in missing)
        {
            var (cash, card) = ResolvePaymentAmounts(inv);
            if (cash <= 0 && card <= 0) continue;
            await RecordSaleAsync(businessId, inv.BranchId, inv.Id, inv.InvoiceNo, cash, card, inv.SaleDate);
        }
    }

    private async Task SyncMissingExpensesAsync(int businessId, int branchId, DateTime date)
    {
        var missing = await _repo.GetExpensesMissingCashFlowAsync(businessId, branchId, date);
        foreach (var exp in missing)
        {
            if (exp.Amount <= 0) continue;
            var description = string.IsNullOrWhiteSpace(exp.CategoryName)
                ? exp.Description
                : $"{exp.CategoryName}: {exp.Description}";
            await RecordExpenseAsync(
                businessId,
                exp.BranchId,
                exp.Id,
                description,
                exp.Amount,
                MapExpensePaymentMethod(exp.PaymentMethod),
                exp.ExpenseDate);
        }
    }

    private static CashFlowPaymentMethod MapExpensePaymentMethod(ExpensePaymentMethod method) => method switch
    {
        ExpensePaymentMethod.Bank   => CashFlowPaymentMethod.Bank,
        ExpensePaymentMethod.Wallet => CashFlowPaymentMethod.Wallet,
        _                           => CashFlowPaymentMethod.Cash,
    };

    private static CashFlowPaymentMethod MapPartyPaymentType(PartyPaymentType type) => type switch
    {
        PartyPaymentType.Bank   => CashFlowPaymentMethod.Bank,
        PartyPaymentType.Online => CashFlowPaymentMethod.Wallet,
        _                       => CashFlowPaymentMethod.Cash,
    };

    private static (decimal Cash, decimal Card) ResolvePaymentAmounts(SaleInvoiceCashFlowDto inv)
    {
        var cash = inv.CashAmount;
        var card = inv.CardAmount;

        if (cash <= 0 && card <= 0 && inv.PaidAmount > 0)
        {
            switch (inv.PaymentMethod)
            {
                case SalePaymentMethod.Cash:
                    cash = inv.PaidAmount;
                    break;
                case SalePaymentMethod.Card:
                    card = inv.PaidAmount;
                    break;
                case SalePaymentMethod.Mixed when inv.CashAmount > 0 || inv.CardAmount > 0:
                    cash = inv.CashAmount;
                    card = inv.CardAmount;
                    break;
                case SalePaymentMethod.Mixed:
                    cash = inv.PaidAmount;
                    break;
            }
        }

        return (cash, card);
    }

    private static CashRegisterDto MapRegister(CashRegister r, string branchName) => new()
    {
        Id           = r.Id,
        BranchId     = r.BranchId,
        BranchName   = branchName,
        RegisterDate = r.RegisterDate,
        OpeningCash  = r.OpeningCash,
        ClosingCash  = r.ClosingCash,
        ExpectedCash = r.ExpectedCash,
        ActualCash   = r.ActualCash,
        Difference   = r.Difference,
        IsClosed     = r.IsClosed,
        Notes        = r.Notes,
    };
}
