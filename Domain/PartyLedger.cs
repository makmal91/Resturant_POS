namespace POSSystem.Domain;

public enum CustomerLedgerTransactionType
{
    CreditSale = 1,
    PaymentReceived = 2,
    Reversal = 3,
    OpeningBalance = 4,
    CashSale = 5
}

public enum SupplierLedgerTransactionType
{
    CreditPurchase = 1,
    PaymentMade = 2,
    Reversal = 3,
    CashPurchase = 4,
}

public class CustomerLedgerTransaction : BaseEntity
{
    public int CustomerId { get; set; }
    public int ReferenceId { get; set; }
    public CustomerLedgerTransactionType Type { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public decimal RunningBalance { get; set; }
    public string Remarks { get; set; } = string.Empty;

    public virtual Customer Customer { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}

public class SupplierLedgerTransaction : BaseEntity
{
    public int SupplierId { get; set; }
    public int ReferenceId { get; set; }
    public SupplierLedgerTransactionType Type { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public decimal RunningBalance { get; set; }
    public string Remarks { get; set; } = string.Empty;

    public virtual Supplier Supplier { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
