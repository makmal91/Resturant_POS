namespace POSSystem.Application.Ledger.DTOs;

/// <summary>Raw party activity row before debit/credit mapping and running balance.</summary>
public class PartyLedgerSourceDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int ReferenceId { get; set; }
    public int? PaymentId { get; set; }
    public bool AffectsBalance { get; set; } = true;
    public bool IsReversal { get; set; }
    public bool HasInvoiceBreakdown { get; set; }
    public List<PartyLedgerInvoiceAllocationDto> InvoiceAllocations { get; set; } = new();
}
