using POSSystem.Domain;

namespace POSSystem.Application.Accounting.DTOs;

public class AccountingTransactionDto
{
    public int AccountId { get; set; }
    public int BranchId { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public Guid GroupId { get; set; }
    public int? ReferenceId { get; set; }
    public string? Description { get; set; }
    public GlTransactionType TransactionType { get; set; } = GlTransactionType.Manual;
    public DateTime? Date { get; set; }
    public Guid? ReversalOfGroupId { get; set; }
    public Guid? OriginalGroupId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsReversal { get; set; }
}
