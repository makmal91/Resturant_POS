using POSSystem.Application.Accounting.DTOs;
using POSSystem.Application.Accounting.Interfaces;

namespace POSSystem.Application.Accounting.Services;

public class TrialBalanceService : ITrialBalanceService
{
    private readonly ITrialBalanceRepository _repository;

    public TrialBalanceService(ITrialBalanceRepository repository) => _repository = repository;

    public async Task<TrialBalanceReportDto> GetTrialBalanceAsync(TrialBalanceFilterDto filter)
    {
        var accounts = await _repository.GetActiveAccountsAsync();
        var totals = await _repository.GetAccountPeriodTotalsAsync(
            filter.BranchId, filter.FromDate, filter.ToDate);

        var totalsByAccount = totals.ToDictionary(t => t.AccountId);
        var accountMap = accounts.ToDictionary(a => a.Id);
        var childrenByParent = accounts
            .Where(a => a.ParentId.HasValue)
            .GroupBy(a => a.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        var hasChildren = accounts.ToDictionary(
            a => a.Id,
            a => childrenByParent.ContainsKey(a.Id));

        var subtreeDebit = new Dictionary<int, decimal>();
        var subtreeCredit = new Dictionary<int, decimal>();

        foreach (var account in accounts)
        {
            var ids = CollectSubtreeIds(account.Id, childrenByParent);
            decimal debit = 0, credit = 0;
            foreach (var id in ids)
            {
                if (totalsByAccount.TryGetValue(id, out var row))
                {
                    debit += row.TotalDebit;
                    credit += row.TotalCredit;
                }
            }

            subtreeDebit[account.Id] = debit;
            subtreeCredit[account.Id] = credit;
        }

        var rows = filter.AccountLevel == TrialBalanceAccountLevel.ParentOnly
            ? BuildParentOnlyRows(accounts, hasChildren, subtreeDebit, subtreeCredit, filter.ShowZeroBalance)
            : BuildHierarchyRows(accounts, childrenByParent, totalsByAccount, filter.ShowZeroBalance);

        var totalDebit = rows.Sum(r => r.Debit);
        var totalCredit = rows.Sum(r => r.Credit);
        var isBalanced = Math.Abs(totalDebit - totalCredit) <= 0.01m;

        return new TrialBalanceReportDto
        {
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            BranchId = filter.BranchId,
            AccountLevel = filter.AccountLevel,
            ShowZeroBalance = filter.ShowZeroBalance,
            Rows = rows,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            IsBalanced = isBalanced,
            BalanceMessage = isBalanced
                ? null
                : "Trial Balance is not balanced. Please check journal entries.",
        };
    }

    private static List<TrialBalanceRowDto> BuildParentOnlyRows(
        IReadOnlyList<GlAccountListItemDto> accounts,
        IReadOnlyDictionary<int, bool> hasChildren,
        IReadOnlyDictionary<int, decimal> subtreeDebit,
        IReadOnlyDictionary<int, decimal> subtreeCredit,
        bool showZeroBalance)
    {
        var accountIds = accounts.Select(a => a.Id).ToHashSet();
        var rows = new List<TrialBalanceRowDto>();

        foreach (var account in accounts.OrderBy(a => a.Type).ThenBy(a => a.Name))
        {
            var isParentNode = hasChildren.GetValueOrDefault(account.Id);
            var isRootLeaf = !isParentNode && (!account.ParentId.HasValue || !accountIds.Contains(account.ParentId.Value));
            if (!isParentNode && !isRootLeaf)
                continue;

            var (debit, credit) = ToTrialColumns(
                subtreeDebit.GetValueOrDefault(account.Id),
                subtreeCredit.GetValueOrDefault(account.Id));

            if (!showZeroBalance && debit == 0 && credit == 0)
                continue;

            rows.Add(new TrialBalanceRowDto
            {
                AccountId = account.Id,
                AccountCode = FormatAccountCode(account.Id),
                AccountName = account.Name,
                ParentAccountId = account.ParentId,
                Level = 0,
                HasChildren = isParentNode,
                Debit = debit,
                Credit = credit,
            });
        }

        return rows;
    }

    private static List<TrialBalanceRowDto> BuildHierarchyRows(
        IReadOnlyList<GlAccountListItemDto> accounts,
        IReadOnlyDictionary<int, List<int>> childrenByParent,
        IReadOnlyDictionary<int, AccountPeriodTotalsRow> totalsByAccount,
        bool showZeroBalance)
    {
        var accountMap = accounts.ToDictionary(a => a.Id);
        var accountIds = accountMap.Keys.ToHashSet();
        var roots = accounts
            .Where(a => !a.ParentId.HasValue || !accountIds.Contains(a.ParentId.Value))
            .OrderBy(a => a.Type)
            .ThenBy(a => a.Name)
            .ToList();

        var rows = new List<TrialBalanceRowDto>();
        foreach (var root in roots)
            WalkHierarchy(root, childrenByParent, accountMap, totalsByAccount, showZeroBalance, 0, rows);

        return rows;
    }

    private static void WalkHierarchy(
        GlAccountListItemDto account,
        IReadOnlyDictionary<int, List<int>> childrenByParent,
        IReadOnlyDictionary<int, GlAccountListItemDto> accountMap,
        IReadOnlyDictionary<int, AccountPeriodTotalsRow> totalsByAccount,
        bool showZeroBalance,
        int level,
        List<TrialBalanceRowDto> rows)
    {
        totalsByAccount.TryGetValue(account.Id, out var ownTotals);
        var rawDebit = ownTotals?.TotalDebit ?? 0;
        var rawCredit = ownTotals?.TotalCredit ?? 0;
        var (debit, credit) = ToTrialColumns(rawDebit, rawCredit);
        var hasChildren = childrenByParent.ContainsKey(account.Id);

        var descendantHasActivity = HasDescendantActivity(account.Id, childrenByParent, totalsByAccount);
        if (showZeroBalance || debit > 0 || credit > 0 || hasChildren || descendantHasActivity)
        {
            rows.Add(new TrialBalanceRowDto
            {
                AccountId = account.Id,
                AccountCode = FormatAccountCode(account.Id),
                AccountName = account.Name,
                ParentAccountId = account.ParentId,
                Level = level,
                HasChildren = hasChildren,
                Debit = debit,
                Credit = credit,
            });
        }

        if (!hasChildren)
            return;

        foreach (var childId in childrenByParent[account.Id]
                     .Select(id => accountMap.GetValueOrDefault(id))
                     .Where(a => a != null)
                     .OrderBy(a => a!.Name))
        {
            WalkHierarchy(childId!, childrenByParent, accountMap, totalsByAccount, showZeroBalance, level + 1, rows);
        }
    }

    private static bool HasDescendantActivity(
        int accountId,
        IReadOnlyDictionary<int, List<int>> childrenByParent,
        IReadOnlyDictionary<int, AccountPeriodTotalsRow> totalsByAccount)
    {
        if (!childrenByParent.TryGetValue(accountId, out var children))
            return false;

        foreach (var childId in children)
        {
            if (totalsByAccount.ContainsKey(childId))
                return true;
            if (HasDescendantActivity(childId, childrenByParent, totalsByAccount))
                return true;
        }

        return false;
    }

    private static List<int> CollectSubtreeIds(int rootId, IReadOnlyDictionary<int, List<int>> childrenByParent)
    {
        var result = new List<int> { rootId };
        if (!childrenByParent.TryGetValue(rootId, out var children))
            return result;

        foreach (var childId in children)
            result.AddRange(CollectSubtreeIds(childId, childrenByParent));

        return result;
    }

    private static (decimal Debit, decimal Credit) ToTrialColumns(decimal totalDebit, decimal totalCredit)
    {
        if (totalDebit > totalCredit)
            return (totalDebit - totalCredit, 0);
        if (totalCredit > totalDebit)
            return (0, totalCredit - totalDebit);
        return (0, 0);
    }

    internal static string FormatAccountCode(int accountId) => $"ACC{accountId:D5}";
}
