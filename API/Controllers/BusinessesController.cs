using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.Application.Business.DTOs;
using POSSystem.Application.Business.Interfaces;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BusinessesController : ControllerBase
{
    private const long MaxLogoSizeBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedLogoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    private readonly IBusinessService _businessService;

    public BusinessesController(IBusinessService businessService)
    {
        _businessService = businessService;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyBusiness()
    {
        var businessIdHeader = Request.Headers["X-Business-Id"].FirstOrDefault();
        if (!int.TryParse(businessIdHeader, out var businessId) || businessId <= 0)
            return NotFound(new { message = "Business context not found." });

        var business = await _businessService.GetBusinessByIdAsync(businessId);
        if (business == null)
            return NotFound(new { message = "Business not found." });

        return Ok(new
        {
            id = business.Id,
            name = business.Name,
            legalName = business.LegalName,
            address = business.Address,
            phone = business.Phone,
            email = business.Email,
            currency = business.Currency,
            taxNumber = business.TaxNumber,
            hasLogo = business.HasLogo
        });
    }

    [HttpGet("my/logo")]
    public async Task<IActionResult> GetMyBusinessLogo()
    {
        var businessIdHeader = Request.Headers["X-Business-Id"].FirstOrDefault();
        if (!int.TryParse(businessIdHeader, out var businessId) || businessId <= 0)
            return NotFound(new { message = "Logo not found." });

        var logo = await _businessService.GetBusinessLogoAsync(businessId);
        if (logo == null || logo.Logo.Length == 0)
            return NotFound(new { message = "Logo not found." });

        var contentType = string.IsNullOrWhiteSpace(logo.LogoContentType)
            ? "application/octet-stream"
            : logo.LogoContentType;

        return File(logo.Logo, contentType, logo.LogoFileName);
    }

    [HttpGet]
    public async Task<IActionResult> GetBusinesses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null)
    {
        var result = await _businessService.GetBusinessesAsync(page, pageSize, search, sortBy, sortDirection);
        return Ok(new
        {
            data = result.Data,
            totalRecords = result.TotalRecords,
            totalPages = result.TotalPages,
            currentPage = result.CurrentPage
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBusinessById(int id)
    {
        var business = await _businessService.GetBusinessByIdAsync(id);
        if (business == null)
            return NotFound(new { message = "Business not found." });

        return Ok(business);
    }

    [HttpGet("{id:int}/logo")]
    public async Task<IActionResult> GetBusinessLogo(int id)
    {
        var logo = await _businessService.GetBusinessLogoAsync(id);
        if (logo == null || logo.Logo.Length == 0)
            return NotFound(new { message = "Logo not found." });

        var contentType = string.IsNullOrWhiteSpace(logo.LogoContentType)
            ? "application/octet-stream"
            : logo.LogoContentType;

        return File(logo.Logo, contentType, logo.LogoFileName);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateBusinessForm([FromForm] BusinessFormDto form, IFormFile? logo)
    {
        try
        {
            var dto = MapCreateDto(form);
            var (logoBytes, fileName, contentType) = await ReadLogoFileAsync(logo);
            var business = await _businessService.CreateBusinessAsync(dto, logoBytes, fileName, contentType);
            return CreatedAtAction(nameof(GetBusinessById), new { id = business.Id }, business);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> CreateBusinessJson([FromBody] CreateBusinessDto dto)
    {
        try
        {
            var business = await _businessService.CreateBusinessAsync(dto, null, null, null);
            return CreatedAtAction(nameof(GetBusinessById), new { id = business.Id }, business);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateBusinessForm(int id, [FromForm] BusinessFormDto form, IFormFile? logo)
    {
        try
        {
            var dto = MapUpdateDto(form);
            var removeLogo = string.Equals(form.RemoveLogo, "true", StringComparison.OrdinalIgnoreCase);
            byte[]? logoBytes = null;
            string? fileName = null;
            string? contentType = null;
            var replaceLogo = false;

            if (logo != null && logo.Length > 0)
            {
                (logoBytes, fileName, contentType) = await ReadLogoFileAsync(logo);
                replaceLogo = true;
            }
            else if (removeLogo)
            {
                replaceLogo = true;
            }

            var business = await _businessService.UpdateBusinessAsync(id, dto, logoBytes, fileName, contentType, replaceLogo);
            if (business == null)
                return NotFound(new { message = "Business not found." });

            return Ok(business);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateBusinessJson(int id, [FromBody] UpdateBusinessDto dto)
    {
        try
        {
            var business = await _businessService.UpdateBusinessAsync(id, dto, null, null, null, false);
            if (business == null)
                return NotFound(new { message = "Business not found." });

            return Ok(business);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBusiness(int id)
    {
        try
        {
            var deleted = await _businessService.DeleteBusinessAsync(id);
            if (!deleted)
                return NotFound(new { message = "Business not found." });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static CreateBusinessDto MapCreateDto(BusinessFormDto form)
    {
        return new CreateBusinessDto
        {
            Name = form.Name,
            LegalName = form.LegalName,
            Phone = form.Phone,
            Email = form.Email,
            Address = form.Address,
            TaxNumber = form.TaxNumber,
            Currency = form.Currency,
            TimeZone = form.TimeZone,
            IsActive = !string.Equals(form.IsActive, "false", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static UpdateBusinessDto MapUpdateDto(BusinessFormDto form)
    {
        return new UpdateBusinessDto
        {
            Name = form.Name,
            LegalName = form.LegalName,
            Phone = form.Phone,
            Email = form.Email,
            Address = form.Address,
            TaxNumber = form.TaxNumber,
            Currency = form.Currency,
            TimeZone = form.TimeZone,
            IsActive = !string.Equals(form.IsActive, "false", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static async Task<(byte[]? Logo, string? FileName, string? ContentType)> ReadLogoFileAsync(IFormFile? logoFile)
    {
        if (logoFile == null || logoFile.Length == 0)
            return (null, null, null);

        if (logoFile.Length > MaxLogoSizeBytes)
            throw new InvalidOperationException("Logo file must be 5 MB or smaller.");

        var contentType = string.IsNullOrWhiteSpace(logoFile.ContentType)
            ? "application/octet-stream"
            : logoFile.ContentType;

        if (!AllowedLogoContentTypes.Contains(contentType))
            throw new InvalidOperationException("Logo must be a JPEG, PNG, GIF, or WebP image.");

        await using var stream = logoFile.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        return (memoryStream.ToArray(), logoFile.FileName, contentType);
    }
}
