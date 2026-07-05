using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Authorization;
using POSSystem.API.Extensions;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.StockTransfer.DTOs;
using POSSystem.Application.StockTransfer.Interfaces;
using System.Security.Claims;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/stock-transfer")]
public class StockTransferController : ControllerBase
{
    private readonly IStockTransferService _stockTransferService;

    public StockTransferController(IStockTransferService stockTransferService)
    {
        _stockTransferService = stockTransferService;
    }

    [HttpGet]
    [RequirePermission(PermissionModules.StockTransfer, PermissionActions.View)]
    public async Task<IActionResult> GetStockTransfers(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null)
    {
        if (!Request.Query.ContainsKey("branchId"))
            return BadRequest(new { message = "branchId is required." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId < 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            var result = await _stockTransferService.GetPagedAsync(
                resolvedBusinessId, resolvedBranchId, page, pageSize, search);

            return Ok(new
            {
                transfers = result.Data,
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

    [HttpGet("{id:int}")]
    [RequirePermission(PermissionModules.StockTransfer, PermissionActions.View)]
    public async Task<IActionResult> GetStockTransferById(
        int id,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var transfer = await _stockTransferService.GetByIdAsync(id, resolvedBusinessId, resolvedBranchId);
        if (transfer == null)
            return NotFound();

        return Ok(transfer);
    }

    [HttpPost]
    [RequirePermission(PermissionModules.StockTransfer, PermissionActions.Create)]
    public async Task<IActionResult> CreateStockTransfer([FromBody] CreateStockTransferVoucherDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        dto.CreatedBy = ResolveUserId();

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var created = await _stockTransferService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetStockTransferById), new { id = created.Id, branchId = created.BranchId }, created);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionModules.StockTransfer, PermissionActions.Edit)]
    public async Task<IActionResult> UpdateStockTransfer(int id, [FromBody] UpdateStockTransferVoucherDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        dto.ModifiedBy = ResolveUserId();

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var updated = await _stockTransferService.UpdateAsync(id, dto);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/reverse")]
    [RequirePermission(PermissionModules.StockTransfer, PermissionActions.Delete)]
    public async Task<IActionResult> ReverseStockTransfer(
        int id,
        [FromBody] ReverseStockTransferVoucherDto? dto,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId = null)
    {
        dto ??= new ReverseStockTransferVoucherDto();
        dto.BusinessId = this.ResolveBusinessId(businessId ?? (dto.BusinessId > 0 ? dto.BusinessId : null));
        dto.BranchId = this.ResolveBranchId(branchId ?? (dto.BranchId > 0 ? dto.BranchId : null));
        dto.ReversedBy = ResolveUserId();

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            var reversed = await _stockTransferService.ReverseAsync(id, dto);
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
