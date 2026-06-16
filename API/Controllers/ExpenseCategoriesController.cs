using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.API.Extensions;
using POSSystem.Application.Common.Constants;
using POSSystem.API.Authorization;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/expense-categories")]
public class ExpenseCategoriesController : ControllerBase
{
    private readonly POSDbContext _db;

    public ExpenseCategoriesController(POSDbContext db) => _db = db;

    [HttpGet]
    [RequirePermission(PermissionModules.Expenses, PermissionActions.View)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] string? search = null)
    {
        var biz    = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        if (branch <= 0)
            return BadRequest(new { message = "branchId is required." });

        var query = _db.ExpenseCategories
            .AsNoTracking()
            .Where(c => c.BusinessId == biz && c.BranchId == branch && c.Status && !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Name.Contains(term));
        }

        var categories = await query
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    [RequirePermission(PermissionModules.Expenses, PermissionActions.Create)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateExpenseCategoryRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Category name is required." });

        var biz    = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        var branch = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (branch <= 0)
            return BadRequest(new { message = "BranchId is required." });

        var name = dto.Name.Trim();
        var exists = await _db.ExpenseCategories.AnyAsync(c =>
            c.BusinessId == biz && c.BranchId == branch && c.Name == name && !c.IsDeleted);

        if (exists)
            return BadRequest(new { message = $"Expense category '{name}' already exists." });

        var category = new ExpenseCategory
        {
            BusinessId  = biz,
            BranchId    = branch,
            Name        = name,
            Description = dto.Description?.Trim(),
            Status      = true,
        };

        _db.ExpenseCategories.Add(category);
        await _db.SaveChangesAsync();

        return Ok(new { category.Id, category.Name, category.Description });
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionModules.Expenses, PermissionActions.Delete)]
    public async Task<IActionResult> DeleteCategory(int id, [FromQuery] int branchId)
    {
        var biz    = this.ResolveBusinessId(null);
        var branch = this.ResolveBranchId(branchId > 0 ? branchId : null);

        if (branch <= 0)
            return BadRequest(new { message = "branchId is required." });

        var category = await _db.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == biz && c.BranchId == branch && !c.IsDeleted);

        if (category == null)
            return NotFound(new { message = "Expense category not found." });

        var inUse = await _db.Expenses.AnyAsync(e =>
            e.ExpenseCategoryId == id && e.BusinessId == biz && e.BranchId == branch && !e.IsDeleted);

        if (inUse)
            return BadRequest(new { message = "Cannot delete category because it is referenced by expenses." });

        category.IsDeleted  = true;
        category.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public class CreateExpenseCategoryRequest
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
