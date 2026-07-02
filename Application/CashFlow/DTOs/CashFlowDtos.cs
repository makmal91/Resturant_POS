using POSSystem.Domain;

namespace POSSystem.Application.CashFlow.DTOs;

// ─── Shared ───────────────────────────────────────────────────────────────────

public class CashFlowTransactionDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    /// <summary>Contra account name(s) from the same journal entry.</summary>
    public string AccountName { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public decimal RunningBalance { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public bool IsInflow { get; set; }
    public decimal DisplayAmount { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ─── Commands ─────────────────────────────────────────────────────────────────

public class RecordCashTransactionDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public CashFlowTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public CashFlowPaymentMethod PaymentMethod { get; set; } = CashFlowPaymentMethod.Cash;
    public int? ReferenceId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    public DateTime? TransactionDate { get; set; }
}

public class OpeningCashDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public decimal Amount { get; set; }
    public DateTime? Date { get; set; }
    public string? Notes { get; set; }
}

public class ClosingCashDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public decimal ActualCash { get; set; }
    public DateTime? Date { get; set; }
    public string? Notes { get; set; }
}

// ─── Queries / Filters ────────────────────────────────────────────────────────

public class CashFlowLedgerFilterDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public CashFlowTransactionType? TransactionType { get; set; }
    public CashFlowPaymentMethod? PaymentMethod { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class CashFlowLedgerPageDto
{
    public string AccountName { get; set; } = string.Empty;
    public IReadOnlyList<CashFlowTransactionDto> Transactions { get; set; } = Array.Empty<CashFlowTransactionDto>();
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public decimal NetTotal { get; set; }
    public decimal PeriodOpeningBalance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
}

public class JournalVoucherDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string VoucherNo { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime VoucherDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class JournalVoucherListFilterDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public CashFlowTransactionType? TransactionType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class JournalVoucherListPageDto
{
    public IReadOnlyList<JournalVoucherDto> Vouchers { get; set; } = Array.Empty<JournalVoucherDto>();
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

// ─── Response DTOs ────────────────────────────────────────────────────────────

public class DailyCashSummaryDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal TotalCashSales { get; set; }
    public decimal TotalCardSales { get; set; }
    public decimal TotalExpensesCash { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalBankTransfers { get; set; }
    public decimal ExpectedClosingCash { get; set; }
    public decimal? ActualClosingCash { get; set; }
    public decimal? Difference { get; set; }
    public bool IsRegistered { get; set; }
    public bool IsClosed { get; set; }
}

public class MonthlyCashSummaryDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetCashFlow { get; set; }
    public List<DailyTrendDto> DailyTrend { get; set; } = [];
}

public class DailyTrendDto
{
    public DateTime Date { get; set; }
    public decimal CashIn { get; set; }
    public decimal CashOut { get; set; }
    public decimal Net { get; set; }
}

public class BranchCashSummaryDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal TodayCashIn { get; set; }
    public decimal TodayCashOut { get; set; }
    public decimal NetPosition { get; set; }
    public decimal OpeningCash { get; set; }
    public bool IsOpenForDay { get; set; }
}

/// <summary>Completed sale invoice that has not yet been posted to the cash flow ledger.</summary>
public class SaleInvoiceCashFlowDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public decimal CashAmount { get; set; }
    public decimal CardAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public SalePaymentMethod PaymentMethod { get; set; }
    public DateTime SaleDate { get; set; }
}

/// <summary>Party payment (customer receipt / supplier payment) missing from cash flow.</summary>
public class InvoicePaymentCashFlowDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public InvoicePaymentModule Module { get; set; }
    public decimal Amount { get; set; }
    public PartyPaymentType PaymentType { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? ReferenceNo { get; set; }
    public string? InvoiceNo { get; set; }
    public string? PartyName { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Expense that has not yet been posted to the cash flow ledger.</summary>
public class ExpenseCashFlowDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ExpensePaymentMethod PaymentMethod { get; set; }
    public DateTime ExpenseDate { get; set; }
}

public class CashRegisterDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime RegisterDate { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal? ClosingCash { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? ActualCash { get; set; }
    public decimal? Difference { get; set; }
    public bool IsClosed { get; set; }
    public string? Notes { get; set; }
}
