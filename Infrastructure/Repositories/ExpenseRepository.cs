using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Expense.Interfaces;
using ExpenseEntity = POSSystem.Domain.Expense;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly POSDbContext _db;

    public ExpenseRepository(POSDbContext db) => _db = db;

    public Task<bool> CategoryExistsAsync(int categoryId, int businessId, int branchId) =>
        _db.ExpenseCategories.AnyAsync(c =>
            c.Id == categoryId && c.BusinessId == businessId && c.BranchId == branchId && !c.IsDeleted);

    public Task<string?> GetCategoryNameAsync(int categoryId) =>
        _db.ExpenseCategories
            .Where(c => c.Id == categoryId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync();

    public Task<ExpenseEntity?> GetByIdAsync(int id, int businessId) =>
        _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.BusinessId == businessId && !e.IsDeleted);

    public async Task AddAsync(ExpenseEntity expense) => await _db.Expenses.AddAsync(expense);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
