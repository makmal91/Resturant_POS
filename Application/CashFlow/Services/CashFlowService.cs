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

    public Task<PagedResultDto<CashFlowTransactionDto>> GetLedgerAsync(CashFlowLedgerFilterDto filter)
        => _repo.GetLedgerPagedAsync(filter);

    // ─── Summaries ─────────────────────────────────────────────────────────────

    public Task<DailyCashSummaryDto> GetDailySummaryAsync(int businessId, int branchId, DateTime? date = null)
        => _repo.GetDailySummaryAsync(businessId, branchId, (date ?? DateTime.UtcNow).Date);

    public Task<MonthlyCashSummaryDto> GetMonthlySummaryAsync(int businessId, int branchId, int? year = null, int? month = null)
    {
        var now = DateTime.UtcNow;
        return _repo.GetMonthlySummaryAsync(businessId, branchId, year ?? now.Year, month ?? now.Month);
    }

    public Task<List<BranchCashSummaryDto>> GetAllBranchesSummaryAsync(int businessId, DateTime? date = null)
        => _repo.GetBranchSummariesAsync(businessId, (date ?? DateTime.UtcNow).Date);

    // ─── Integration ───────────────────────────────────────────────────────────

    public async Task RecordSaleAsync(int businessId, int branchId, int saleId, string invoiceNo, decimal cashAmount, decimal cardAmount)
    {
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
                TransactionDate = DateTime.UtcNow,
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
                TransactionDate = DateTime.UtcNow,
            });
        }
    }

    public async Task RecordExpenseAsync(int businessId, int branchId, int expenseId, string description, decimal amount, CashFlowPaymentMethod paymentMethod)
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
            TransactionDate = DateTime.UtcNow,
        });
    }

    // ─── Mapping helpers ───────────────────────────────────────────────────────

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
