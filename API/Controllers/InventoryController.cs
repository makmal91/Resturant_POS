using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Inventory.DTOs;
using POSSystem.Application.Inventory.Interfaces;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetInventory([FromQuery] int branchId, [FromQuery] int? businessId = null)
    {
        try
        {
            var resolvedBusinessId = this.ResolveBusinessId(businessId);
            var resolvedBranchId = this.ResolveBranchId(branchId);
            var items = await _inventoryService.GetInventoryItemsAsync(resolvedBusinessId, resolvedBranchId);
            return Ok(new { items });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase([FromBody] AddStockDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
            dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
            await _inventoryService.AddStockAsync(dto);
            return Ok(new { message = "Stock added successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> Adjust([FromBody] AdjustStockDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
            dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
            await _inventoryService.AdjustStockAsync(dto);
            return Ok(new { message = "Stock adjustment applied." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
