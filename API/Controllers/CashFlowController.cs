using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.CashFlow.DTOs;
using POSSystem.Application.CashFlow.Interfaces;
using POSSystem.Domain;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/cashflow")]
public class CashFlowController : ControllerBase
{
    private readonly ICashFlowService _cashFlowService;

    public CashFlowController(ICashFlowService cashFlowService)
    {
        _cashFlowService = cashFlowService;
    }

    // ─── Cash Register ─────────────────────────────────────────────────────────

    /// <summary>Open the cash register for today (set opening balance).</summary>
    [HttpPost("opening")]
    public async Task<IActionResult> OpenCash([FromBody] OpeningCashDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId   = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var result = await _cashFlowService.OpenCashAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Close cash register for today (enter actual cash, calc difference).</summary>
    [HttpPost("closing")]
    public async Task<IActionResult> CloseCash([FromBody] ClosingCashDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId   = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var result = await _cashFlowService.CloseCashAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Get today's register status for the active branch.</summary>
    [HttpGet("register/today")]
    public async Task<IActionResult> GetTodayRegister([FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var biz    = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        var result = await _cashFlowService.GetTodayRegisterAsync(biz, branch);
        return Ok(result);
    }

    // ─── Transactions ──────────────────────────────────────────────────────────

    /// <summary>Record a manual cash transaction (CashIn / CashOut / BankTransfer).</summary>
    [HttpPost("transaction")]
    public async Task<IActionResult> RecordTransaction([FromBody] RecordCashTransactionDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId   = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var result = await _cashFlowService.RecordTransactionAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get paginated cash flow ledger.
    /// Filters: branchId, fromDate, toDate, transactionType, paymentMethod, page, pageSize.
    /// </summary>
    [HttpGet("ledger")]
    public async Task<IActionResult> GetLedger(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] CashFlowTransactionType? transactionType,
        [FromQuery] CashFlowPaymentMethod? paymentMethod,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var filter = new CashFlowLedgerFilterDto
        {
            BusinessId      = this.ResolveBusinessId(businessId),
            BranchId        = this.ResolveBranchId(branchId),
            FromDate        = fromDate,
            ToDate          = toDate,
            TransactionType = transactionType,
            PaymentMethod   = paymentMethod,
            Page            = page,
            PageSize        = pageSize,
        };

        var result = await _cashFlowService.GetLedgerAsync(filter);
        return Ok(new
        {
            transactions = result.Transactions,
            totalRecords = result.TotalRecords,
            totalPages   = result.TotalPages,
            currentPage  = result.CurrentPage,
            pageSize     = result.PageSize,
            totalIn      = result.TotalIn,
            totalOut     = result.TotalOut,
            netTotal     = result.NetTotal,
        });
    }

    // ─── Summaries ─────────────────────────────────────────────────────────────

    /// <summary>Daily cash summary for a specific branch and date.</summary>
    [HttpGet("summary/daily")]
    public async Task<IActionResult> GetDailySummary(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] DateTime? date)
    {
        var biz    = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        var result = await _cashFlowService.GetDailySummaryAsync(biz, branch, date);
        return Ok(result);
    }

    /// <summary>Monthly cash summary with daily trend data.</summary>
    [HttpGet("summary/monthly")]
    public async Task<IActionResult> GetMonthlySummary(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int? year,
        [FromQuery] int? month)
    {
        var biz    = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        var result = await _cashFlowService.GetMonthlySummaryAsync(biz, branch, year, month);
        return Ok(result);
    }

    /// <summary>All-branch cash summary for a given date (head-office view).</summary>
    [HttpGet("summary/branch")]
    public async Task<IActionResult> GetBranchSummary(
        [FromQuery] int? businessId,
        [FromQuery] DateTime? date)
    {
        var biz    = this.ResolveBusinessId(businessId);
        var result = await _cashFlowService.GetAllBranchesSummaryAsync(biz, date);
        return Ok(result);
    }
}
