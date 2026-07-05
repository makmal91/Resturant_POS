using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.CashFlow.DTOs;
using POSSystem.Application.CashFlow.Interfaces;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/cashflow/registers")]
public class PosRegistersController : ControllerBase
{
    private readonly IPosRegisterService _registers;

    public PosRegistersController(IPosRegisterService registers) => _registers = registers;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] int? branchId)
    {
        var branch = this.ResolveBranchId(branchId);
        if (branch <= 0) return BadRequest(new { message = "BranchId is required." });
        return Ok(await _registers.GetDashboardAsync(branch));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? branchId)
    {
        var branch = this.ResolveBranchId(branchId);
        if (branch <= 0) return BadRequest(new { message = "BranchId is required." });
        return Ok(await _registers.GetRegistersAsync(branch));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePosRegisterRequest request)
    {
        request.BranchId = this.ResolveBranchId(request.BranchId > 0 ? request.BranchId : null);
        if (request.BranchId <= 0) return BadRequest(new { message = "BranchId is required." });

        var userId = this.ResolveUserId() ?? 0;
        try
        {
            return Ok(await _registers.CreateRegisterAsync(request, userId));
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePosRegisterRequest request)
    {
        var branch = this.ResolveBranchId(null);
        if (branch <= 0) return BadRequest(new { message = "BranchId is required." });

        var userId = this.ResolveUserId() ?? 0;
        try
        {
            return Ok(await _registers.UpdateRegisterAsync(id, request, branch, userId));
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{registerId:int}/opening-hint")]
    public async Task<IActionResult> GetOpeningHint(int registerId, [FromQuery] int? branchId)
    {
        var branch = this.ResolveBranchId(branchId);
        if (branch <= 0) return BadRequest(new { message = "BranchId is required." });

        try
        {
            return Ok(await _registers.GetOpeningHintAsync(registerId, branch));
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("open")]
    public async Task<IActionResult> Open([FromBody] OpenRegisterRequest request)
    {
        var branch = this.ResolveBranchId(null);
        if (branch <= 0) return BadRequest(new { message = "BranchId is required." });

        var userId = this.ResolveUserId() ?? 0;
        try
        {
            return Ok(await _registers.OpenRegisterAsync(request, branch, userId));
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{registerId:int}/close-preview")]
    public async Task<IActionResult> GetClosePreview(int registerId, [FromQuery] int? branchId)
    {
        var branch = this.ResolveBranchId(branchId);
        if (branch <= 0) return BadRequest(new { message = "BranchId is required." });

        try
        {
            return Ok(await _registers.GetClosePreviewAsync(registerId, branch));
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("close")]
    public async Task<IActionResult> Close([FromBody] CloseRegisterRequest request)
    {
        var branch = this.ResolveBranchId(null);
        if (branch <= 0) return BadRequest(new { message = "BranchId is required." });

        var userId = this.ResolveUserId() ?? 0;
        try
        {
            return Ok(await _registers.CloseRegisterAsync(request, branch, userId));
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int? branchId,
        [FromQuery] int? posRegisterId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var branch = this.ResolveBranchId(branchId);
        if (branch <= 0) return BadRequest(new { message = "BranchId is required." });

        var filter = new RegisterHistoryFilter
        {
            PosRegisterId = posRegisterId,
            From = from,
            To = to,
            Page = page,
            PageSize = pageSize,
        };

        var result = await _registers.GetHistoryAsync(branch, filter);
        return Ok(new
        {
            items = result.Items,
            totalRecords = result.TotalRecords,
            totalPages = result.TotalPages,
            currentPage = page,
            pageSize,
        });
    }
}
