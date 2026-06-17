using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Stock.DTOs;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Domain;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> GetLedger(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int? productId,
        [FromQuery] int? variantId,
        [FromQuery] int? warehouseId,
        [FromQuery] StockLedgerType? type,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var filter = new StockLedgerFilterDto
        {
            BusinessId = resolvedBusinessId,
            BranchId = resolvedBranchId,
            ProductId = productId,
            VariantId = variantId,
            WarehouseId = warehouseId,
            Type = type,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Page = page,
            PageSize = pageSize
        };

        try
        {
            var result = await _stockService.GetLedgerAsync(filter);
            return Ok(new
            {
                entries = result.Data,
                totalRecords = result.TotalRecords,
                totalPages = result.TotalPages,
                currentPage = result.CurrentPage,
                pageSize
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("balances")]
    public async Task<IActionResult> GetStockBalances(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int? warehouseId,
        [FromQuery] int? productId,
        [FromQuery] int? variantId,
        [FromQuery] bool variantWise = false)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        try
        {
            var balances = await _stockService.GetStockBalancesAsync(
                resolvedBusinessId, resolvedBranchId, warehouseId, productId, variantId, variantWise);
            return Ok(balances);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentStock(
        [FromQuery] int productId,
        [FromQuery] int warehouseId,
        [FromQuery] int? variantId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        if (productId <= 0) return BadRequest(new { message = "productId is required." });
        if (warehouseId <= 0) return BadRequest(new { message = "warehouseId is required." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var qty = await _stockService.GetCurrentStockAsync(
            resolvedBusinessId, resolvedBranchId, productId, variantId, warehouseId);

        return Ok(new { productId, variantId, warehouseId, quantity = qty });
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> TransferStock([FromBody] StockTransferDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            await _stockService.TransferStockAsync(dto);
            return Ok(new { message = "Stock transferred successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
