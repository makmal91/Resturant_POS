using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Branch.DTOs;
using POSSystem.Application.Branch.Interfaces;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;
    private readonly ILogger<BranchesController> _logger;

    public BranchesController(IBranchService branchService, ILogger<BranchesController> logger)
    {
        _branchService = branchService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetBranches(
        [FromQuery] int? businessId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var result = await _branchService.GetBranchesPagedAsync(
            resolvedBusinessId,
            page,
            pageSize,
            search,
            sortBy,
            sortDirection);

        return Ok(new
        {
            branches = result.Data,
            totalRecords = result.TotalRecords,
            totalPages = result.TotalPages,
            currentPage = result.CurrentPage,
            pageSize
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBranchById(int id, [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var branch = await _branchService.GetBranchByIdAsync(id, resolvedBusinessId);
        if (branch == null)
            return NotFound();

        return Ok(branch);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchDto dto)
    {
        if (dto == null)
            return BadRequest("Request body is null");

        try
        {
            var resolvedBusinessId = this.ResolveBusinessId();
            var branch = await _branchService.CreateBranchAsync(dto, resolvedBusinessId);
            return CreatedAtAction(nameof(GetBranchById), new { id = branch.Id }, branch);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { message = ex.Message });

            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating branch");
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return StatusCode(500, new { message = "Internal server error while creating branch", detail = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateBranch(int id, [FromBody] UpdateBranchDto dto, [FromQuery] int? businessId = null)
    {
        if (dto == null)
            return BadRequest("Request body is null");

        try
        {
            var resolvedBusinessId = this.ResolveBusinessId(businessId);
            var branch = await _branchService.UpdateBranchAsync(id, dto, resolvedBusinessId);
            if (branch == null)
                return NotFound();

            return Ok(branch);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { message = ex.Message });

            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating branch {BranchId}", id);
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return StatusCode(500, new { message = "Internal server error while updating branch", detail = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBranch(int id, [FromQuery] int? businessId = null)
    {
        try
        {
            var resolvedBusinessId = this.ResolveBusinessId(businessId);
            var deleted = await _branchService.DeleteBranchAsync(id, resolvedBusinessId);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting branch {BranchId}", id);
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return StatusCode(500, new { message = "Internal server error while deleting branch", detail = ex.Message });
        }
    }
}
