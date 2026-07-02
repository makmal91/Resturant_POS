using POSSystem.Domain;

namespace POSSystem.Application.Expense.DTOs;

public class CreateExpenseDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public int ExpenseCategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ExpensePaymentMethod PaymentMethod { get; set; } = ExpensePaymentMethod.Cash;
    public DateTime? ExpenseDate { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
}

public class ExpenseDetailDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int ExpenseCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
