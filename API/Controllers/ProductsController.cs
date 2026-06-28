using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Barcode.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Product.DTOs;
using POSSystem.Application.Product.Interfaces;
using POSSystem.Domain;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    private readonly IProductService _productService;
    private readonly IUnitPricingService _unitPricingService;
    private readonly IFeaturePermissionService _featurePermission;
    private readonly IBarcodePrintService _barcodePrintService;

    public ProductsController(
        IProductService productService,
        IUnitPricingService unitPricingService,
        IFeaturePermissionService featurePermission,
        IBarcodePrintService barcodePrintService)
    {
        _productService = productService;
        _unitPricingService = unitPricingService;
        _featurePermission = featurePermission;
        _barcodePrintService = barcodePrintService;
    }

    [HttpGet]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int? subCategoryId = null,
        [FromQuery] int? brandId = null,
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
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "All branches product view is available to global admins only." });

        try
        {
            var result = await _productService.SearchProductsAsync(new ProductSearchRequestDto
            {
                BusinessId = resolvedBusinessId,
                BranchId = resolvedBranchId,
                Page = page,
                PageSize = pageSize,
                Search = search,
                CategoryId = categoryId,
                SubCategoryId = subCategoryId,
                BrandId = brandId,
                Status = status,
                SortBy = sortBy,
                SortDirection = sortDirection
            });

            return Ok(new
            {
                products = result.Data,
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

    [HttpGet("{id:int}/details")]
    public async Task<IActionResult> GetProductPrintDetails(int id, [FromQuery] int branchId, [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        var details = await _barcodePrintService.GetProductPrintDetailsAsync(id, resolvedBusinessId, resolvedBranchId);
        if (details == null)
            return NotFound();

        return Ok(details);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductById(int id, [FromQuery] int branchId, [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        var product = await _productService.GetProductByIdAsync(id, resolvedBusinessId, resolvedBranchId);
        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var product = await _productService.CreateProductAsync(dto);
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id, businessId = dto.BusinessId, branchId = dto.BranchId }, product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var product = await _productService.UpdateProductAsync(id, dto);
            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/units")]
    public async Task<IActionResult> UpdateProductUnits(int id, [FromBody] ProductChildUpdateDto<ProductUnitWriteDto> dto)
    {
        if (!await _featurePermission.IsEnabledAsync(PermissionFeatureKeys.UnitEnable))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Unit management is not enabled for your role." });

        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            return Ok(await _productService.ReplaceUnitsAsync(id, dto.BusinessId, dto.BranchId, dto.Items));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/unit-pricing")]
    public async Task<IActionResult> GetUnitPricing(int id, [FromQuery] int branchId, [FromQuery] int? businessId = null)
    {
        if (!await _featurePermission.IsEnabledAsync(PermissionFeatureKeys.UnitEnable))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Unit management is not enabled for your role." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);
        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "branchId is required." });

        var pricing = await _unitPricingService.GetProductUnitPricingAsync(id, resolvedBusinessId, resolvedBranchId);
        return pricing == null ? NotFound() : Ok(pricing);
    }

    [HttpPost("{id:int}/calculate-unit-price")]
    public async Task<IActionResult> CalculateUnitPrice(int id, [FromBody] CalculateUnitPriceRequestDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            return Ok(await _unitPricingService.CalculateUnitPriceAsync(id, dto));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/units/{unitId:int}/price-override")]
    public async Task<IActionResult> SaveUnitPriceOverride(
        int id, int unitId, [FromBody] SaveUnitPriceOverrideDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            return Ok(await _unitPricingService.SaveUnitPriceOverrideAsync(id, unitId, dto));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/base-price")]
    public async Task<IActionResult> UpdateBasePrice(int id, [FromBody] UpdateBasePriceDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            return Ok(await _unitPricingService.UpdateBasePriceAndRecalculateAsync(id, dto));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/variants")]
    public async Task<IActionResult> UpdateProductVariants(int id, [FromBody] ProductChildUpdateDto<ProductVariantWriteDto> dto)
    {
        if (!await _featurePermission.IsEnabledAsync(PermissionFeatureKeys.VariantEnable))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Variant management is not enabled for your role." });

        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            return Ok(await _productService.ReplaceVariantsAsync(id, dto.BusinessId, dto.BranchId, dto.Items));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/barcodes")]
    public async Task<IActionResult> UpdateProductBarcodes(int id, [FromBody] ProductChildUpdateDto<ProductBarcodeWriteDto> dto)
    {
        if (!await _featurePermission.IsEnabledAsync(PermissionFeatureKeys.BarcodeEnable))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Barcode management is not enabled for your role." });

        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            return Ok(await _productService.ReplaceBarcodesAsync(id, dto.BusinessId, dto.BranchId, dto.Items));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}/barcodes/{barcodeId:int}")]
    public async Task<IActionResult> RemoveProductBarcode(int id, int barcodeId, [FromQuery] int branchId, [FromQuery] int? businessId = null)
    {
        if (!await _featurePermission.IsEnabledAsync(PermissionFeatureKeys.BarcodeEnable))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Barcode management is not enabled for your role." });

        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        try
        {
            await _productService.RemoveBarcodeAsync(id, barcodeId, resolvedBusinessId, resolvedBranchId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/images")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProductImages(int id, [FromForm] ProductImageUploadFormDto form)
    {
        var resolvedBusinessId = this.ResolveBusinessId(form.BusinessId > 0 ? form.BusinessId : null);
        var resolvedBranchId = this.ResolveBranchId(form.BranchId > 0 ? form.BranchId : null);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var uploads = new List<ProductImageUploadDto>();
            foreach (var image in form.Images)
            {
                uploads.Add(await ReadImageFileAsync(image, form.IsPrimary && uploads.Count == 0));
            }

            var product = await _productService.AddImagesAsync(id, resolvedBusinessId, resolvedBranchId, uploads);
            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> GetProductImage(int id, int imageId, [FromQuery] int branchId, [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);
        var image = await _productService.GetProductImageAsync(id, imageId, resolvedBusinessId, resolvedBranchId);
        if (image == null || image.ImageData.Length == 0)
            return NotFound(new { message = "Product image not found." });

        return File(image.ImageData, image.ContentType, image.FileName);
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> RemoveProductImage(int id, int imageId, [FromQuery] int branchId, [FromQuery] int? businessId = null)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        try
        {
            await _productService.RemoveImageAsync(id, imageId, resolvedBusinessId, resolvedBranchId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<ProductImageUploadDto> ReadImageFileAsync(IFormFile imageFile, bool isPrimary)
    {
        if (imageFile.Length == 0)
            throw new InvalidOperationException("Image file is empty.");

        if (imageFile.Length > MaxImageSizeBytes)
            throw new InvalidOperationException("Product image must be 5 MB or smaller.");

        var contentType = string.IsNullOrWhiteSpace(imageFile.ContentType)
            ? "application/octet-stream"
            : imageFile.ContentType;

        if (!AllowedImageContentTypes.Contains(contentType))
            throw new InvalidOperationException("Product image must be JPEG, PNG, GIF, or WebP.");

        await using var stream = imageFile.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        return new ProductImageUploadDto
        {
            FileName = imageFile.FileName,
            ContentType = contentType,
            ImageData = memoryStream.ToArray(),
            IsPrimary = isPrimary
        };
    }

    private bool IsGlobalAdminRequest()
    {
        var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ??
                   Request.Headers["X-User-Role"].FirstOrDefault();
        return RoleNames.HasGlobalBranchAccess(role ?? string.Empty);
    }
}

public class ProductChildUpdateDto<T>
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public List<T> Items { get; set; } = new();
}

public class ProductImageUploadFormDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public bool IsPrimary { get; set; }
    public List<IFormFile> Images { get; set; } = new();
}
