using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.API.Extensions;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BranchesController : ControllerBase
{
    private readonly POSDbContext _context;
    private readonly ILogger<BranchesController> _logger;

    public BranchesController(POSDbContext context, ILogger<BranchesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public sealed class CreateBranchDto
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public decimal? TaxRate { get; set; }
        public bool? IsActive { get; set; }
        public int BusinessId { get; set; }
        public int CompanyId { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> GetBranches([FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);

        var branches = await _context.Branches
            .Where(b => b.BusinessId == resolvedBusinessId)
            .OrderBy(b => b.Name)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Code,
                b.IsActive,
                b.BusinessId
            })
            .ToListAsync();

        return Ok(branches);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBranchById(int id, [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);

        var branch = await _context.Branches
            .Where(b => b.Id == id && b.BusinessId == resolvedBusinessId)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Code,
                b.Address,
                b.City,
                b.Phone,
                b.Email,
                b.IsActive,
                b.BusinessId
            })
            .FirstOrDefaultAsync();

        if (branch == null)
            return NotFound();

        return Ok(branch);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchDto dto)
    {
        if (dto == null)
            return BadRequest("Request body is null");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Branch name is required");

        if (string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest("Branch code is required");

        var resolvedBusinessId = dto.BusinessId > 0
            ? dto.BusinessId
            : (dto.CompanyId > 0 ? dto.CompanyId : this.ResolveBusinessId());

        if (resolvedBusinessId <= 0)
            return BadRequest("BusinessId is required");

        try
        {
            var normalizedCode = dto.Code.Trim().ToUpperInvariant();

            var companyExists = await _context.Businesses
                .AnyAsync(b => b.Id == resolvedBusinessId);

            if (!companyExists)
                return BadRequest(new { message = "Invalid BusinessId. Business does not exist." });

            var codeExists = await _context.Branches
                .AnyAsync(b => b.Code == normalizedCode);

            if (codeExists)
                return Conflict(new { message = "Branch code already exists." });

            var branch = new Branch
            {
                Name = dto.Name.Trim(),
                Code = normalizedCode,
                Address = dto.Address?.Trim() ?? string.Empty,
                City = dto.City?.Trim() ?? string.Empty,
                Phone = dto.Phone?.Trim() ?? string.Empty,
                Email = dto.Email?.Trim() ?? string.Empty,
                OpeningTime = new TimeSpan(8, 0, 0),
                ClosingTime = new TimeSpan(23, 0, 0),
                TaxRate = dto.TaxRate ?? 0m,
                IsActive = dto.IsActive ?? true,
                BusinessId = resolvedBusinessId,
                BranchId = 1
            };

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBranchById), new { id = branch.Id }, new
            {
                branch.Id,
                branch.Name,
                branch.Code,
                branch.Address,
                branch.City,
                branch.Phone,
                branch.Email,
                branch.TaxRate,
                branch.IsActive,
                businessId = branch.BusinessId,
                companyId = branch.BusinessId
            });
        }
        catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message.Contains("idx_branch_code", StringComparison.OrdinalIgnoreCase) == true)
        {
            _logger.LogWarning(dbEx, "Duplicate branch code while creating branch.");
            return Conflict(new { message = "Branch code already exists." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating branch");

            return StatusCode(500, new
            {
                message = "Internal server error while creating branch",
                detail = ex.Message
            });
        }
    }
}
