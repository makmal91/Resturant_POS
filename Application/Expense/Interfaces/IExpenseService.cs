using POSSystem.Application.Expense.DTOs;

namespace POSSystem.Application.Expense.Interfaces;

public interface IExpenseService
{
    Task<ExpenseDetailDto> CreateAsync(CreateExpenseDto dto);
    Task<ExpenseDetailDto> UpdateAsync(int id, CreateExpenseDto dto);
    Task DeleteAsync(int id, int businessId);
}
