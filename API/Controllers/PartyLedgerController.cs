using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Ledger.DTOs;
using POSSystem.Application.Ledger.Interfaces;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/ledger")]
public class PartyLedgerController : ControllerBase
{
    private readonly IPartyLedgerService _partyLedgerService;

    public PartyLedgerController(IPartyLedgerService partyLedgerService)
    {
        _partyLedgerService = partyLedgerService;
    }

    // ─── Customer Ledger ───────────────────────────────────────────────────────

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomerLedger(
        [FromQuery] int customerId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (customerId <= 0)
            return BadRequest(new { message = "CustomerId is required." });

        var filter = new PartyLedgerFilterDto
        {
            BusinessId = this.ResolveBusinessId(businessId),
            BranchId = this.ResolveBranchId(branchId),
            PartyId = customerId,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        };

        if (filter.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var result = await _partyLedgerService.GetCustomerLedgerAsync(filter);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("customers/{customerId:int}/balance")]
    public async Task<IActionResult> GetCustomerBalance(
        int customerId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        if (branch <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var result = await _partyLedgerService.GetCustomerBalanceAsync(customerId, biz, branch);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("customers/payment")]
    public async Task<IActionResult> ReceiveCustomerPayment([FromBody] ReceiveCustomerPaymentDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (dto.CustomerId <= 0)
            return BadRequest(new { message = "CustomerId is required." });

        try
        {
            var result = await _partyLedgerService.ReceiveCustomerPaymentAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ─── Supplier Ledger ───────────────────────────────────────────────────────

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSupplierLedger(
        [FromQuery] int supplierId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (supplierId <= 0)
            return BadRequest(new { message = "SupplierId is required." });

        var filter = new PartyLedgerFilterDto
        {
            BusinessId = this.ResolveBusinessId(businessId),
            BranchId = this.ResolveBranchId(branchId),
            PartyId = supplierId,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        };

        if (filter.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var result = await _partyLedgerService.GetSupplierLedgerAsync(filter);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("suppliers/{supplierId:int}/balance")]
    public async Task<IActionResult> GetSupplierBalance(
        int supplierId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        if (branch <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var result = await _partyLedgerService.GetSupplierBalanceAsync(supplierId, biz, branch);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("suppliers/payment")]
    public async Task<IActionResult> PaySupplier([FromBody] PaySupplierDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (dto.SupplierId <= 0)
            return BadRequest(new { message = "SupplierId is required." });

        try
        {
            var result = await _partyLedgerService.PaySupplierAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
