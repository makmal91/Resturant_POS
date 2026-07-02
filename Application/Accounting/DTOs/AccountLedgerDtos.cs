using POSSystem.Domain;

namespace POSSystem.Application.Accounting.DTOs;

/// <summary>
/// Filter for universal account ledger (backed by GL journal lines / Transactions table).
/// </summary>
public class AccountLedgerFilterDto
{
    public int AccountId { get; set; }
    public int BusinessId { get; set; }
    public int? BranchId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    /// <summary>When false (default), only active lines (IsActive = 1). When true, includes full audit trail.</summary>
    public bool AuditView { get; set; }

    /// <summary>When audit view is on, sort and group related lines by <see cref="AccountLedgerEntryDto.OriginalGroupId"/>.</summary>
    public bool GroupByChain { get; set; }
}

public class AccountLedgerEntryDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    /// <summary>Other account(s) in the same journal entry (contra side).</summary>
    public string AccountName { get; set; } = string.Empty;
    /// <summary>GL account this line posts to (populated when viewing a parent with sub-accounts).</summary>
    public string LineAccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
    public bool IsOpeningBalance { get; set; }
    public Guid GroupId { get; set; }
    public Guid? OriginalGroupId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsReversal { get; set; }
    /// <summary>Inactive original superseded by an edit (audit view only).</summary>
    public bool IsSuperseded { get; set; }
    /// <summary>Active replacement after an edit.</summary>
    public bool IsReplacement { get; set; }
}

public class AccountLedgerPageDto
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal PeriodNet { get; set; }
    public List<AccountLedgerEntryDto> Entries { get; set; } = new();
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public bool AuditView { get; set; }
    /// <summary>True when the selected account has child accounts and their lines are included.</summary>
    public bool IncludesSubAccounts { get; set; }
    /// <summary>Authoritative closing balance (clean / effective position).</summary>
    public decimal EffectiveClosingBalance { get; set; }
}

public class GlAccountListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public bool IsActive { get; set; }
}
