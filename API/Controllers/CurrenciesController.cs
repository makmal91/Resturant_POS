using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Infrastructure.Data;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CurrenciesController : ControllerBase
{
    private readonly POSDbContext _db;

    public CurrenciesController(POSDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetCurrencies()
    {
        var currencies = await _db.Currencies
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.IsBase ? 0 : 1)
            .ThenBy(c => c.Code)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Name,
                c.Symbol,
                c.ExchangeRateToPKR,
                c.IsBase,
            })
            .ToListAsync();

        return Ok(currencies);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCurrencyById(int id)
    {
        var currency = await _db.Currencies
            .AsNoTracking()
            .Where(c => c.Id == id && c.IsActive)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Name,
                c.Symbol,
                c.ExchangeRateToPKR,
                c.IsBase,
            })
            .FirstOrDefaultAsync();

        if (currency == null)
            return NotFound(new { message = "Currency not found." });

        return Ok(currency);
    }
}
