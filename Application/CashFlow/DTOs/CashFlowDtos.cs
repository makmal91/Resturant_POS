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
    public DateTime TransactionDate { get; set; }
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
