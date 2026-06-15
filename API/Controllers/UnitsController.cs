using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Unit.DTOs;
using POSSystem.Application.Unit.Interfaces;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UnitsController : ControllerBase
{
    private readonly IUnitService _unitService;

    public UnitsController(IUnitService unitService)
    {
        _unitService = unitService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUnits([FromQuery] int? branchId, [FromQuery] int? businessId, [FromQuery] bool? status = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId < 0)
            return BadRequest(new { message = "branchId is required." });

        var units = await _unitService.GetUnitsAsync(resolvedBusinessId, resolvedBranchId, status);
        return Ok(new { units });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUnitById(int id, [FromQuery] int? branchId, [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);
        var unit = await _unitService.GetUnitByIdAsync(id, resolvedBusinessId, resolvedBranchId);
        if (unit == null)
            return NotFound();

        return Ok(unit);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUnit([FromBody] CreateUnitDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        try
        {
            var unit = await _unitService.CreateUnitAsync(dto);
            return CreatedAtAction(nameof(GetUnitById), new { id = unit.Id, branchId = unit.BranchId }, unit);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUnit(int id, [FromBody] UpdateUnitDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        try
        {
            return Ok(await _unitService.UpdateUnitAsync(id, dto));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUnit(int id, [FromQuery] int? branchId, [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        try
        {
            await _unitService.DeleteUnitAsync(id, resolvedBusinessId, resolvedBranchId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
