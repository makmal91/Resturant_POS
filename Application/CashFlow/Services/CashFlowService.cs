using POSSystem.Application.Accounting.DTOs;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Application.Accounting.Services;
using POSSystem.Application.CashFlow.DTOs;
using POSSystem.Application.CashFlow.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.CashFlow.Services;

/// <summary>
/// Cash register UI; all financial movements are stored in GL Transactions.
/// </summary>
public class CashFlowService : ICashFlowService
{
    private readonly ICashFlowRepository _repo;
    private readonly IAccountLedgerService _accountLedger;
    private readonly IAccountingService _accounting;
    private readonly IGlAccountRepository _glAccounts;
    private readonly ICodeGeneratorService _codeGenerator;

    public CashFlowService(
        ICashFlowRepository repo,
        IAccountLedgerService accountLedger,
        IAccountingService accounting,
        IGlAccountRepository glAccounts,
        ICodeGeneratorService codeGenerator)
    {
        _repo = repo;
        _accountLedger = accountLedger;
        _accounting = accounting;
        _glAccounts = glAccounts;
        _codeGenerator = codeGenerator;
    }

    public async Task<CashRegisterDto> OpenCashAsync(OpeningCashDto dto)
    {
        var date = (dto.Date ?? DateTime.UtcNow).Date;

        var existing = await _repo.GetRegisterAsync(dto.BusinessId, dto.BranchId, date);
        if (existing != null)
            throw new InvalidOperationException($"Cash register already opened for {date:yyyy-MM-dd}.");

        var register = new CashRegister
        {
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId,
            RegisterDate = date,
            OpeningCash = dto.Amount,
            Notes = dto.Notes,
        };

        await _repo.AddRegisterAsync(register);
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
        var actual = dto.ActualCash;
        var diff = actual - expected;

        var tracked = new CashRegister
        {
            Id = register.Id,
            BusinessId = register.BusinessId,
            BranchId = register.BranchId,
            RegisterDate = register.RegisterDate,
            OpeningCash = register.OpeningCash,
            ClosingCash = actual,
            ExpectedCash = expected,
            ActualCash = actual,
            Difference = diff,
            IsClosed = true,
            Notes = dto.Notes ?? register.Notes,
            ClosedAt = DateTime.UtcNow,
        };

        await _repo.UpdateRegisterAsync(tracked);
        return MapRegister(tracked, string.Empty);
    }

    public async Task<CashRegisterDto?> GetTodayRegisterAsync(int businessId, int branchId)
    {
        var reg = await _repo.GetRegisterAsync(businessId, branchId, DateTime.UtcNow.Date);
        return reg == null ? null : MapRegister(reg, string.Empty);
    }

    public async Task<CashFlowTransactionDto> RecordTransactionAsync(RecordCashTransactionDto dto)
    {
        if (dto.Amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero.");

        if (dto.TransactionType is not (CashFlowTransactionType.CashIn or CashFlowTransactionType.CashOut))
            throw new InvalidOperationException($"Manual GL posting is not supported for {dto.TransactionType}.");

        var voucherNo = await _codeGenerator.GenerateAsync(CodeModuleNames.JournalVoucher, dto.BranchId);
        var accounts = await _glAccounts.ResolvePostingAccountsAsync();
        var cashAccountId = dto.PaymentMethod == CashFlowPaymentMethod.Bank ? accounts.Bank : accounts.Cash;
        var groupId = Guid.NewGuid();
        var date = dto.TransactionDate ?? DateTime.UtcNow;
        var userDescription = dto.Description?.Trim();
        var description = string.IsNullOrWhiteSpace(userDescription)
            ? $"{voucherNo} — {dto.TransactionType}"
            : $"{voucherNo} — {userDescription}";

        var voucher = new JournalVoucher
        {
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId,
            VoucherNo = voucherNo,
            TransactionType = dto.TransactionType,
            PaymentMethod = dto.PaymentMethod,
            Amount = dto.Amount,
            Description = userDescription,
            VoucherDate = date,
            GlGroupId = groupId,
        };

        await _repo.AddJournalVoucherAsync(voucher);
        await _repo.SaveChangesAsync();

        var entries = new List<AccountingTransactionDto>();

        switch (dto.TransactionType)
        {
            case CashFlowTransactionType.CashIn:
                entries.Add(_accounting.CreateEntry(cashAccountId, dto.BranchId, dto.Amount, 0, groupId, voucher.Id, description));
                entries.Add(_accounting.CreateEntry(accounts.Sales, dto.BranchId, 0, dto.Amount, groupId, voucher.Id, description));
                break;
            case CashFlowTransactionType.CashOut:
                entries.Add(_accounting.CreateEntry(accounts.GeneralExpense, dto.BranchId, dto.Amount, 0, groupId, voucher.Id, description));
                entries.Add(_accounting.CreateEntry(cashAccountId, dto.BranchId, 0, dto.Amount, groupId, voucher.Id, description));
                break;
        }

        foreach (var entry in entries)
        {
            entry.TransactionType = GlTransactionType.Adjustment;
            entry.Date = date;
        }

        await _accounting.CreateDoubleEntryAsync(entries);
        await _repo.SaveChangesAsync();

        return MapJournalVoucherToCashFlowDto(voucher);
    }

    public async Task<JournalVoucherListPageDto> ListJournalVouchersAsync(JournalVoucherListFilterDto filter)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 25 : filter.PageSize;

        var (items, total) = await _repo.ListJournalVouchersAsync(
            filter.BusinessId,
            filter.BranchId,
            filter.FromDate,
            filter.ToDate,
            filter.TransactionType,
            page,
            pageSize);

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

        return new JournalVoucherListPageDto
        {
            Vouchers = items.Select(MapJournalVoucher).ToList(),
            TotalRecords = total,
            TotalPages = totalPages,
            CurrentPage = page,
            PageSize = pageSize,
        };
    }

    public async Task<CashFlowLedgerPageDto> GetLedgerAsync(CashFlowLedgerFilterDto filter)
    {
        var accounts = await _glAccounts.ResolvePostingAccountsAsync();
        var ledger = await _accountLedger.GetAccountLedgerAsync(new AccountLedgerFilterDto
        {
            AccountId = accounts.Cash,
            BusinessId = filter.BusinessId,
            BranchId = filter.BranchId,
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            Page = filter.Page,
            PageSize = filter.PageSize,
        });

        return AccountLedgerMapper.ToCashFlowLedgerPage(ledger, filter.BranchId);
    }

    public Task<DailyCashSummaryDto> GetDailySummaryAsync(int businessId, int branchId, DateTime? date = null) =>
        _repo.GetDailySummaryAsync(businessId, branchId, (date ?? DateTime.UtcNow).Date);

    public Task<MonthlyCashSummaryDto> GetMonthlySummaryAsync(int businessId, int branchId, int? year = null, int? month = null)
    {
        var now = DateTime.UtcNow;
        return _repo.GetMonthlySummaryAsync(businessId, branchId, year ?? now.Year, month ?? now.Month);
    }

    public Task<List<BranchCashSummaryDto>> GetAllBranchesSummaryAsync(int businessId, DateTime? date = null) =>
        _repo.GetBranchSummariesAsync(businessId, (date ?? DateTime.UtcNow).Date);

    private static CashRegisterDto MapRegister(CashRegister r, string branchName) => new()
    {
        Id = r.Id,
        BranchId = r.BranchId,
        BranchName = branchName,
        RegisterDate = r.RegisterDate,
        OpeningCash = r.OpeningCash,
        ClosingCash = r.ClosingCash,
        ExpectedCash = r.ExpectedCash,
        ActualCash = r.ActualCash,
        Difference = r.Difference,
        IsClosed = r.IsClosed,
        Notes = r.Notes,
    };

    private static JournalVoucherDto MapJournalVoucher(JournalVoucher voucher) => new()
    {
        Id = voucher.Id,
        BranchId = voucher.BranchId,
        VoucherNo = voucher.VoucherNo,
        TransactionType = voucher.TransactionType.ToString(),
        PaymentMethod = voucher.PaymentMethod.ToString(),
        Amount = voucher.Amount,
        Description = voucher.Description,
        VoucherDate = voucher.VoucherDate,
        CreatedAt = voucher.CreatedAt,
    };

    private static CashFlowTransactionDto MapJournalVoucherToCashFlowDto(JournalVoucher voucher) => new()
    {
        Id = voucher.Id,
        BranchId = voucher.BranchId,
        TransactionType = voucher.TransactionType.ToString(),
        PaymentMethod = voucher.PaymentMethod.ToString(),
        Amount = voucher.Amount,
        ReferenceNo = voucher.VoucherNo,
        Description = voucher.Description,
        TransactionDate = voucher.VoucherDate,
        CreatedAt = voucher.CreatedAt,
    };
}
