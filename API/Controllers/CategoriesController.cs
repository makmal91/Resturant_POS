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
public class CategoriesController : ControllerBase
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

    public CategoriesController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] CategoryType? categoryType = null)
    {
        if (!Request.Query.ContainsKey("branchId"))
            return BadRequest(new { message = "branchId is required." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId < 0)
            return BadRequest(new { message = "branchId is required." });

        if (resolvedBranchId == 0 && !IsGlobalAdminRequest())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "All branches category view is available to global admins only." });

        try
        {
            var result = await _menuService.GetCategoriesPagedAsync(resolvedBusinessId, resolvedBranchId, page, pageSize, categoryType);
            return Ok(new
            {
                categories = result.Data,
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
    public async Task<IActionResult> GetCategoryImage(int id, [FromQuery] int branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        var image = await _menuService.GetCategoryImageAsync(id, resolvedBusinessId, resolvedBranchId);
        if (image == null || image.Image.Length == 0)
            return NotFound(new { message = "Category image not found." });

        var contentType = string.IsNullOrWhiteSpace(image.ImageContentType)
            ? "application/octet-stream"
            : image.ImageContentType;

        return File(image.Image, contentType, image.ImageFileName);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCategoryById(int id, [FromQuery] int branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        var category = await _menuService.GetCategoryByIdAsync(id, resolvedBusinessId, resolvedBranchId);
        if (category == null)
            return NotFound();

        return Ok(category);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateCategoryForm([FromForm] CategoryFormDto form, IFormFile? image)
    {
        if (form.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var dto = MapCreateDto(form);
            dto.BusinessId = this.ResolveBusinessId(null);
            var (imageBytes, fileName, contentType) = await ReadImageFileAsync(image);
            var category = await _menuService.AddCategoryAsync(dto, imageBytes, fileName, contentType);
            var created = await _menuService.GetCategoryByIdAsync(category.Id, category.BusinessId, category.BranchId);
            return CreatedAtAction(
                nameof(GetCategoryById),
                new { id = category.Id, businessId = category.BusinessId, branchId = category.BranchId },
                created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateMenuCategoryDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var category = await _menuService.AddCategoryAsync(dto);
            var created = await _menuService.GetCategoryByIdAsync(category.Id, category.BusinessId, category.BranchId);
            return CreatedAtAction(
                nameof(GetCategoryById),
                new { id = category.Id, businessId = category.BusinessId, branchId = category.BranchId },
                created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateCategoryForm(int id, [FromForm] CategoryFormDto form, IFormFile? image)
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

            if (image != null && image.Length > 0)
            {
                (imageBytes, fileName, contentType) = await ReadImageFileAsync(image);
                replaceImage = true;
            }
            else if (removeImage)
            {
                replaceImage = true;
            }

            await _menuService.UpdateCategoryAsync(id, dto, imageBytes, fileName, contentType, replaceImage);
            var updated = await _menuService.GetCategoryByIdAsync(id, dto.BusinessId, dto.BranchId);
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
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateMenuCategoryDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            await _menuService.UpdateCategoryAsync(id, dto);
            var updated = await _menuService.GetCategoryByIdAsync(id, dto.BusinessId, dto.BranchId);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id, [FromQuery] int branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        try
        {
            await _menuService.DeleteCategoryAsync(id, resolvedBusinessId, resolvedBranchId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static CreateMenuCategoryDto MapCreateDto(CategoryFormDto form) => new()
    {
        Name = form.Name,
        Code = form.Code ?? string.Empty,
        Description = form.Description ?? string.Empty,
        DisplayOrder = form.DisplayOrder,
        ImageUrl = form.ImageUrl ?? string.Empty,
        Icon = form.Icon ?? string.Empty,
        Color = string.IsNullOrWhiteSpace(form.Color) ? "#2563eb" : form.Color,
        Status = !string.Equals(form.Status ?? "Active", "Inactive", StringComparison.OrdinalIgnoreCase),
        CategoryType = form.CategoryType,
        BranchId = form.BranchId
    };

    private static UpdateMenuCategoryDto MapUpdateDto(CategoryFormDto form) => new()
    {
        Name = form.Name,
        Code = form.Code ?? string.Empty,
        Description = form.Description ?? string.Empty,
        DisplayOrder = form.DisplayOrder,
        ImageUrl = form.ImageUrl ?? string.Empty,
        Icon = form.Icon ?? string.Empty,
        Color = string.IsNullOrWhiteSpace(form.Color) ? "#2563eb" : form.Color,
        Status = !string.Equals(form.Status ?? "Active", "Inactive", StringComparison.OrdinalIgnoreCase),
        CategoryType = form.CategoryType,
        BranchId = form.BranchId
    };

    private static async Task<(byte[]? Image, string? FileName, string? ContentType)> ReadImageFileAsync(IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            return (null, null, null);

        if (imageFile.Length > MaxImageSizeBytes)
            throw new InvalidOperationException("Category image must be 5 MB or smaller.");

        var contentType = string.IsNullOrWhiteSpace(imageFile.ContentType)
            ? "application/octet-stream"
            : imageFile.ContentType;

        if (!AllowedImageContentTypes.Contains(contentType))
            throw new InvalidOperationException("Category image must be JPEG, PNG, GIF, or WebP.");

        await using var stream = imageFile.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        return (memoryStream.ToArray(), imageFile.FileName, contentType);
    }

    private bool IsGlobalAdminRequest()
    {
        if (User?.IsInRole("SuperAdmin") == true)
            return true;

        var roleHeader = Request.Headers["X-User-Role"].FirstOrDefault();
        return string.Equals(roleHeader, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }
}
