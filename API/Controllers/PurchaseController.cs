using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Purchase.DTOs;
using POSSystem.Application.Purchase.Interfaces;
using POSSystem.Application.Sales.DTOs;
using POSSystem.Domain;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PurchaseController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;

    public PurchaseController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPurchases(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] PurchaseStatus? status = null)
    {
        if (!Request.Query.ContainsKey("branchId"))
            return BadRequest(new { message = "branchId is required." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId < 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            var result = await _purchaseService.GetPurchasesPagedAsync(
                resolvedBusinessId, resolvedBranchId, page, pageSize, search, status);

            return Ok(new
            {
                purchases = result.Data,
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
    public async Task<IActionResult> GetPurchaseById(int id, [FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var purchase = await _purchaseService.GetPurchaseByIdAsync(id, resolvedBusinessId, resolvedBranchId);
        if (purchase == null) return NotFound();
        return Ok(purchase);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePurchase([FromBody] CreatePurchaseDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var created = await _purchaseService.CreatePurchaseAsync(dto);
            return CreatedAtAction(nameof(GetPurchaseById),
                new { id = created.Id, branchId = created.BranchId }, created);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePurchase(int id, [FromBody] UpdatePurchaseDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var updated = await _purchaseService.UpdatePurchaseAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/post")]
    public async Task<IActionResult> PostPurchase(int id, [FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var dto = new PostPurchaseDto
        {
            BusinessId = this.ResolveBusinessId(businessId),
            BranchId = this.ResolveBranchId(branchId)
        };

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            var result = await _purchaseService.PostPurchaseAsync(id, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePurchase(int id, [FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            await _purchaseService.DeletePurchaseAsync(id, resolvedBusinessId, resolvedBranchId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    // ─── Transaction Correction ────────────────────────────────────────────────

    /// <summary>
    /// Void a posted purchase.
    /// Creates PurchaseReversal ledger entries to remove stock and marks purchase as Cancelled.
    /// </summary>
    [HttpPost("{id:int}/void")]
    public async Task<IActionResult> VoidPurchase(int id, [FromBody] VoidPurchaseDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId   = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            var result = await _purchaseService.VoidPurchaseAsync(id, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get stock ledger history for a specific purchase.
    /// Shows all PurchaseEntry and PurchaseReversal entries linked to this purchase.
    /// </summary>
    [HttpGet("{id:int}/ledger")]
    public async Task<IActionResult> GetPurchaseLedgerHistory(
        int id, [FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId   = this.ResolveBranchId(branchId);

        var entries = await _purchaseService.GetPurchaseLedgerHistoryAsync(
            id, resolvedBusinessId, resolvedBranchId);

        return Ok(entries);
    }
}
