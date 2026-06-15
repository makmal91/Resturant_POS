using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Brand.DTOs;
using POSSystem.Application.Brand.Interfaces;
using POSSystem.Domain;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BrandsController : ControllerBase
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly IBrandService _brandService;

    public BrandsController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBrands(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] bool? status = null)
    {
        if (!Request.Query.ContainsKey("branchId"))
            return BadRequest(new { message = "branchId is required." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId < 0)
            return BadRequest(new { message = "branchId is required." });

        if (resolvedBranchId == 0 && !IsGlobalAdminRequest())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "All branches brand view is available to global admins only." });

        try
        {
            var result = await _brandService.GetBrandsPagedAsync(
                resolvedBusinessId,
                resolvedBranchId,
                page,
                pageSize,
                search,
                status);

            return Ok(new
            {
                brands = result.Data,
                totalRecords = result.TotalRecords,
                totalPages = result.TotalPages,
                currentPage = result.CurrentPage,
                pageSize
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/image")]
    public async Task<IActionResult> GetBrandImage(int id, [FromQuery] int branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        var image = await _brandService.GetBrandImageAsync(id, resolvedBusinessId, resolvedBranchId);
        if (image == null || image.ImageData.Length == 0)
            return NotFound(new { message = "Brand image not found." });

        var contentType = string.IsNullOrWhiteSpace(image.ImageContentType)
            ? "application/octet-stream"
            : image.ImageContentType;

        return File(image.ImageData, contentType);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBrandById(int id, [FromQuery] int branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        var brand = await _brandService.GetBrandByIdAsync(id, resolvedBusinessId, resolvedBranchId);
        if (brand == null)
            return NotFound();

        return Ok(brand);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateBrandForm([FromForm] BrandFormDto form, IFormFile? imageFile)
    {
        if (form.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var dto = MapCreateDto(form);
            dto.BusinessId = this.ResolveBusinessId(null);
            var (imageBytes, fileName, contentType) = await ReadImageFileAsync(imageFile);
            var brand = await _brandService.AddBrandAsync(dto, imageBytes, fileName, contentType);
            var created = await _brandService.GetBrandByIdAsync(brand.Id, brand.BusinessId, brand.BranchId);
            return CreatedAtAction(
                nameof(GetBrandById),
                new { id = brand.Id, businessId = brand.BusinessId, branchId = brand.BranchId },
                created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrandDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var brand = await _brandService.AddBrandAsync(dto);
            var created = await _brandService.GetBrandByIdAsync(brand.Id, brand.BusinessId, brand.BranchId);
            return CreatedAtAction(
                nameof(GetBrandById),
                new { id = brand.Id, businessId = brand.BusinessId, branchId = brand.BranchId },
                created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateBrandForm(int id, [FromForm] BrandFormDto form, IFormFile? imageFile)
    {
        if (form.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var dto = MapUpdateDto(form);
            dto.BusinessId = this.ResolveBusinessId(null);
            var removeImage = string.Equals(form.RemoveImage, "true", StringComparison.OrdinalIgnoreCase);
            byte[]? imageBytes = null;
            string? fileName = null;
            string? contentType = null;
            var replaceImage = false;

            if (imageFile != null && imageFile.Length > 0)
            {
                (imageBytes, fileName, contentType) = await ReadImageFileAsync(imageFile);
                replaceImage = true;
            }
            else if (removeImage)
            {
                replaceImage = true;
            }

            var updated = await _brandService.UpdateBrandAsync(id, dto, imageBytes, fileName, contentType, replaceImage);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateBrand(int id, [FromBody] UpdateBrandDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var updated = await _brandService.UpdateBrandAsync(id, dto);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("status")]
    public async Task<IActionResult> PatchBrandStatus([FromBody] BrandStatusPatchDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (dto.Items.Count == 0)
            return BadRequest(new { message = "At least one brand status update is required." });

        try
        {
            await _brandService.PatchBrandStatusAsync(dto);
            return Ok(new { message = "Brand status updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBrand(int id, [FromQuery] int branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            await _brandService.DeleteBrandAsync(id, resolvedBusinessId, resolvedBranchId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static CreateBrandDto MapCreateDto(BrandFormDto form) => new()
    {
        Name = form.Name,
        Description = form.Description ?? string.Empty,
        Status = !string.Equals(form.Status ?? "Active", "Inactive", StringComparison.OrdinalIgnoreCase),
        BranchId = form.BranchId
    };

    private static UpdateBrandDto MapUpdateDto(BrandFormDto form) => new()
    {
        Name = form.Name,
        Description = form.Description ?? string.Empty,
        Status = !string.Equals(form.Status ?? "Active", "Inactive", StringComparison.OrdinalIgnoreCase),
        BranchId = form.BranchId
    };

    private static async Task<(byte[]? Image, string? FileName, string? ContentType)> ReadImageFileAsync(IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            return (null, null, null);

        if (imageFile.Length > MaxImageSizeBytes)
            throw new InvalidOperationException("Brand image must be 5 MB or smaller.");

        var contentType = string.IsNullOrWhiteSpace(imageFile.ContentType)
            ? "application/octet-stream"
            : imageFile.ContentType;

        if (!AllowedImageContentTypes.Contains(contentType))
            throw new InvalidOperationException("Brand image must be JPEG, PNG, or WebP.");

        await using var stream = imageFile.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        return (memoryStream.ToArray(), imageFile.FileName, contentType);
    }

    private bool IsGlobalAdminRequest()
    {
        var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ??
                   Request.Headers["X-User-Role"].FirstOrDefault();
        return RoleNames.HasGlobalBranchAccess(role ?? string.Empty);
    }
}
