using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Product.DTOs;

namespace POSSystem.Application.Product.Interfaces;

public interface IProductService
{
    Task<PagedResultDto<ProductListDto>> SearchProductsAsync(ProductSearchRequestDto request);
    Task<ProductDetailDto?> GetProductByIdAsync(int id, int businessId, int branchId);
    Task<ProductDetailDto> CreateProductAsync(CreateProductDto dto);
    Task<ProductDetailDto> UpdateProductAsync(int id, UpdateProductDto dto);
    Task<ProductDetailDto> ReplaceUnitsAsync(int id, int businessId, int branchId, List<ProductUnitWriteDto> units);
    Task<ProductDetailDto> ReplaceVariantsAsync(int id, int businessId, int branchId, List<ProductVariantWriteDto> variants);
    Task<ProductDetailDto> ReplaceBarcodesAsync(int id, int businessId, int branchId, List<ProductBarcodeWriteDto> barcodes);
    Task<ProductDetailDto> AddImagesAsync(int id, int businessId, int branchId, IEnumerable<ProductImageUploadDto> images);
    Task RemoveBarcodeAsync(int id, int barcodeId, int businessId, int branchId);
    Task RemoveImageAsync(int id, int imageId, int businessId, int branchId);
    Task<ProductImageDataDto?> GetProductImageAsync(int productId, int imageId, int businessId, int branchId);
    Task<ProductImageDataDto?> GetPrimaryProductImageAsync(int productId, int businessId, int branchId);
}

public class ProductImageUploadDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public bool IsPrimary { get; set; }
}
