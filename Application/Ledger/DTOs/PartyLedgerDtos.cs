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
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
}

public class PaySupplierDto
{
    public int SupplierId { get; set; }
    public int? PurchaseId { get; set; }
    public PartyPaymentType PaymentType { get; set; } = PartyPaymentType.Cash;
    public decimal Amount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
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
}

public class PartyLedgerEntryDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
    public int ReferenceId { get; set; }
}

public class PartyLedgerPageDto
{
    public int PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public List<PartyLedgerEntryDto> Entries { get; set; } = new();
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}

public class PartyBalanceDto
{
    public int PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
