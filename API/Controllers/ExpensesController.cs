using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.API.Extensions;
using POSSystem.Application.CashFlow.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.API.Authorization;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly POSDbContext _db;
    private readonly ICashFlowService _cashFlow;

    public ExpensesController(POSDbContext db, ICashFlowService cashFlow)
    {
        _db = db;
        _cashFlow = cashFlow;
    }

    [HttpGet]
    [RequirePermission(PermissionModules.Expenses, PermissionActions.View)]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] ExpensePaymentMethod? paymentMethod,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var biz    = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        var query = _db.Expenses
            .Include(e => e.Branch)
            .Include(e => e.ExpenseCategory)
            .AsNoTracking()
            .Where(e => e.BusinessId == biz);

        if (branch > 0)
            query = query.Where(e => e.BranchId == branch);

        if (fromDate.HasValue)
            query = query.Where(e => e.ExpenseDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(e => e.ExpenseDate < toDate.Value.Date.AddDays(1));

        if (paymentMethod.HasValue)
            query = query.Where(e => e.PaymentMethod == paymentMethod.Value);

        var totalRecords = await query.CountAsync();

        var summary = await query.GroupBy(_ => 1).Select(g => new
        {
            TotalExpenses = g.Sum(e => (decimal?)e.Amount) ?? 0,
            TotalCash     = g.Where(e => e.PaymentMethod == ExpensePaymentMethod.Cash).Sum(e => (decimal?)e.Amount) ?? 0,
            TotalBank     = g.Where(e => e.PaymentMethod == ExpensePaymentMethod.Bank).Sum(e => (decimal?)e.Amount) ?? 0,
            Count         = g.Count(),
        }).FirstOrDefaultAsync();

        var expenses = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.BranchId,
                BranchName         = e.Branch.Name,
                e.ExpenseCategoryId,
                CategoryName       = e.ExpenseCategory.Name,
                e.Description,
                e.Amount,
                PaymentMethod      = e.PaymentMethod.ToString(),
                ExpenseDate        = e.ExpenseDate,
                e.ReferenceNo,
                e.Notes,
                e.CreatedBy,
                CreatedAt          = e.CreatedAt,
            })
            .ToListAsync();

        return Ok(new
        {
            expenses,
            totalRecords,
            totalPages   = (int)Math.Ceiling(totalRecords / (double)pageSize),
            currentPage  = page,
            pageSize,
            summary = new
            {
                totalExpenses = summary?.TotalExpenses ?? 0,
                totalCash     = summary?.TotalCash ?? 0,
                totalBank     = summary?.TotalBank ?? 0,
                count         = summary?.Count ?? 0,
            }
        });
    }

    [HttpPost]
    [RequirePermission(PermissionModules.Expenses, PermissionActions.Create)]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest dto)
    {
        if (dto.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero." });

        var biz    = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        var branch = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (branch <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (dto.ExpenseCategoryId <= 0)
            return BadRequest(new { message = "ExpenseCategoryId is required." });

        var categoryExists = await _db.ExpenseCategories.AnyAsync(c =>
            c.Id == dto.ExpenseCategoryId && c.BusinessId == biz && c.BranchId == branch && !c.IsDeleted);

        if (!categoryExists)
            return BadRequest(new { message = "Invalid expense category." });

        var expense = new Expense
        {
            BusinessId         = biz,
            BranchId           = branch,
            ExpenseCategoryId  = dto.ExpenseCategoryId,
            Description        = dto.Description.Trim(),
            Amount             = dto.Amount,
            PaymentMethod      = dto.PaymentMethod,
            ExpenseDate        = (dto.ExpenseDate ?? DateTime.UtcNow).Date,
            ReferenceNo        = dto.ReferenceNo?.Trim(),
            Notes              = dto.Notes?.Trim(),
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        var categoryName = await _db.ExpenseCategories
            .Where(c => c.Id == expense.ExpenseCategoryId)
            .Select(c => c.Name)
            .FirstAsync();

        await _cashFlow.RecordExpenseAsync(
            biz,
            branch,
            expense.Id,
            $"{categoryName}: {expense.Description}",
            expense.Amount,
            MapPaymentMethod(expense.PaymentMethod),
            expense.ExpenseDate);

        return Ok(new
        {
            expense.Id,
            expense.BranchId,
            expense.ExpenseCategoryId,
            CategoryName = categoryName,
            expense.Description,
            expense.Amount,
            PaymentMethod = expense.PaymentMethod.ToString(),
            ExpenseDate   = expense.ExpenseDate,
            expense.ReferenceNo,
            expense.Notes,
            expense.CreatedBy,
            CreatedAt     = expense.CreatedAt,
        });
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionModules.Expenses, PermissionActions.Edit)]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] CreateExpenseRequest dto)
    {
        if (dto.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero." });

        var biz = this.ResolveBusinessId(null);

        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.BusinessId == biz);
        if (expense == null)
            return NotFound(new { message = "Expense not found." });

        if (dto.ExpenseCategoryId <= 0)
            return BadRequest(new { message = "ExpenseCategoryId is required." });

        var categoryExists = await _db.ExpenseCategories.AnyAsync(c =>
            c.Id == dto.ExpenseCategoryId &&
            c.BusinessId == biz &&
            c.BranchId == expense.BranchId &&
            !c.IsDeleted);

        if (!categoryExists)
            return BadRequest(new { message = "Invalid expense category." });

        var oldCategoryName = await _db.ExpenseCategories
            .Where(c => c.Id == expense.ExpenseCategoryId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync() ?? "Expense";

        var oldAmount         = expense.Amount;
        var oldPaymentMethod  = expense.PaymentMethod;
        var oldDescription    = $"{oldCategoryName}: {expense.Description}";
        var oldExpenseDate    = expense.ExpenseDate;

        expense.ExpenseCategoryId = dto.ExpenseCategoryId;
        expense.Description       = dto.Description.Trim();
        expense.Amount            = dto.Amount;
        expense.PaymentMethod     = dto.PaymentMethod;
        expense.ExpenseDate       = (dto.ExpenseDate ?? DateTime.UtcNow).Date;
        expense.ReferenceNo       = dto.ReferenceNo?.Trim();
        expense.Notes             = dto.Notes?.Trim();
        expense.ModifiedAt        = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var categoryName = await _db.ExpenseCategories
            .Where(c => c.Id == expense.ExpenseCategoryId)
            .Select(c => c.Name)
            .FirstAsync();

        await _cashFlow.ReverseExpenseAsync(
            biz,
            expense.BranchId,
            expense.Id,
            oldDescription,
            oldAmount,
            MapPaymentMethod(oldPaymentMethod),
            oldExpenseDate,
            "updated");

        await _cashFlow.RecordExpenseAsync(
            biz,
            expense.BranchId,
            expense.Id,
            $"{categoryName}: {expense.Description}",
            expense.Amount,
            MapPaymentMethod(expense.PaymentMethod),
            expense.ExpenseDate);

        return Ok(new
        {
            expense.Id,
            expense.BranchId,
            expense.ExpenseCategoryId,
            CategoryName = categoryName,
            expense.Description,
            expense.Amount,
            PaymentMethod = expense.PaymentMethod.ToString(),
            ExpenseDate   = expense.ExpenseDate,
            expense.ReferenceNo,
            expense.Notes,
            expense.CreatedBy,
            CreatedAt     = expense.CreatedAt,
        });
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionModules.Expenses, PermissionActions.Delete)]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var biz = this.ResolveBusinessId(null);

        var expense = await _db.Expenses
            .Include(e => e.ExpenseCategory)
            .FirstOrDefaultAsync(e => e.Id == id && e.BusinessId == biz);
        if (expense == null)
            return NotFound(new { message = "Expense not found." });

        var description = $"{expense.ExpenseCategory.Name}: {expense.Description}";

        expense.IsDeleted  = true;
        expense.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _cashFlow.ReverseExpenseAsync(
            biz,
            expense.BranchId,
            expense.Id,
            description,
            expense.Amount,
            MapPaymentMethod(expense.PaymentMethod),
            expense.ExpenseDate,
            "deleted");

        return NoContent();
    }

    private static CashFlowPaymentMethod MapPaymentMethod(ExpensePaymentMethod method) => method switch
    {
        ExpensePaymentMethod.Bank   => CashFlowPaymentMethod.Bank,
        ExpensePaymentMethod.Wallet => CashFlowPaymentMethod.Wallet,
        _                           => CashFlowPaymentMethod.Cash,
    };
}

public class CreateExpenseRequest
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
