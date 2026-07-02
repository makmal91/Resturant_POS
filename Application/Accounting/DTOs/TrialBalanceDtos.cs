namespace POSSystem.Application.Accounting.DTOs;

public enum TrialBalanceAccountLevel
{
    ParentOnly = 1,
    ParentAndChild = 2,
}

public class TrialBalanceFilterDto
{
    public int BusinessId { get; set; }
    public int? BranchId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public TrialBalanceAccountLevel AccountLevel { get; set; } = TrialBalanceAccountLevel.ParentAndChild;
    public bool ShowZeroBalance { get; set; }
}

public class TrialBalanceRowDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int? ParentAccountId { get; set; }
    public int Level { get; set; }
    public bool HasChildren { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public class TrialBalanceReportDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? BranchId { get; set; }
    public TrialBalanceAccountLevel AccountLevel { get; set; }
    public bool ShowZeroBalance { get; set; }
    public IReadOnlyList<TrialBalanceRowDto> Rows { get; set; } = Array.Empty<TrialBalanceRowDto>();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced { get; set; }
    public string? BalanceMessage { get; set; }
}

public sealed class AccountPeriodTotalsRow
{
    public int AccountId { get; init; }
    public decimal TotalDebit { get; init; }
    public decimal TotalCredit { get; init; }
}
