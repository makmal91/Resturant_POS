using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Product.DTOs;
using ProductEntity = POSSystem.Domain.Product;

namespace POSSystem.Application.Product.Interfaces;

public interface IProductRepository
{
    Task<PagedResultDto<ProductEntity>> SearchAsync(ProductSearchRequestDto request);
    Task<ProductEntity?> GetByIdAsync(int id, int businessId, int branchId);
    Task<int> GetNextProductNumberAsync(int businessId, int branchId);
    Task<bool> ProductCodeExistsAsync(string productCode, int businessId, int branchId, int? excludeProductId = null);
    Task<bool> BarcodeExistsAsync(string barcodeValue, int? excludeBarcodeId = null);
    Task<bool> CategoryExistsAsync(int categoryId, int businessId, int branchId);
    Task<bool> SubCategoryBelongsToCategoryAsync(int subCategoryId, int categoryId, int businessId, int branchId);
    Task<bool> BrandExistsAsync(int brandId, int businessId, int branchId);
    Task AddAsync(ProductEntity product);
    Task<POSSystem.Domain.ProductImage?> GetImageByIdAsync(int productId, int imageId, int businessId, int branchId);
    void RemoveImage(POSSystem.Domain.ProductImage image);
    Task SaveChangesAsync();
}
