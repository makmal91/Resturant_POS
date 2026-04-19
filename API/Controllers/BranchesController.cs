using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Infrastructure.Data;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BranchesController : ControllerBase
{
    private readonly POSDbContext _context;

    public BranchesController(POSDbContext context)
    {
        _context = context;
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
}
