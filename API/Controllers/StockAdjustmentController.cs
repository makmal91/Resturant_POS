using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Authorization;
using POSSystem.API.Extensions;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.StockAdjustment.DTOs;
using POSSystem.Application.StockAdjustment.Interfaces;
using System.Security.Claims;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/stock-adjustment")]
public class StockAdjustmentController : ControllerBase
{
    private readonly IStockAdjustmentService _stockAdjustmentService;

    public StockAdjustmentController(IStockAdjustmentService stockAdjustmentService)
    {
        _stockAdjustmentService = stockAdjustmentService;
    }

    [HttpGet]
    [RequirePermission(PermissionModules.StockAdjustment, PermissionActions.View)]
    public async Task<IActionResult> GetStockAdjustments(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int? adjustmentTypeId = null,
        [FromQuery] string? direction = null)
    {
        if (!Request.Query.ContainsKey("branchId"))
            return BadRequest(new { message = "branchId is required." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId < 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            var result = await _stockAdjustmentService.GetPagedAsync(new StockAdjustmentFilterDto
            {
                BusinessId = resolvedBusinessId,
                BranchId = resolvedBranchId,
                Page = page,
                PageSize = pageSize,
                Search = search,
                FromDate = fromDate,
                ToDate = toDate,
                WarehouseId = warehouseId,
                AdjustmentTypeId = adjustmentTypeId,
                Direction = direction
            });

            return Ok(new
            {
                adjustments = result.Data,
                totalRecords = result.TotalRecords,
                totalPages = result.TotalPages,
                currentPage = result.CurrentPage,
                pageSize
            });
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("types")]
    [RequirePermission(PermissionModules.StockAdjustment, PermissionActions.View)]
    public async Task<IActionResult> GetAdjustmentTypes(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);
        var types = await _stockAdjustmentService.GetAdjustmentTypesAsync(resolvedBusinessId, resolvedBranchId);
        return Ok(types);
    }

    [HttpGet("report")]
    [RequirePermission(PermissionModules.StockAdjustment, PermissionActions.View)]
    public async Task<IActionResult> GetStockAdjustmentReport(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int? adjustmentTypeId = null,
        [FromQuery] string? direction = null)
    {
        if (!Request.Query.ContainsKey("branchId"))
            return BadRequest(new { message = "branchId is required." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId < 0)
            return BadRequest(new { message = "branchId is required." });

        var rows = await _stockAdjustmentService.GetReportAsync(new StockAdjustmentFilterDto
        {
            BusinessId = resolvedBusinessId,
            BranchId = resolvedBranchId,
            FromDate = fromDate,
            ToDate = toDate,
            WarehouseId = warehouseId,
            AdjustmentTypeId = adjustmentTypeId,
            Direction = direction
        });

        var gainTotal = rows.Where(r => !r.IsReversed).Sum(r => r.GainAmount);
        var lossTotal = rows.Where(r => !r.IsReversed).Sum(r => r.LossAmount);

        return Ok(new
        {
            rows,
            gainTotal,
            lossTotal,
            netTotal = gainTotal - lossTotal
        });
    }

    [HttpGet("{id:int}")]
    [RequirePermission(PermissionModules.StockAdjustment, PermissionActions.View)]
    public async Task<IActionResult> GetStockAdjustmentById(
        int id,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var adjustment = await _stockAdjustmentService.GetByIdAsync(id, resolvedBusinessId, resolvedBranchId);
        if (adjustment == null)
            return NotFound();

        return Ok(adjustment);
    }

    [HttpPost]
    [RequirePermission(PermissionModules.StockAdjustment, PermissionActions.Create)]
    public async Task<IActionResult> CreateStockAdjustment([FromBody] CreateStockAdjustmentDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        dto.CreatedBy = ResolveUserId();

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var created = await _stockAdjustmentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetStockAdjustmentById), new { id = created.Id, branchId = created.BranchId }, created);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionModules.StockAdjustment, PermissionActions.Edit)]
    public async Task<IActionResult> UpdateStockAdjustment(int id, [FromBody] UpdateStockAdjustmentDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        dto.ModifiedBy = ResolveUserId();

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var updated = await _stockAdjustmentService.UpdateAsync(id, dto);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionModules.StockAdjustment, PermissionActions.Delete)]
    public async Task<IActionResult> DeleteStockAdjustment(
        int id,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId < 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            await _stockAdjustmentService.DeleteAsync(id, resolvedBusinessId, resolvedBranchId, ResolveUserId());
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/reverse")]
    [RequirePermission(PermissionModules.StockAdjustment, PermissionActions.Delete)]
    public async Task<IActionResult> ReverseStockAdjustment(
        int id,
        [FromBody] ReverseStockAdjustmentDto? dto,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId = null)
    {
        dto ??= new ReverseStockAdjustmentDto();
        var resolvedBusinessId = this.ResolveBusinessId(businessId ?? (dto.BusinessId > 0 ? dto.BusinessId : null));
        var resolvedBranchId = this.ResolveBranchId(branchId ?? (dto.BranchId > 0 ? dto.BranchId : null));

        if (resolvedBranchId < 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            var reversed = await _stockAdjustmentService.ReverseAsync(
                id, resolvedBusinessId, resolvedBranchId, ResolveUserId(), dto.Reason);
            return Ok(reversed);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    private int? ResolveUserId()
    {
        var value =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("userId") ??
            User.FindFirstValue("UserId");

        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : null;
    }
}

public class ReverseStockAdjustmentDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public string? Reason { get; set; }
}
