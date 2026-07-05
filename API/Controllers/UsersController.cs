using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.API.Extensions;
using POSSystem.Application.Users.DTOs;
using POSSystem.Application.Users.Interfaces;
using POSSystem.Domain;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null)
    {
        if (!Request.Query.ContainsKey("branchId"))
            return BadRequest(new { message = "branchId is required." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId < 0)
            return BadRequest(new { message = "branchId is required." });

        if (resolvedBranchId == 0 && !IsGlobalAdminRequest())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Global user view is available to admins only." });

        try
        {
            var result = await _userService.GetUsersPagedAsync(
                resolvedBusinessId,
                resolvedBranchId,
                page,
                pageSize,
                search,
                sortBy,
                sortDirection,
                IsGlobalAdminRequest());

            return Ok(new
            {
                data = result.Data,
                totalRecords = result.TotalRecords,
                totalPages = result.TotalPages,
                currentPage = result.CurrentPage,
                pageSize
            });
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUserById(int id, [FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId ?? 1);

        var user = await _userService.GetUserByIdAsync(id, resolvedBusinessId, resolvedBranchId, IsGlobalAdminRequest());
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (IsGlobalReadOnlyRequest())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Create is disabled in global view." });

        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);

        if (dto.BranchIds.Count == 0)
        {
            var headerBranchId = this.ResolveBranchId(null);
            if (headerBranchId > 0)
                dto.BranchIds = new List<int> { headerBranchId };
        }

        if (dto.BranchIds.Count == 0)
            return BadRequest(new { message = "At least one branch must be assigned to the user." });

        try
        {
            var created = await _userService.CreateUserAsync(dto, IsGlobalAdminRequest(), ResolveRoleName());
            return CreatedAtAction(nameof(GetUserById), new { id = created.Id, branchId = created.PrimaryBranchId }, created);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            return BadRequest(new { message = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto, [FromQuery] int? branchId)
    {
        if (IsGlobalReadOnlyRequest())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Edit is disabled in global view." });

        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        var resolvedBranchId = this.ResolveBranchId(branchId ?? 1);

        try
        {
            var updated = await _userService.UpdateUserAsync(id, dto, resolvedBranchId, IsGlobalAdminRequest(), ResolveRoleName());
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, [FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        if (IsGlobalReadOnlyRequest())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Delete is disabled in global view." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId ?? 1);

        try
        {
            await _userService.DeleteUserAsync(id, resolvedBusinessId, resolvedBranchId, IsGlobalAdminRequest(), ResolveRoleName());
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/branches")]
    public async Task<IActionResult> GetUserBranches(int id, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var branches = await _userService.GetUserBranchesAsync(id, resolvedBusinessId);
        return Ok(branches);
    }

    [HttpPost("{id:int}/branches")]
    public async Task<IActionResult> AssignUserBranches(int id, [FromBody] AssignUserBranchesDto dto, [FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        if (IsGlobalReadOnlyRequest())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Branch assignment is disabled in global view." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId ?? 1);

        try
        {
            await _userService.AssignUserBranchesAsync(id, dto, resolvedBusinessId, resolvedBranchId, IsGlobalAdminRequest());
            var branches = await _userService.GetUserBranchesAsync(id, resolvedBusinessId);
            return Ok(branches);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}/branches/{branchId:int}")]
    public async Task<IActionResult> RemoveUserBranch(int id, int branchId, [FromQuery] int? requestBranchId, [FromQuery] int? businessId)
    {
        if (IsGlobalReadOnlyRequest())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Branch assignment is disabled in global view." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedRequestBranchId = this.ResolveBranchId(requestBranchId ?? branchId);

        try
        {
            await _userService.RemoveUserBranchAsync(id, branchId, resolvedBusinessId, resolvedRequestBranchId, IsGlobalAdminRequest());
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool IsGlobalAdminRequest()
    {
        var role = ResolveRoleName();
        return RoleNames.HasGlobalBranchAccess(role ?? string.Empty);
    }

    private bool IsGlobalReadOnlyRequest()
    {
        var resolvedBranchId = this.ResolveBranchId(null);
        if (resolvedBranchId != 0)
            return false;

        return !IsGlobalAdminRequest();
    }

    private string? ResolveRoleName()
    {
        return User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ??
               Request.Headers["X-User-Role"].FirstOrDefault();
    }
}
