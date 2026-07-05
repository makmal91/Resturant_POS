using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Menu.DTOs;
using POSSystem.Application.Menu.Interfaces;
using POSSystem.Domain;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SubCategoriesController : ControllerBase
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    private readonly IMenuService _menuService;

    public SubCategoriesController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSubCategories(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] bool? status = null,
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
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "All branches subcategory view is available to global admins only." });

        try
        {
            var result = await _menuService.GetSubCategoriesPagedAsync(
                resolvedBusinessId,
                resolvedBranchId,
                page,
                pageSize,
                search,
                categoryId,
                status,
                sortBy,
                sortDirection);

            return Ok(new
            {
                subCategories = result.Data,
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

    [HttpGet("{id:int}/image")]
    public async Task<IActionResult> GetSubCategoryImage(int id, [FromQuery] int branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        var image = await _menuService.GetSubCategoryImageAsync(id, resolvedBusinessId, resolvedBranchId);
        if (image == null || image.ImageData.Length == 0)
            return NotFound(new { message = "SubCategory image not found." });

        var contentType = string.IsNullOrWhiteSpace(image.ImageContentType)
            ? "application/octet-stream"
            : image.ImageContentType;

        return File(image.ImageData, contentType);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSubCategoryById(int id, [FromQuery] int branchId, [FromQuery] int? businessId, [FromQuery] bool includeImage = true)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        var subCategory = await _menuService.GetSubCategoryByIdAsync(id, resolvedBusinessId, resolvedBranchId, includeImage);
        if (subCategory == null)
            return NotFound();

        return Ok(subCategory);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateSubCategoryForm([FromForm] SubCategoryFormDto form, IFormFile? imageFile)
    {
        if (form.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (form.CategoryId <= 0)
            return BadRequest(new { message = "CategoryId is required." });

        try
        {
            var dto = MapCreateDto(form);
            dto.BusinessId = this.ResolveBusinessId(null);
            var (imageBytes, contentType) = await ReadImageFileAsync(imageFile);
            var subCategory = await _menuService.AddSubCategoryAsync(dto, imageBytes, contentType);
            var created = await _menuService.GetSubCategoryByIdAsync(subCategory.Id, subCategory.BusinessId, subCategory.BranchId, includeImage: true);
            return CreatedAtAction(
                nameof(GetSubCategoryById),
                new { id = subCategory.Id, businessId = subCategory.BusinessId, branchId = subCategory.BranchId },
                created);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> CreateSubCategory([FromBody] CreateSubCategoryDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (dto.CategoryId <= 0)
            return BadRequest(new { message = "CategoryId is required." });

        try
        {
            var subCategory = await _menuService.AddSubCategoryAsync(dto);
            var created = await _menuService.GetSubCategoryByIdAsync(subCategory.Id, subCategory.BusinessId, subCategory.BranchId, includeImage: true);
            return CreatedAtAction(
                nameof(GetSubCategoryById),
                new { id = subCategory.Id, businessId = subCategory.BusinessId, branchId = subCategory.BranchId },
                created);
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateSubCategoryForm(int id, [FromForm] SubCategoryFormDto form, IFormFile? imageFile)
    {
        if (form.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (form.CategoryId <= 0)
            return BadRequest(new { message = "CategoryId is required." });

        try
        {
            var dto = MapUpdateDto(form);
            dto.BusinessId = this.ResolveBusinessId(null);
            var removeImage = string.Equals(form.RemoveImage, "true", StringComparison.OrdinalIgnoreCase);
            byte[]? imageBytes = null;
            string? contentType = null;
            var replaceImage = false;

            if (imageFile != null && imageFile.Length > 0)
            {
                (imageBytes, contentType) = await ReadImageFileAsync(imageFile);
                replaceImage = true;
            }
            else if (removeImage)
            {
                replaceImage = true;
            }

            await _menuService.UpdateSubCategoryAsync(id, dto, imageBytes, contentType, replaceImage);
            var updated = await _menuService.GetSubCategoryByIdAsync(id, dto.BusinessId, dto.BranchId, includeImage: true);
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

    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateSubCategory(int id, [FromBody] UpdateSubCategoryDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (dto.CategoryId <= 0)
            return BadRequest(new { message = "CategoryId is required." });

        try
        {
            await _menuService.UpdateSubCategoryAsync(id, dto);
            var updated = await _menuService.GetSubCategoryByIdAsync(id, dto.BusinessId, dto.BranchId, includeImage: true);
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

    [HttpPatch("status")]
    public async Task<IActionResult> PatchSubCategoryStatus([FromBody] SubCategoryStatusPatchDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (dto.Items.Count == 0)
            return BadRequest(new { message = "At least one subcategory status update is required." });

        try
        {
            await _menuService.PatchSubCategoryStatusAsync(dto);
            return Ok(new { message = "SubCategory status updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSubCategory(int id, [FromQuery] int branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            await _menuService.DeleteSubCategoryAsync(id, resolvedBusinessId, resolvedBranchId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            await HttpContextExceptionLogging.LogAsync(HttpContext, ex);
            return BadRequest(new { message = ex.Message });
        }
    }

    private static CreateSubCategoryDto MapCreateDto(SubCategoryFormDto form) => new()
    {
        Name = form.Name,
        Code = form.Code ?? string.Empty,
        Description = form.Description ?? string.Empty,
        DisplayOrder = form.DisplayOrder,
        Icon = form.Icon ?? string.Empty,
        Status = !string.Equals(form.Status ?? "Active", "Inactive", StringComparison.OrdinalIgnoreCase),
        CategoryId = form.CategoryId,
        BranchId = form.BranchId
    };

    private static UpdateSubCategoryDto MapUpdateDto(SubCategoryFormDto form) => new()
    {
        Name = form.Name,
        Code = form.Code ?? string.Empty,
        Description = form.Description ?? string.Empty,
        DisplayOrder = form.DisplayOrder,
        Icon = form.Icon ?? string.Empty,
        Status = !string.Equals(form.Status ?? "Active", "Inactive", StringComparison.OrdinalIgnoreCase),
        CategoryId = form.CategoryId,
        BranchId = form.BranchId
    };

    private static async Task<(byte[]? Image, string? ContentType)> ReadImageFileAsync(IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            return (null, null);

        if (imageFile.Length > MaxImageSizeBytes)
            throw new InvalidOperationException("SubCategory image must be 5 MB or smaller.");

        var contentType = string.IsNullOrWhiteSpace(imageFile.ContentType)
            ? "application/octet-stream"
            : imageFile.ContentType;

        if (!AllowedImageContentTypes.Contains(contentType))
            throw new InvalidOperationException("SubCategory image must be JPEG, PNG, GIF, or WebP.");

        await using var stream = imageFile.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        return (memoryStream.ToArray(), contentType);
    }

    private bool IsGlobalAdminRequest()
    {
        var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ??
                   Request.Headers["X-User-Role"].FirstOrDefault();
        return RoleNames.HasGlobalBranchAccess(role ?? string.Empty);
    }
}
