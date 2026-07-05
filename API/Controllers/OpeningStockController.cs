using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Authorization;
using POSSystem.API.Extensions;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.OpeningStock.DTOs;
using POSSystem.Application.OpeningStock.Interfaces;
using System.Security.Claims;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/opening-stock")]
public class OpeningStockController : ControllerBase
{
    private readonly IOpeningStockService _openingStockService;

    public OpeningStockController(IOpeningStockService openingStockService)
    {
        _openingStockService = openingStockService;
    }

    [HttpGet]
    [RequirePermission(PermissionModules.OpeningStock, PermissionActions.View)]
    public async Task<IActionResult> GetOpeningStockVouchers(
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
            var result = await _openingStockService.GetPagedAsync(
                resolvedBusinessId, resolvedBranchId, page, pageSize, search);

            return Ok(new
            {
                vouchers = result.Data,
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
    [RequirePermission(PermissionModules.OpeningStock, PermissionActions.View)]
    public async Task<IActionResult> GetOpeningStockVoucherById(
        int id,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var voucher = await _openingStockService.GetByIdAsync(id, resolvedBusinessId, resolvedBranchId);
        if (voucher == null)
            return NotFound();

        return Ok(voucher);
    }

    [HttpPost]
    [RequirePermission(PermissionModules.OpeningStock, PermissionActions.Create)]
    public async Task<IActionResult> CreateOpeningStockVoucher([FromBody] CreateOpeningStockVoucherDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        dto.CreatedBy = ResolveUserId();

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var created = await _openingStockService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetOpeningStockVoucherById), new { id = created.Id, branchId = created.BranchId }, created);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionModules.OpeningStock, PermissionActions.Edit)]
    public async Task<IActionResult> UpdateOpeningStockVoucher(
        int id,
        [FromBody] UpdateOpeningStockVoucherDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        dto.ModifiedBy = ResolveUserId();

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var updated = await _openingStockService.UpdateAsync(id, dto);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/reverse")]
    [RequirePermission(PermissionModules.OpeningStock, PermissionActions.Delete)]
    public async Task<IActionResult> ReverseOpeningStockVoucher(
        int id,
        [FromBody] ReverseOpeningStockVoucherDto? dto,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId = null)
    {
        dto ??= new ReverseOpeningStockVoucherDto();
        dto.BusinessId = this.ResolveBusinessId(businessId ?? (dto.BusinessId > 0 ? dto.BusinessId : null));
        dto.BranchId = this.ResolveBranchId(branchId ?? (dto.BranchId > 0 ? dto.BranchId : null));
        dto.ReversedBy = ResolveUserId();

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            var reversed = await _openingStockService.ReverseAsync(id, dto);
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
