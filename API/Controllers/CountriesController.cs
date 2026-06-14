using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.Application.Branch.Interfaces;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CountriesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public CountriesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCountries()
    {
        var countries = await _branchService.GetCountriesAsync();
        return Ok(countries);
    }

    [HttpGet("{countryId:int}/cities")]
    public async Task<IActionResult> GetCitiesByCountry(int countryId)
    {
        if (countryId <= 0)
            return BadRequest(new { message = "CountryId is required." });

        var cities = await _branchService.GetCitiesByCountryIdAsync(countryId);
        return Ok(cities);
    }
}
