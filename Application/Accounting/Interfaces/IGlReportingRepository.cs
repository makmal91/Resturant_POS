using POSSystem.Domain;

namespace POSSystem.Application.Accounting.Interfaces;

public interface IGlReportingRepository
{
    Task<decimal> GetAccountBalanceAsync(int accountId, int? branchId, DateTime? asOfDate = null);
    Task<IReadOnlyDictionary<int, decimal>> GetAccountBalancesAsync(
        IEnumerable<int> accountIds, bool creditNormal, int? branchId, DateTime? asOfDate = null);
    Task<decimal> GetSubtreeBalanceAsync(
        int? branchId, string rootAccountName, bool creditNormal, DateTime? asOfDate = null);
    Task<decimal> GetAccountTypeNetAsync(int? branchId, AccountType accountType, DateTime? from, DateTime? to);
    Task<decimal> GetSubtreePeriodNetAsync(
        int? branchId, string rootAccountName, bool creditNormal, DateTime? from, DateTime? to);
    Task<decimal> GetTransactionTypeDebitTotalAsync(
        int? branchId, GlTransactionType transactionType, DateTime? from, DateTime? to);
    Task<IReadOnlyDictionary<int, decimal>> GetSubtreePeriodNetByBranchAsync(
        string rootAccountName, bool creditNormal, DateTime from, DateTime to);
    Task<IReadOnlyDictionary<int, decimal>> GetAccountTypeNetByBranchAsync(
        AccountType accountType, DateTime from, DateTime to);
    Task<decimal> GetPartySubAccountsNetAsync(
        int businessId, int? branchId, AccountType partyType, DateTime? asOfDate = null);
    Task<IReadOnlyDictionary<int, DateTime>> GetAccountLastActivityDatesAsync(
        IEnumerable<int> accountIds, int? branchId, DateTime? asOfDate = null);
    Task<IReadOnlyDictionary<int, decimal>> GetPartyDocumentChargesAsync(
        IEnumerable<int> partyAccountIds, GlTransactionType transactionType, int? branchId, DateTime? asOfDate = null);
    Task<IReadOnlyList<DailyGlCashMovementRow>> GetDailyCashMovementsAsync(int? branchId, DateTime from, DateTime to);
    Task<GlCashDaySummary> GetGlCashDaySummaryAsync(int branchId, DateTime date);
}

public sealed class DailyGlCashMovementRow
{
    public DateTime Date { get; init; }
    public decimal CashIn { get; init; }
    public decimal CashOut { get; init; }
}

public sealed class GlCashDaySummary
{
    public decimal CashSales { get; init; }
    public decimal CardSales { get; init; }
    public decimal Expenses { get; init; }
    public decimal CashIn { get; init; }
    public decimal CashOut { get; init; }
    public decimal NetMovement { get; init; }
}
