using POSSystem.Application.Common.Constants;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Application.Expense.DTOs;
using POSSystem.Application.Expense.Interfaces;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Domain;
using ExpenseEntity = POSSystem.Domain.Expense;

namespace POSSystem.Application.Expense.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _repository;
    private readonly IAccountingIntegrationService _accountingIntegration;
    private readonly IAccountingRepository _accountingRepository;
    private readonly ICodeGeneratorService _codeGenerator;

    public ExpenseService(
        IExpenseRepository repository,
        IAccountingIntegrationService accountingIntegration,
        IAccountingRepository accountingRepository,
        ICodeGeneratorService codeGenerator)
    {
        _repository = repository;
        _accountingIntegration = accountingIntegration;
        _accountingRepository = accountingRepository;
        _codeGenerator = codeGenerator;
    }

    public async Task<ExpenseDetailDto> CreateAsync(CreateExpenseDto dto)
    {
        if (dto.Amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero.");
        if (dto.BranchId <= 0)
            throw new InvalidOperationException("BranchId is required.");
        if (dto.ExpenseCategoryId <= 0)
            throw new InvalidOperationException("ExpenseCategoryId is required.");

        if (!await _repository.CategoryExistsAsync(dto.ExpenseCategoryId, dto.BusinessId, dto.BranchId))
            throw new InvalidOperationException("Invalid expense category.");

        var expense = new ExpenseEntity
        {
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId,
            ExpenseCategoryId = dto.ExpenseCategoryId,
            Description = dto.Description.Trim(),
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            ExpenseDate = (dto.ExpenseDate ?? DateTime.UtcNow).Date,
            ReferenceNo = dto.ReferenceNo?.Trim(),
            Notes = dto.Notes?.Trim(),
        };

        await _accountingRepository.RunInTransactionAsync(async () =>
        {
            expense.ReferenceNo = await _codeGenerator.GenerateAsync(
                CodeModuleNames.Expense, dto.BranchId);

            await _repository.AddAsync(expense);
            await _repository.SaveChangesAsync();
            await _accountingIntegration.PostExpenseAsync(expense);
        });

        return await MapDetailAsync(expense);
    }

    public async Task<ExpenseDetailDto> UpdateAsync(int id, CreateExpenseDto dto)
    {
        if (dto.Amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero.");
        if (dto.ExpenseCategoryId <= 0)
            throw new InvalidOperationException("ExpenseCategoryId is required.");

        var expense = await _repository.GetByIdAsync(id, dto.BusinessId)
            ?? throw new InvalidOperationException("Expense not found.");

        if (!await _repository.CategoryExistsAsync(dto.ExpenseCategoryId, dto.BusinessId, expense.BranchId))
            throw new InvalidOperationException("Invalid expense category.");

        await _accountingRepository.RunInTransactionAsync(async () =>
        {
            await _accountingIntegration.ReverseTransactionAsync(
                id, GlTransactionType.Expense, $"Edit — {expense.Description}");

            expense.ExpenseCategoryId = dto.ExpenseCategoryId;
            expense.Description = dto.Description.Trim();
            expense.Amount = dto.Amount;
            expense.PaymentMethod = dto.PaymentMethod;
            expense.ExpenseDate = (dto.ExpenseDate ?? DateTime.UtcNow).Date;
            expense.Notes = dto.Notes?.Trim();
            expense.ModifiedAt = DateTime.UtcNow;

            await _repository.SaveChangesAsync();
            await _accountingIntegration.PostExpenseAsync(expense);
        });

        return await MapDetailAsync(expense);
    }

    public async Task DeleteAsync(int id, int businessId)
    {
        var expense = await _repository.GetByIdAsync(id, businessId)
            ?? throw new InvalidOperationException("Expense not found.");

        await _accountingRepository.RunInTransactionAsync(async () =>
        {
            await _accountingIntegration.ReverseTransactionAsync(
                id, GlTransactionType.Expense, $"Delete — {expense.Description}");

            expense.IsDeleted = true;
            expense.ModifiedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();
        });
    }

    private async Task<ExpenseDetailDto> MapDetailAsync(ExpenseEntity expense)
    {
        var categoryName = await _repository.GetCategoryNameAsync(expense.ExpenseCategoryId) ?? "Expense";
        return new ExpenseDetailDto
        {
            Id = expense.Id,
            BranchId = expense.BranchId,
            ExpenseCategoryId = expense.ExpenseCategoryId,
            CategoryName = categoryName,
            Description = expense.Description,
            Amount = expense.Amount,
            PaymentMethod = expense.PaymentMethod.ToString(),
            ExpenseDate = expense.ExpenseDate,
            ReferenceNo = expense.ReferenceNo,
            Notes = expense.Notes,
            CreatedBy = expense.CreatedBy,
            CreatedAt = expense.CreatedAt,
        };
    }
}
