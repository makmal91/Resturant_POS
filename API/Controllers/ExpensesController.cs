using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.API.Extensions;
using POSSystem.Application.Expense.DTOs;
using POSSystem.Application.Expense.Interfaces;
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
    private readonly IExpenseService _expenseService;

    public ExpensesController(POSDbContext db, IExpenseService expenseService)
    {
        _db = db;
        _expenseService = expenseService;
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

        try
        {
            var result = await _expenseService.CreateAsync(new CreateExpenseDto
            {
                BusinessId = biz,
                BranchId = branch,
                ExpenseCategoryId = dto.ExpenseCategoryId,
                Description = dto.Description,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                ExpenseDate = dto.ExpenseDate,
                ReferenceNo = dto.ReferenceNo,
                Notes = dto.Notes,
            });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionModules.Expenses, PermissionActions.Edit)]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] CreateExpenseRequest dto)
    {
        if (dto.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero." });

        var biz = this.ResolveBusinessId(null);

        try
        {
            var result = await _expenseService.UpdateAsync(id, new CreateExpenseDto
            {
                BusinessId = biz,
                BranchId = dto.BranchId,
                ExpenseCategoryId = dto.ExpenseCategoryId,
                Description = dto.Description,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                ExpenseDate = dto.ExpenseDate,
                ReferenceNo = dto.ReferenceNo,
                Notes = dto.Notes,
            });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message == "Expense not found."
                ? NotFound(new { message = ex.Message })
                : BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionModules.Expenses, PermissionActions.Delete)]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var biz = this.ResolveBusinessId(null);

        try
        {
            await _expenseService.DeleteAsync(id, biz);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message == "Expense not found."
                ? NotFound(new { message = ex.Message })
                : BadRequest(new { message = ex.Message });
        }
    }
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
