using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Common.Helpers;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Application.Product.DTOs;
using POSSystem.Application.Product.Interfaces;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Application.Unit.Interfaces;
using POSSystem.Application.Warehouse.Interfaces;
using POSSystem.Domain;
using ProductEntity = POSSystem.Domain.Product;

namespace POSSystem.Application.Product.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IStockLedgerRepository _stockLedgerRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ILowStockAlertService _lowStockAlertService;
    private readonly IUnitRepository _unitRepository;
    private readonly IUnitPricingService _unitPricingService;

    public ProductService(
        IProductRepository repository,
        ICodeGeneratorService codeGenerator,
        IStockLedgerRepository stockLedgerRepository,
        IWarehouseRepository warehouseRepository,
        ILowStockAlertService lowStockAlertService,
        IUnitRepository unitRepository,
        IUnitPricingService unitPricingService)
    {
        _repository = repository;
        _codeGenerator = codeGenerator;
        _stockLedgerRepository = stockLedgerRepository;
        _warehouseRepository = warehouseRepository;
        _lowStockAlertService = lowStockAlertService;
        _unitRepository = unitRepository;
        _unitPricingService = unitPricingService;
    }

    public async Task<PagedResultDto<ProductListDto>> SearchProductsAsync(ProductSearchRequestDto request)
    {
        var result = await _repository.SearchAsync(request);

        return new PagedResultDto<ProductListDto>
        {
            Data = result.Data.Select(MapListDto).ToList(),
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage
        };
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(int id, int businessId, int branchId)
    {
        var product = await _repository.GetByIdAsync(id, businessId, branchId);
        return product == null ? null : await MapDetailDto(product);
    }

    public async Task<ProductDetailDto> CreateProductAsync(CreateProductDto dto)
    {
        await ValidateProductAsync(dto);
        var productCode = await ResolveProductCodeAsync(dto.ProductCode, dto.BusinessId, dto.BranchId);

        var product = new ProductEntity
        {
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId,
            ProductName = dto.ProductName.Trim(),
            ProductCode = productCode,
            SKU = dto.SKU?.Trim() ?? string.Empty,
            Description = dto.Description?.Trim() ?? string.Empty,
            Status = dto.Status,
            CategoryId = dto.CategoryId,
            SubCategoryId = dto.SubCategoryId > 0 ? dto.SubCategoryId : null,
            BrandId = dto.BrandId > 0 ? dto.BrandId : null,
            CostPrice = Math.Max(0, dto.CostPrice),
            SellingPrice = Math.Max(0, dto.SellingPrice),
            WholesalePrice = Math.Max(0, dto.WholesalePrice),
            UseAutoUnitPricing = dto.UseAutoUnitPricing,
            IsVariantEnabled = dto.IsVariantEnabled,
            IsDiscountAllowed = dto.IsDiscountAllowed,
            DiscountType = dto.IsDiscountAllowed ? dto.DiscountType : null,
            DiscountValue = dto.IsDiscountAllowed ? Math.Max(0, dto.DiscountValue) : 0,
            AllowNegativeStock = dto.AllowNegativeStock,
            EnableLowStockAlert = dto.EnableLowStockAlert,
            LowStockAlertLevel = dto.EnableLowStockAlert ? dto.LowStockAlertLevel : null,
            OpeningStock = Math.Max(0, dto.OpeningStock),
            OpeningStockVariantWise = dto.OpeningStockVariantWise && dto.IsVariantEnabled
        };

        await ReplaceUnitsAsync(product, dto.Units, dto.BusinessId, dto.BranchId);
        ReplaceVariants(product, dto.IsVariantEnabled ? dto.Variants : new List<ProductVariantWriteDto>(), dto.BusinessId, dto.BranchId);
        await ReplaceBarcodesAsync(product, dto.Barcodes, dto.BusinessId, dto.BranchId);
        await EnsurePrimaryBarcodeAsync(product, dto.BusinessId, dto.BranchId);

        if (product.OpeningStockVariantWise)
            product.OpeningStock = ResolveVariantOpeningTotal(dto);

        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();
        SyncBaseUnitId(product);
        await _repository.SaveChangesAsync();

        await ApplyOpeningStockAsync(product, dto);

        var created = await _repository.GetByIdAsync(product.Id, dto.BusinessId, dto.BranchId);
        return await MapDetailDto(created ?? product);
    }

    public async Task<ProductDetailDto> UpdateProductAsync(int id, UpdateProductDto dto)
    {
        await ValidateProductAsync(dto);
        var product = await _repository.GetByIdAsync(id, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Product not found.");

        var productCode = string.IsNullOrWhiteSpace(dto.ProductCode)
            ? product.ProductCode
            : dto.ProductCode.Trim();

        if (await _repository.ProductCodeExistsAsync(productCode, dto.BusinessId, dto.BranchId, id))
            throw new InvalidOperationException("ProductCode must be unique within the selected branch.");

        product.ProductName = dto.ProductName.Trim();
        product.ProductCode = productCode;
        product.SKU = dto.SKU?.Trim() ?? string.Empty;
        product.Description = dto.Description?.Trim() ?? string.Empty;
        product.Status = dto.Status;
        product.CategoryId = dto.CategoryId;
        product.SubCategoryId = dto.SubCategoryId > 0 ? dto.SubCategoryId : null;
        product.BrandId = dto.BrandId > 0 ? dto.BrandId : null;
        product.CostPrice = Math.Max(0, dto.CostPrice);
        product.SellingPrice = Math.Max(0, dto.SellingPrice);
        product.WholesalePrice = Math.Max(0, dto.WholesalePrice);
        product.UseAutoUnitPricing = dto.UseAutoUnitPricing;
        product.IsVariantEnabled = dto.IsVariantEnabled;
        product.IsDiscountAllowed = dto.IsDiscountAllowed;
        product.DiscountType = dto.IsDiscountAllowed ? dto.DiscountType : null;
        product.DiscountValue = dto.IsDiscountAllowed ? Math.Max(0, dto.DiscountValue) : 0;
        product.AllowNegativeStock = dto.AllowNegativeStock;
        product.EnableLowStockAlert = dto.EnableLowStockAlert;
        product.LowStockAlertLevel = dto.EnableLowStockAlert ? dto.LowStockAlertLevel : null;
        await ReplaceUnitsAsync(product, dto.Units, dto.BusinessId, dto.BranchId);
        ReplaceVariants(product, dto.IsVariantEnabled ? dto.Variants : new List<ProductVariantWriteDto>(), dto.BusinessId, dto.BranchId);
        await ReplaceBarcodesAsync(product, dto.Barcodes, dto.BusinessId, dto.BranchId);

        SyncBaseUnitId(product);
        await _repository.SaveChangesAsync();
        return await MapDetailDto(product);
    }

    public async Task<ProductDetailDto> ReplaceUnitsAsync(int id, int businessId, int branchId, List<ProductUnitWriteDto> units)
    {
        var product = await GetProductOrThrowAsync(id, businessId, branchId);
        ValidateUnits(units);
        await ReplaceUnitsAsync(product, units, businessId, branchId);
        SyncBaseUnitId(product);
        await _repository.SaveChangesAsync();
        return await MapDetailDto(product);
    }

    public async Task<ProductDetailDto> ReplaceVariantsAsync(int id, int businessId, int branchId, List<ProductVariantWriteDto> variants)
    {
        var product = await GetProductOrThrowAsync(id, businessId, branchId);
        ReplaceVariants(product, variants, businessId, branchId);
        product.IsVariantEnabled = variants.Count > 0;
        await _repository.SaveChangesAsync();
        return await MapDetailDto(product);
    }

    public async Task<ProductDetailDto> ReplaceBarcodesAsync(int id, int businessId, int branchId, List<ProductBarcodeWriteDto> barcodes)
    {
        var product = await GetProductOrThrowAsync(id, businessId, branchId);
        await ReplaceBarcodesAsync(product, barcodes, businessId, branchId);
        await _repository.SaveChangesAsync();
        return await MapDetailDto(product);
    }

    public async Task<ProductDetailDto> AddImagesAsync(int id, int businessId, int branchId, IEnumerable<ProductImageUploadDto> images)
    {
        var product = await GetProductOrThrowAsync(id, businessId, branchId);
        var uploads = images.Where(i => i.ImageData.Length > 0).ToList();
        if (uploads.Count == 0)
            throw new InvalidOperationException("At least one image is required.");

        var sortOrder = product.Images.Count == 0 ? 1 : product.Images.Max(i => i.SortOrder) + 1;
        var shouldSetPrimary = !product.Images.Any(i => i.IsPrimary);

        foreach (var upload in uploads)
        {
            var isPrimary = upload.IsPrimary || shouldSetPrimary;
            if (isPrimary)
            {
                foreach (var existing in product.Images)
                    existing.IsPrimary = false;
                shouldSetPrimary = false;
            }

            product.Images.Add(new ProductImage
            {
                BusinessId = businessId,
                BranchId = branchId,
                FileName = upload.FileName,
                ContentType = upload.ContentType,
                ImageData = upload.ImageData,
                IsPrimary = isPrimary,
                SortOrder = sortOrder++
            });
        }

        await _repository.SaveChangesAsync();
        return await MapDetailDto(product);
    }

    public async Task RemoveBarcodeAsync(int id, int barcodeId, int businessId, int branchId)
    {
        var product = await GetProductOrThrowAsync(id, businessId, branchId);
        var barcode = product.Barcodes.FirstOrDefault(b => b.Id == barcodeId);
        if (barcode == null)
            throw new InvalidOperationException("Barcode not found.");

        product.Barcodes.Remove(barcode);
        await _repository.SaveChangesAsync();
    }

    public async Task RemoveImageAsync(int id, int imageId, int businessId, int branchId)
    {
        var image = await _repository.GetImageByIdAsync(id, imageId, businessId, branchId)
            ?? throw new InvalidOperationException("Product image not found.");

        _repository.RemoveImage(image);
        await _repository.SaveChangesAsync();
    }

    public async Task<ProductImageDataDto?> GetProductImageAsync(int productId, int imageId, int businessId, int branchId)
    {
        var image = await _repository.GetImageByIdAsync(productId, imageId, businessId, branchId);
        return image == null
            ? null
            : new ProductImageDataDto
            {
                Id = image.Id,
                FileName = image.FileName,
                ContentType = image.ContentType,
                ImageData = image.ImageData,
                IsPrimary = image.IsPrimary,
                SortOrder = image.SortOrder
            };
    }

    private async Task ValidateProductAsync(CreateProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ProductName))
            throw new InvalidOperationException("ProductName is required.");

        if (dto.BranchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        if (dto.CategoryId <= 0)
            throw new InvalidOperationException("CategoryId is required.");

        if (!await _repository.CategoryExistsAsync(dto.CategoryId, dto.BusinessId, dto.BranchId))
            throw new InvalidOperationException("Product must belong to a valid category.");

        if (dto.SubCategoryId.HasValue && dto.SubCategoryId.Value > 0 &&
            !await _repository.SubCategoryBelongsToCategoryAsync(dto.SubCategoryId.Value, dto.CategoryId, dto.BusinessId, dto.BranchId))
            throw new InvalidOperationException("SubCategory must belong to the selected Category.");

        if (dto.BrandId.HasValue && dto.BrandId.Value > 0 &&
            !await _repository.BrandExistsAsync(dto.BrandId.Value, dto.BusinessId, dto.BranchId))
            throw new InvalidOperationException("Brand was not found in the selected branch.");

        ValidateUnits(dto.Units);
        await ValidateBarcodesAsync(dto.Barcodes);
        ValidateStockSettings(dto);
    }

    private static void ValidateStockSettings(CreateProductDto dto)
    {
        if (dto.OpeningStock < 0)
            throw new InvalidOperationException("OpeningStock cannot be negative.");

        if (dto.OpeningStockVariantWise)
        {
            if (!dto.IsVariantEnabled || dto.Variants.Count == 0)
                throw new InvalidOperationException("Variant-wise opening stock requires at least one variant.");

            foreach (var line in dto.OpeningStockByVariant)
            {
                if (line.Quantity < 0)
                    throw new InvalidOperationException("Opening stock quantity cannot be negative for any variant.");
            }
        }

        if (dto.LowStockAlertLevel.HasValue && dto.LowStockAlertLevel.Value < 0)
            throw new InvalidOperationException("LowStockAlertLevel cannot be negative.");

        if (dto.EnableLowStockAlert && (!dto.LowStockAlertLevel.HasValue || dto.LowStockAlertLevel.Value < 0))
            throw new InvalidOperationException("LowStockAlertLevel is required when low stock alert is enabled.");
    }

    private static decimal ResolveVariantOpeningTotal(CreateProductDto dto)
    {
        return dto.OpeningStockByVariant
            .Where(l => l.Quantity > 0)
            .Sum(l => l.Quantity);
    }

    private async Task ApplyOpeningStockAsync(ProductEntity product, CreateProductDto dto)
    {
        if (await _stockLedgerRepository.HasOpeningEntryAsync(product.Id, dto.BusinessId, dto.BranchId))
            throw new InvalidOperationException("Opening stock has already been applied for this product.");

        var warehouseId = await ResolveOpeningStockWarehouseIdAsync(
            dto.OpeningStockWarehouseId, dto.BusinessId, dto.BranchId);

        var stockChanges = new List<StockChangeItem>();
        var now = DateTime.UtcNow;
        var baseUnit = product.Units.FirstOrDefault(u => u.IsBaseUnit && !u.IsDeleted);

        if (product.OpeningStockVariantWise)
        {
            var lines = dto.OpeningStockByVariant.Where(l => l.Quantity > 0).ToList();
            if (lines.Count == 0)
                return;

            foreach (var line in lines)
            {
                var variant = product.Variants
                    .FirstOrDefault(v => !v.IsDeleted
                        && v.VariantName.Equals(line.VariantName.Trim(), StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException(
                        $"Variant '{line.VariantName}' was not found for opening stock.");

                var unitCost = variant.CostPriceOverride ?? product.CostPrice;

                await _stockLedgerRepository.AddAsync(new StockLedger
                {
                    ProductId = product.Id,
                    VariantId = variant.Id,
                    WarehouseId = warehouseId,
                    Type = StockLedgerType.Opening,
                    ReferenceId = product.Id,
                    QuantityInBaseUnit = line.Quantity,
                    UnitId = baseUnit?.Id,
                    UnitQuantity = line.Quantity,
                    UnitPrice = unitCost,
                    TotalAmount = line.Quantity * unitCost,
                    Date = now,
                    Remarks = $"Opening Stock — Product: {product.ProductCode} | Variant: {variant.VariantName}",
                    BusinessId = dto.BusinessId,
                    BranchId = dto.BranchId
                });

                stockChanges.Add(new StockChangeItem(product.Id, variant.Id, warehouseId));
            }
        }
        else if (dto.OpeningStock > 0)
        {
            await _stockLedgerRepository.AddAsync(new StockLedger
            {
                ProductId = product.Id,
                VariantId = null,
                WarehouseId = warehouseId,
                Type = StockLedgerType.Opening,
                ReferenceId = product.Id,
                QuantityInBaseUnit = dto.OpeningStock,
                UnitId = baseUnit?.Id,
                UnitQuantity = dto.OpeningStock,
                UnitPrice = product.CostPrice,
                TotalAmount = dto.OpeningStock * product.CostPrice,
                Date = now,
                Remarks = $"Opening Stock — Product: {product.ProductCode}",
                BusinessId = dto.BusinessId,
                BranchId = dto.BranchId
            });

            stockChanges.Add(new StockChangeItem(product.Id, null, warehouseId));
        }
        else
        {
            return;
        }

        await _stockLedgerRepository.SaveChangesAsync();

        await _lowStockAlertService.EvaluateAfterStockChangeAsync(
            dto.BusinessId, dto.BranchId, stockChanges);
    }

    private async Task<int> ResolveOpeningStockWarehouseIdAsync(int? requestedWarehouseId, int businessId, int branchId)
    {
        if (requestedWarehouseId.HasValue && requestedWarehouseId.Value > 0)
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(requestedWarehouseId.Value, businessId, branchId);
            if (warehouse == null || warehouse.IsDeleted || !warehouse.IsActive)
                throw new InvalidOperationException("A valid active warehouse is required for opening stock.");
            return warehouse.Id;
        }

        var activeWarehouses = await _warehouseRepository.GetAllActiveAsync(businessId, branchId);
        if (activeWarehouses.Count == 0)
            throw new InvalidOperationException("At least one active warehouse is required to record opening stock.");

        return activeWarehouses[0].Id;
    }

    private async Task<string> ResolveProductCodeAsync(string? requestedCode, int businessId, int branchId)
    {
        var productCode = await _codeGenerator.ResolveAsync(CodeModuleNames.Product, branchId, requestedCode);

        if (await _repository.ProductCodeExistsAsync(productCode, businessId, branchId))
            throw new InvalidOperationException("ProductCode must be unique within the selected branch.");

        return productCode;
    }

    private static void ValidateUnits(List<ProductUnitWriteDto> units)
    {
        if (units.Count == 0)
            throw new InvalidOperationException("At least one product unit is required.");

        if (units.Count(u => u.IsBaseUnit) != 1)
            throw new InvalidOperationException("Exactly one base unit is required.");

        if (units.Any(u => string.IsNullOrWhiteSpace(u.UnitName)))
            throw new InvalidOperationException("UnitName is required for every unit.");

        foreach (var unit in units)
        {
            if (unit.IsBaseUnit)
            {
                UnitConversionHelper.ValidateConversionFactor(true, 1m, unit.UnitName.Trim());
                continue;
            }

            if (unit.ConversionFactor <= 0)
                throw new InvalidOperationException(
                    $"ConversionFactor must be greater than zero for unit '{unit.UnitName.Trim()}'.");

            UnitConversionHelper.ValidateConversionFactor(false, unit.ConversionFactor, unit.UnitName.Trim());
        }

        var duplicateNames = units
            .Select(u => u.UnitName.Trim().ToUpperInvariant())
            .GroupBy(n => n)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateNames.Count > 0)
            throw new InvalidOperationException("Duplicate units are not allowed on the same product.");
    }

    private static void SyncBaseUnitId(ProductEntity product)
    {
        var baseUnit = product.Units.FirstOrDefault(u => u.IsBaseUnit && !u.IsDeleted);
        product.BaseUnitId = baseUnit?.Id > 0 ? baseUnit.Id : null;
    }

    private async Task ValidateBarcodesAsync(List<ProductBarcodeWriteDto> barcodes)
    {
        var values = barcodes
            .Select(b => b.BarcodeValue.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        if (values.Count != values.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            throw new InvalidOperationException("Barcode values must be unique.");

        foreach (var barcode in barcodes.Where(b => !string.IsNullOrWhiteSpace(b.BarcodeValue)))
        {
            if (await _repository.BarcodeExistsAsync(barcode.BarcodeValue, barcode.Id))
                throw new InvalidOperationException($"Barcode '{barcode.BarcodeValue}' already exists.");
        }
    }

    private async Task ReplaceUnitsAsync(ProductEntity product, List<ProductUnitWriteDto> units, int businessId, int branchId)
    {
        var masterUnits = await _unitRepository.GetAllAsync(businessId, branchId, status: true);
        var masterByName = masterUnits.ToDictionary(
            u => u.Name.Trim(),
            u => u,
            StringComparer.OrdinalIgnoreCase);
        var masterById = masterUnits.ToDictionary(u => u.Id);

        product.Units.Clear();
        foreach (var unit in units)
        {
            var unitName = unit.UnitName.Trim();
            if (string.IsNullOrWhiteSpace(unitName) && (!unit.UnitId.HasValue || unit.UnitId.Value <= 0))
                throw new InvalidOperationException("UnitName or UnitId is required for every product unit.");

            if (string.IsNullOrWhiteSpace(unitName) && unit.UnitId.HasValue && masterById.TryGetValue(unit.UnitId.Value, out var named))
                unitName = named.Name.Trim();

            var masterUnit = ResolveMasterUnit(unit, masterByName, masterById);

            var conversionFactor = UnitConversionHelper.ResolveConversionFactor(
                unit.IsBaseUnit,
                unit.ConversionFactor,
                masterUnit.DefaultConversionFactor,
                masterUnit.Name.Trim());

            UnitConversionHelper.ValidateConversionFactor(unit.IsBaseUnit, conversionFactor, masterUnit.Name.Trim());

            product.Units.Add(new ProductUnit
            {
                BusinessId = businessId,
                BranchId = branchId,
                UnitId = masterUnit.Id,
                UnitName = masterUnit.Name.Trim(),
                ConversionFactor = conversionFactor,
                IsBaseUnit = unit.IsBaseUnit,
                IsPriceOverridden = unit.IsBaseUnit ? false : unit.IsPriceOverridden,
                CostPrice = unit.IsPriceOverridden ? unit.CostPrice : null,
                SellingPrice = unit.IsPriceOverridden ? unit.SellingPrice : null,
                WholesalePrice = unit.IsPriceOverridden ? unit.WholesalePrice : null
            });
        }

        _unitPricingService.RecalculateAutoPrices(product);
    }

    private static MeasurementUnit ResolveMasterUnit(
        ProductUnitWriteDto unit,
        Dictionary<string, MeasurementUnit> masterByName,
        Dictionary<int, MeasurementUnit> masterById)
    {
        if (unit.UnitId.HasValue && unit.UnitId.Value > 0)
        {
            if (masterById.TryGetValue(unit.UnitId.Value, out var byId))
                return byId;
            throw new InvalidOperationException($"Unit id {unit.UnitId} was not found in unit master.");
        }

        var unitName = unit.UnitName.Trim();
        if (masterByName.TryGetValue(unitName, out var byName))
            return byName;

        throw new InvalidOperationException($"Unit '{unitName}' was not found in unit master.");
    }

    private void RecalculateUnitPrices(ProductEntity product)
        => _unitPricingService.RecalculateAutoPrices(product);

    private static void ReplaceVariants(ProductEntity product, List<ProductVariantWriteDto> variants, int businessId, int branchId)
    {
        product.Variants.Clear();
        foreach (var variant in variants.Where(v => !string.IsNullOrWhiteSpace(v.VariantName)))
        {
            product.Variants.Add(new ProductVariant
            {
                BusinessId = businessId,
                BranchId = branchId,
                VariantName = variant.VariantName.Trim(),
                Size = variant.Size?.Trim() ?? string.Empty,
                Color = variant.Color?.Trim() ?? string.Empty,
                SKU = variant.SKU?.Trim() ?? string.Empty,
                AdditionalPrice = Math.Max(0, variant.AdditionalPrice),
                CostPriceOverride = variant.CostPriceOverride,
                SellingPriceOverride = variant.SellingPriceOverride,
                Status = variant.Status
            });
        }
    }

    private async Task EnsurePrimaryBarcodeAsync(ProductEntity product, int businessId, int branchId)
    {
        if (product.Barcodes.Any(b => !b.IsDeleted && !string.IsNullOrWhiteSpace(b.BarcodeValue)))
            return;

        var barcode = await _codeGenerator.GenerateBarcodeAsync(businessId, branchId);
        product.Barcodes.Add(new ProductBarcode
        {
            BusinessId = businessId,
            BranchId = branchId,
            BarcodeValue = barcode,
            IsPrimary = true
        });
    }

    private async Task ReplaceBarcodesAsync(ProductEntity product, List<ProductBarcodeWriteDto> barcodes, int businessId, int branchId)
    {
        await ValidateBarcodesAsync(barcodes);

        var incoming = barcodes
            .Where(b => !string.IsNullOrWhiteSpace(b.BarcodeValue))
            .ToList();

        // Remove barcodes that are no longer in the incoming list (avoids soft-delete + same-value insert collision).
        var incomingValues = incoming
            .Select(b => b.BarcodeValue.Trim().ToLowerInvariant())
            .ToHashSet();

        var toRemove = product.Barcodes
            .Where(b => !b.IsDeleted && !incomingValues.Contains(b.BarcodeValue.ToLowerInvariant()))
            .ToList();

        foreach (var old in toRemove)
            product.Barcodes.Remove(old);

        // Add or update each incoming barcode.
        foreach (var dto in incoming)
        {
            var value = dto.BarcodeValue.Trim();
            var existing = product.Barcodes
                .FirstOrDefault(b => !b.IsDeleted &&
                    b.BarcodeValue.Equals(value, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                // Update in-place — no delete/reinsert, so no duplicate-key risk.
                existing.IsPrimary = dto.IsPrimary;
                ResolveUnitVariant(existing, dto, product);
            }
            else
            {
                var entity = new ProductBarcode
                {
                    BusinessId = businessId,
                    BranchId = branchId,
                    BarcodeValue = value,
                    IsPrimary = dto.IsPrimary
                };
                ResolveUnitVariant(entity, dto, product);
                product.Barcodes.Add(entity);
            }
        }
    }

    private static void ResolveUnitVariant(ProductBarcode entity, ProductBarcodeWriteDto dto, ProductEntity product)
    {
        // Prefer name-based resolution because units/variants are replaced (new objects, Id=0 until saved).
        if (!string.IsNullOrWhiteSpace(dto.UnitName))
        {
            var unit = product.Units.FirstOrDefault(u =>
                u.UnitName.Equals(dto.UnitName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (unit != null)
            {
                entity.ProductUnit = unit;
                entity.ProductUnitId = null;
            }
        }
        else if (dto.UnitId > 0)
        {
            entity.ProductUnitId = dto.UnitId;
            entity.ProductUnit = null;
        }
        else
        {
            entity.ProductUnitId = null;
            entity.ProductUnit = null;
        }

        if (!string.IsNullOrWhiteSpace(dto.VariantName))
        {
            var variant = product.Variants.FirstOrDefault(v =>
                v.VariantName.Equals(dto.VariantName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (variant != null)
            {
                entity.ProductVariant = variant;
                entity.ProductVariantId = null;
            }
        }
        else if (dto.VariantId > 0)
        {
            entity.ProductVariantId = dto.VariantId;
            entity.ProductVariant = null;
        }
        else
        {
            entity.ProductVariantId = null;
            entity.ProductVariant = null;
        }
    }

    private async Task<ProductEntity> GetProductOrThrowAsync(int id, int businessId, int branchId)
    {
        return await _repository.GetByIdAsync(id, businessId, branchId)
            ?? throw new InvalidOperationException("Product not found.");
    }

    private static ProductListDto MapListDto(ProductEntity product)
    {
        return new ProductListDto
        {
            Id = product.Id,
            ProductName = product.ProductName,
            ProductCode = product.ProductCode,
            SKU = product.SKU,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            SubCategoryId = product.SubCategoryId,
            SubCategoryName = product.SubCategory?.Name ?? string.Empty,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name ?? string.Empty,
            SellingPrice = product.SellingPrice,
            Status = product.Status,
            HasImage = product.Images.Any(i => !i.IsDeleted),
            IsVariantEnabled = product.IsVariantEnabled,
            BranchId = product.BranchId,
            BranchName = product.Branch?.Name ?? string.Empty,
            AllowNegativeStock = product.AllowNegativeStock,
            EnableLowStockAlert = product.EnableLowStockAlert,
            LowStockAlertLevel = product.LowStockAlertLevel
        };
    }

    private async Task<ProductDetailDto> MapDetailDto(ProductEntity product)
    {
        var openingEntries = await _stockLedgerRepository.GetOpeningEntriesAsync(
            product.Id, product.BusinessId, product.BranchId);
        var hasOpening = openingEntries.Count > 0;

        var dto = new ProductDetailDto
        {
            Id = product.Id,
            ProductName = product.ProductName,
            ProductCode = product.ProductCode,
            SKU = product.SKU,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            SubCategoryId = product.SubCategoryId,
            SubCategoryName = product.SubCategory?.Name ?? string.Empty,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name ?? string.Empty,
            Description = product.Description,
            CostPrice = product.CostPrice,
            SellingPrice = product.SellingPrice,
            WholesalePrice = product.WholesalePrice,
            UseAutoUnitPricing = product.UseAutoUnitPricing,
            Status = product.Status,
            IsVariantEnabled = product.IsVariantEnabled,
            IsDiscountAllowed = product.IsDiscountAllowed,
            DiscountType = product.DiscountType,
            DiscountValue = product.DiscountValue,
            HasImage = product.Images.Any(i => !i.IsDeleted),
            BranchId = product.BranchId,
            BranchName = product.Branch?.Name ?? string.Empty,
            AllowNegativeStock = product.AllowNegativeStock,
            EnableLowStockAlert = product.EnableLowStockAlert,
            LowStockAlertLevel = product.LowStockAlertLevel,
            OpeningStock = product.OpeningStock,
            BaseUnitId = product.BaseUnitId,
            BaseUnitName = product.Units.FirstOrDefault(u => u.IsBaseUnit && !u.IsDeleted)?.UnitName
                ?? product.BaseUnit?.UnitName
                ?? string.Empty,
            HasOpeningStockApplied = hasOpening,
            OpeningStockVariantWise = product.OpeningStockVariantWise,
            OpeningStockByVariant = openingEntries.Select(e => new ProductOpeningStockDto
            {
                VariantId = e.VariantId,
                VariantName = e.Variant?.VariantName ?? string.Empty,
                Quantity = e.QuantityInBaseUnit,
                UnitPrice = e.UnitPrice,
                TotalAmount = e.TotalAmount
            }).ToList()
        };

        dto.Units = product.Units
            .Where(u => !u.IsDeleted)
            .OrderByDescending(u => u.IsBaseUnit)
            .ThenBy(u => u.UnitName)
            .Select(u => new ProductUnitDto
            {
                Id = u.Id,
                UnitId = u.UnitId,
                UnitName = u.UnitName,
                ConversionFactor = u.ConversionFactor,
                IsBaseUnit = u.IsBaseUnit,
                IsPriceOverridden = u.IsPriceOverridden,
                CalculatedSellingPrice = _unitPricingService.CalculateAutoPrice(
                    product.SellingPrice, u.ConversionFactor, u.IsBaseUnit),
                CalculatedWholesalePrice = _unitPricingService.CalculateAutoPrice(
                    product.WholesalePrice, u.ConversionFactor, u.IsBaseUnit),
                CostPrice = u.CostPrice,
                SellingPrice = u.SellingPrice,
                WholesalePrice = u.WholesalePrice
            })
            .ToList();

        dto.Variants = product.Variants
            .Where(v => !v.IsDeleted)
            .OrderBy(v => v.VariantName)
            .Select(v => new ProductVariantDto
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
            })
            .ToList();

        dto.Barcodes = product.Barcodes
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.IsPrimary)
            .ThenBy(b => b.BarcodeValue)
            .Select(b => new ProductBarcodeDto
            {
                Id = b.Id,
                BarcodeValue = b.BarcodeValue,
                UnitId = b.ProductUnitId,
                VariantId = b.ProductVariantId,
                UnitName = b.ProductUnit?.UnitName
                    ?? (b.ProductUnitId.HasValue
                        ? product.Units.FirstOrDefault(u => u.Id == b.ProductUnitId)?.UnitName
                        : null),
                VariantName = b.ProductVariant?.VariantName
                    ?? (b.ProductVariantId.HasValue
                        ? product.Variants.FirstOrDefault(v => v.Id == b.ProductVariantId)?.VariantName
                        : null),
                IsPrimary = b.IsPrimary
            })
            .ToList();

        dto.Images = product.Images
            .Where(i => !i.IsDeleted)
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Select(i => new ProductImageDto
            {
                Id = i.Id,
                FileName = i.FileName,
                ContentType = i.ContentType,
                IsPrimary = i.IsPrimary,
                SortOrder = i.SortOrder
            })
            .ToList();

        return dto;
    }
}
