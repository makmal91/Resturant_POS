using POSSystem.Domain;

namespace POSSystem.Application.Payments.DTOs;

public class RecordCustomerPaymentDto
{
    public int CustomerId { get; set; }
    public int? SaleInvoiceId { get; set; }
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
    public int? CreatedBy { get; set; }
}

public class RecordSupplierPaymentDto
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
    public int? CreatedBy { get; set; }
}

public class PaymentAllocationItemDto
{
    public int InvoiceId { get; set; }
    public decimal AppliedAmount { get; set; }
}

public class PaymentAllocationDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string? InvoiceNo { get; set; }
    public decimal AppliedAmount { get; set; }
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
    public InvoicePaymentCategory Category { get; set; } = InvoicePaymentCategory.AgainstInvoice;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAdvancePayment => Allocations.Count == 0 && !InvoiceId.HasValue;
    public bool IsReversed { get; set; }
    public bool HasAllocations => Allocations.Count > 0;
    public List<PaymentAllocationDto> Allocations { get; set; } = new();
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

public class PaymentListFilterDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public InvoicePaymentModule? Module { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public bool IncludeReversed { get; set; }
}

public class InvoiceBalanceDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public decimal InvoiceTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public string SettlementStatus { get; set; } = "Pending";
}

public class OutstandingInvoiceOptionDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public string SettlementStatus { get; set; } = "Pending";
}

public class ReversePaymentDto
{
    public int PaymentId { get; set; }
    public string? Reason { get; set; }
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public int? ReversedBy { get; set; }
}

public class ReversePaymentRequest
{
    public string? Reason { get; set; }
}

public class UpdatePaymentDto
{
    public PartyPaymentType PaymentType { get; set; } = PartyPaymentType.Cash;
    public InvoicePaymentCategory? Category { get; set; }
    public decimal Amount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
    public bool AutoAllocate { get; set; } = true;
    public int? SaleInvoiceId { get; set; }
    public int? PurchaseId { get; set; }
    public List<PaymentAllocationItemDto>? Allocations { get; set; }
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public int? ModifiedBy { get; set; }
}
