using POSSystem.Application.Barcode.DTOs;
using POSSystem.Application.Barcode.Helpers;
using POSSystem.Application.Barcode.Interfaces;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Product.DTOs;
using POSSystem.Application.Product.Interfaces;
using ProductEntity = POSSystem.Domain.Product;

namespace POSSystem.Application.Barcode.Services;

public class BarcodePrintService : IBarcodePrintService
{
    private readonly IBarcodePrintRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitPricingService _unitPricingService;

    public BarcodePrintService(
        IBarcodePrintRepository repository,
        IProductRepository productRepository,
        IUnitPricingService unitPricingService)
    {
        _repository = repository;
        _productRepository = productRepository;
        _unitPricingService = unitPricingService;
    }

    public async Task<PagedResultDto<BarcodePrintProductDto>> SearchItemsAsync(BarcodePrintSearchRequestDto request)
    {
        var result = await _repository.SearchProductsAsync(request);
        var productIds = result.Data.Select(p => p.Id).ToList();
        var stockMap = await _repository.GetProductStockTotalsAsync(request.BusinessId, request.BranchId, productIds);

        return new PagedResultDto<BarcodePrintProductDto>
        {
            Data = result.Data.Select(p => MapProductDto(p, stockMap)).ToList(),
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage
        };
    }

    public async Task<ProductPrintDetailsDto?> GetProductPrintDetailsAsync(int productId, int businessId, int branchId)
    {
        var product = await _productRepository.GetByIdAsync(productId, businessId, branchId);
        if (product == null)
            return null;

        var units = product.Units.Where(u => !u.IsDeleted).OrderByDescending(u => u.IsBaseUnit).ThenBy(u => u.UnitName).ToList();
        var variants = product.Variants.Where(v => !v.IsDeleted && v.Status).OrderBy(v => v.VariantName).ToList();
        var barcodes = product.Barcodes.Where(b => !b.IsDeleted).ToList();

        return new ProductPrintDetailsDto
        {
            ProductId = product.Id,
            Name = product.ProductName,
            Sku = product.SKU,
            SellingPrice = product.SellingPrice,
            HasMultipleUnits = units.Count > 1,
            HasVariants = product.IsVariantEnabled && variants.Count > 0,
            Units = units.Select(u => new ProductUnitDto
            {
                Id = u.Id,
                UnitId = u.UnitId,
                UnitName = u.UnitName,
                ConversionFactor = u.ConversionFactor,
                IsBaseUnit = u.IsBaseUnit,
                IsPriceOverridden = u.IsPriceOverridden,
                CostPrice = u.CostPrice,
                SellingPrice = u.SellingPrice,
                WholesalePrice = u.WholesalePrice,
                CalculatedSellingPrice = _unitPricingService.CalculateAutoPrice(
                    product.SellingPrice, u.ConversionFactor, u.IsBaseUnit),
                CalculatedWholesalePrice = _unitPricingService.CalculateAutoPrice(
                    product.WholesalePrice, u.ConversionFactor, u.IsBaseUnit)
            }).ToList(),
            Variants = variants.Select(v => new ProductVariantDto
            {
                Id = v.Id,
                VariantName = v.VariantName,
                Size = v.Size,
                Color = v.Color,
                SKU = v.SKU,
                AdditionalPrice = v.AdditionalPrice,
                CostPriceOverride = v.CostPriceOverride,
                SellingPriceOverride = v.SellingPriceOverride,
                Status = v.Status
            }).ToList(),
            Barcodes = barcodes.Select(b => new ProductBarcodeDto
            {
                Id = b.Id,
                BarcodeValue = b.BarcodeValue,
                UnitId = b.ProductUnitId,
                VariantId = b.ProductVariantId,
                UnitName = b.ProductUnit?.UnitName
                    ?? units.FirstOrDefault(u => u.Id == b.ProductUnitId)?.UnitName,
                VariantName = b.ProductVariant?.VariantName
                    ?? variants.FirstOrDefault(v => v.Id == b.ProductVariantId)?.VariantName,
                IsPrimary = b.IsPrimary
            }).ToList()
        };
    }

    private static BarcodePrintProductDto MapProductDto(ProductEntity product, Dictionary<int, decimal> stockMap)
    {
        var units = product.Units.Where(u => !u.IsDeleted).ToList();
        var variants = product.Variants.Where(v => !v.IsDeleted && v.Status).ToList();
        var barcodes = product.Barcodes.Where(b => !b.IsDeleted).ToList();
        var defaultUnitId = BarcodeValueHelper.ResolveDefaultUnitId(product);
        var primaryBarcode = barcodes.FirstOrDefault(b => b.IsPrimary)?.BarcodeValue
            ?? barcodes.FirstOrDefault()?.BarcodeValue
            ?? (defaultUnitId > 0 ? BarcodeValueHelper.Generate(product.Id, defaultUnitId, null) : null);

        stockMap.TryGetValue(product.Id, out var stockQty);

        return new BarcodePrintProductDto
        {
            ProductId = product.Id,
            ProductName = product.ProductName,
            Sku = product.SKU,
            PrimaryBarcode = primaryBarcode,
            SellingPrice = product.SellingPrice,
            StockQty = stockQty,
            HasMultipleUnits = units.Count > 1,
            HasVariants = product.IsVariantEnabled && variants.Count > 0,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            SubCategoryId = product.SubCategoryId,
            SubCategoryName = product.SubCategory?.Name ?? string.Empty,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name ?? string.Empty
        };
    }
}
