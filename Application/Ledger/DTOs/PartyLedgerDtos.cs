using System.Text.Json.Serialization;
using POSSystem.Application.Payments.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Ledger.DTOs;

public class ReceiveCustomerPaymentDto
{
    public int CustomerId { get; set; }
    public int? SaleInvoiceId { get; set; }
    public PartyPaymentType PaymentType { get; set; } = PartyPaymentType.Cash;
    public decimal Amount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
    public bool AutoAllocate { get; set; } = true;
    public List<PaymentAllocationItemDto>? Allocations { get; set; }
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
}

public class PaySupplierDto
{
    public int SupplierId { get; set; }
    public int? PurchaseId { get; set; }
    public PartyPaymentType PaymentType { get; set; } = PartyPaymentType.Cash;
    public InvoicePaymentCategory Category { get; set; } = InvoicePaymentCategory.AgainstInvoice;
    public decimal Amount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
    public bool AutoAllocate { get; set; } = true;
    public List<PaymentAllocationItemDto>? Allocations { get; set; }
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
}

public class PartyLedgerFilterDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public int PartyId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public bool AuditView { get; set; }
    public bool GroupByChain { get; set; }
}

public class PartyLedgerEntryDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    /// <summary>Single-entry: payment received (customer ledger).</summary>
    public decimal In { get; set; }
    /// <summary>Single-entry: sale / charge (customer ledger).</summary>
    public decimal Out { get; set; }
    public decimal RunningBalance { get; set; }
    public int ReferenceId { get; set; }
    public int? PaymentId { get; set; }
    public bool CanReverse { get; set; }
    public bool HasInvoiceBreakdown { get; set; }
    public List<PartyLedgerInvoiceAllocationDto> InvoiceAllocations { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public bool IsReversal { get; set; }
    public bool IsSuperseded { get; set; }
    public bool IsReplacement { get; set; }
    public string? OriginalGroupId { get; set; }
    public string? GroupId { get; set; }

    [JsonIgnore]
    public bool AffectsPayableBalance { get; set; } = true;
}

public class PartyLedgerInvoiceAllocationDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public decimal AppliedAmount { get; set; }
}

public class PartyLedgerPageDto
{
    public int PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal PeriodClosingBalance { get; set; }
    public List<PartyLedgerEntryDto> Entries { get; set; } = new();
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public bool AuditView { get; set; }
    public decimal EffectiveClosingBalance { get; set; }
}

public class PartyBalanceDto
{
    public int PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
