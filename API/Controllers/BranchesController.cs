using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public string? Currency { get; set; }
        public bool? IsActive { get; set; }
        public int CompanyId { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> GetBranches()
    {
        var branches = await _context.Branches
            .OrderBy(b => b.Name)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Code,
                b.IsActive
            })
            .ToListAsync();

        return Ok(branches);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBranchById(int id)
    {
        var branch = await _context.Branches
            .Where(b => b.Id == id)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Code,
                b.Address,
                b.City,
                b.Phone,
                b.Email,
                b.IsActive
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

        if (dto.CompanyId <= 0)
            return BadRequest("CompanyId is required");

        try
        {
            var normalizedCode = dto.Code.Trim().ToUpperInvariant();

            var companyExists = await _context.Branches
                .AnyAsync(b => b.Id == dto.CompanyId);

            if (!companyExists)
                return BadRequest(new { message = "Invalid CompanyId. Company does not exist." });

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
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "USD" : dto.Currency.Trim().ToUpperInvariant(),
                IsActive = dto.IsActive ?? true,
                BranchId = dto.CompanyId
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
                branch.Currency,
                branch.IsActive,
                companyId = branch.BranchId
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
