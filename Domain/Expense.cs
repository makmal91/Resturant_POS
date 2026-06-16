namespace POSSystem.Domain;

public enum ExpensePaymentMethod
{
    Cash   = 1,
    Bank   = 2,
    Wallet = 3,
}

public class Expense : BaseEntity
{
    public string CategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ExpensePaymentMethod PaymentMethod { get; set; } = ExpensePaymentMethod.Cash;
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }

    public virtual Branch Branch { get; set; } = null!;
}
