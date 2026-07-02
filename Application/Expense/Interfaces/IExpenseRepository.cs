using POSSystem.Application.Expense.DTOs;
using ExpenseEntity = POSSystem.Domain.Expense;

namespace POSSystem.Application.Expense.Interfaces;

public interface IExpenseRepository
{
    Task<bool> CategoryExistsAsync(int categoryId, int businessId, int branchId);
    Task<string?> GetCategoryNameAsync(int categoryId);
    Task<ExpenseEntity?> GetByIdAsync(int id, int businessId);
    Task AddAsync(ExpenseEntity expense);
    Task SaveChangesAsync();
}
