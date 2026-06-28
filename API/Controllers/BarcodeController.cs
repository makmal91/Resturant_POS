using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Barcode.DTOs;
using POSSystem.Application.Barcode.Interfaces;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/barcode")]
public class BarcodeController : ControllerBase
{
    private readonly IBarcodePrintService _barcodePrintService;

    public BarcodeController(IBarcodePrintService barcodePrintService)
    {
        _barcodePrintService = barcodePrintService;
    }

    [HttpGet("items")]
    public async Task<IActionResult> GetPrintItems(
        [FromQuery] int branchId,
        [FromQuery] int? businessId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int? subCategoryId = null,
        [FromQuery] int? brandId = null,
        [FromQuery] bool inStock = false)
    {
        if (branchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var result = await _barcodePrintService.SearchItemsAsync(new BarcodePrintSearchRequestDto
        {
            BusinessId = resolvedBusinessId,
            BranchId = resolvedBranchId,
            Page = page,
            PageSize = pageSize,
            Search = search,
            CategoryId = categoryId > 0 ? categoryId : null,
            SubCategoryId = subCategoryId > 0 ? subCategoryId : null,
            BrandId = brandId > 0 ? brandId : null,
            InStockOnly = inStock
        });

        return Ok(new
        {
            items = result.Data,
            totalRecords = result.TotalRecords,
            totalPages = result.TotalPages,
            currentPage = result.CurrentPage,
            pageSize
        });
    }
}
