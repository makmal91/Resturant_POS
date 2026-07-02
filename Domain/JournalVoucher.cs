namespace POSSystem.Domain;

/// <summary>Manual cash journal entry (Cash In / Cash Out) with a JV document number.</summary>
public class JournalVoucher : BaseEntity
{
    public string VoucherNo { get; set; } = string.Empty;
    public CashFlowTransactionType TransactionType { get; set; }
    public CashFlowPaymentMethod PaymentMethod { get; set; } = CashFlowPaymentMethod.Cash;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime VoucherDate { get; set; } = DateTime.UtcNow;
    public Guid GlGroupId { get; set; }

    public virtual Branch Branch { get; set; } = null!;
}
