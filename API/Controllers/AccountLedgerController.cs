using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Authorization;
using POSSystem.API.Extensions;
using POSSystem.Application.Accounting.DTOs;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Application.Common.Constants;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/accounting")]
public class AccountLedgerController : ControllerBase
{
    private readonly IAccountLedgerService _accountLedger;

    public AccountLedgerController(IAccountLedgerService accountLedger)
    {
        _accountLedger = accountLedger;
    }

    [HttpGet("accounts")]
    [RequirePermission(PermissionModules.AccountLedger, PermissionActions.View)]
    public async Task<IActionResult> ListAccounts(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        var biz = this.ResolveBusinessId(businessId);
        var accounts = await _accountLedger.ListAccountsAsync();
        return Ok(accounts);
    }

    [HttpGet("ledger")]
    [RequirePermission(PermissionModules.AccountLedger, PermissionActions.View)]
    public async Task<IActionResult> GetAccountLedger(
        [FromQuery] int accountId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool auditView = false,
        [FromQuery] bool groupByChain = false)
    {
        if (accountId <= 0)
            return BadRequest(new { message = "AccountId is required." });

        var resolvedBranch = this.ResolveBranchId(branchId);

        var filter = new AccountLedgerFilterDto
        {
            AccountId = accountId,
            BusinessId = this.ResolveBusinessId(businessId),
            BranchId = resolvedBranch > 0 ? resolvedBranch : null,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize,
            AuditView = auditView,
            GroupByChain = groupByChain,
        };

        try
        {
            var result = await _accountLedger.GetAccountLedgerAsync(filter);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
