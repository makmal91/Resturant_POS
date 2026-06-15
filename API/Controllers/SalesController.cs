using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Sales.DTOs;
using POSSystem.Application.Sales.Interfaces;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISalesService _salesService;

    public SalesController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    /// <summary>
    /// Look up a product by barcode — optimised for scanner input (&lt;200ms target).
    /// </summary>
    [HttpGet("product/barcode/{barcode}")]
    public async Task<IActionResult> GetProductByBarcode(string barcode, [FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return BadRequest(new { message = "Barcode is required." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var product = await _salesService.GetProductByBarcodeAsync(barcode.Trim(), resolvedBusinessId, resolvedBranchId);
        if (product == null)
            return NotFound(new { message = $"No product found for barcode '{barcode}'." });

        return Ok(product);
    }

    /// <summary>
    /// Search products by name / code / SKU / barcode.
    /// </summary>
    [HttpGet("products/search")]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string q,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new List<PosProductLookupDto>());

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var results = await _salesService.SearchProductsAsync(q, resolvedBusinessId, resolvedBranchId);
        return Ok(results);
    }

    /// <summary>
    /// Grouped product search — returns parent + all active variants with live stock.
    /// Used by POS search dropdown.
    /// </summary>
    [HttpGet("products/search-grouped")]
    public async Task<IActionResult> SearchProductsGrouped(
        [FromQuery] string q,
        [FromQuery] int? warehouseId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new List<PosSearchGroupDto>());

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var results = await _salesService.SearchProductsGroupedAsync(q, resolvedBusinessId, resolvedBranchId, warehouseId);
        return Ok(results);
    }

    /// <summary>
    /// Search customers by name or phone.
    /// </summary>
    [HttpGet("customers/search")]
    public async Task<IActionResult> SearchCustomers(
        [FromQuery] string q,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new List<PosCustomerDto>());

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var results = await _salesService.SearchCustomersAsync(q, resolvedBusinessId, resolvedBranchId);
        return Ok(results);
    }

    /// <summary>
    /// Create a completed sale invoice and deduct stock.
    /// </summary>
    [HttpPost("invoice")]
    public async Task<IActionResult> CreateSaleInvoice([FromBody] CreateSaleInvoiceDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var result = await _salesService.CreateSaleInvoiceAsync(dto);
            return CreatedAtAction(nameof(GetInvoiceById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get invoice by ID.
    /// </summary>
    [HttpGet("invoice/{id:int}")]
    public async Task<IActionResult> GetInvoiceById(int id, [FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var invoice = await _salesService.GetInvoiceByIdAsync(id, resolvedBusinessId, resolvedBranchId);
        if (invoice == null) return NotFound();
        return Ok(invoice);
    }

    /// <summary>
    /// Hold the current bill (save cart for later).
    /// </summary>
    [HttpPost("hold")]
    public async Task<IActionResult> HoldBill([FromBody] HoldBillDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var result = await _salesService.HoldBillAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all currently held bills.
    /// </summary>
    [HttpGet("held")]
    public async Task<IActionResult> GetHeldBills([FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var results = await _salesService.GetHeldBillsAsync(resolvedBusinessId, resolvedBranchId);
        return Ok(results);
    }

    /// <summary>
    /// Cancel a held bill.
    /// </summary>
    [HttpDelete("held/{id:int}")]
    public async Task<IActionResult> CancelHeldBill(int id, [FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        try
        {
            await _salesService.CancelHeldBillAsync(id, resolvedBusinessId, resolvedBranchId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
