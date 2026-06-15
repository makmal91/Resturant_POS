using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Supplier.DTOs;
using POSSystem.Application.Supplier.Interfaces;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSuppliers(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        if (!Request.Query.ContainsKey("branchId"))
            return BadRequest(new { message = "branchId is required." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId < 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            var result = await _supplierService.GetSuppliersPagedAsync(
                resolvedBusinessId, resolvedBranchId, page, pageSize, search, isActive);

            return Ok(new
            {
                suppliers = result.Data,
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

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSuppliers([FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var items = await _supplierService.GetAllActiveAsync(resolvedBusinessId, resolvedBranchId);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSupplierById(int id, [FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        var supplier = await _supplierService.GetSupplierByIdAsync(id, resolvedBusinessId, resolvedBranchId);
        if (supplier == null) return NotFound();
        return Ok(supplier);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var created = await _supplierService.CreateSupplierAsync(dto);
            return CreatedAtAction(nameof(GetSupplierById),
                new { id = created.Id, branchId = created.BranchId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var updated = await _supplierService.UpdateSupplierAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSupplier(int id, [FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            await _supplierService.DeleteSupplierAsync(id, resolvedBusinessId, resolvedBranchId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
