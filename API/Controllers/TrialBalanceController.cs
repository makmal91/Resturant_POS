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
[Route("api/reports/trial-balance")]
public class TrialBalanceController : ControllerBase
{
    private readonly ITrialBalanceService _trialBalance;

    public TrialBalanceController(ITrialBalanceService trialBalance) => _trialBalance = trialBalance;

    [HttpGet]
    [RequirePermission(PermissionModules.TrialBalanceReport, PermissionActions.View)]
    public async Task<IActionResult> GetTrialBalance(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] TrialBalanceAccountLevel accountLevel = TrialBalanceAccountLevel.ParentAndChild,
        [FromQuery] bool showZeroBalance = false)
    {
        var resolvedBranch = this.ResolveBranchId(branchId);

        var filter = new TrialBalanceFilterDto
        {
            BusinessId = this.ResolveBusinessId(businessId),
            BranchId = resolvedBranch > 0 ? resolvedBranch : null,
            FromDate = fromDate,
            ToDate = toDate,
            AccountLevel = accountLevel,
            ShowZeroBalance = showZeroBalance,
        };

        var result = await _trialBalance.GetTrialBalanceAsync(filter);
        return Ok(result);
    }
}
