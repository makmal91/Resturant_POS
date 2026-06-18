using POSSystem.Domain;

namespace POSSystem.Application.Payments.DTOs;

public class RecordCustomerPaymentDto
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
    public int? CreatedBy { get; set; }
}

public class RecordSupplierPaymentDto
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
    public int? CreatedBy { get; set; }
}

public class InvoicePaymentDto
{
    public int Id { get; set; }
    public InvoicePaymentModule Module { get; set; }
    public int? InvoiceId { get; set; }
    public string? InvoiceNo { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public PartyPaymentType PaymentType { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAdvancePayment => !InvoiceId.HasValue;
}

public class InvoicePaymentFilterDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public InvoicePaymentModule? Module { get; set; }
    public int? SaleInvoiceId { get; set; }
    public int? PurchaseId { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
}

public class InvoiceBalanceDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public decimal InvoiceTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
}

public class OutstandingInvoiceOptionDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
}
