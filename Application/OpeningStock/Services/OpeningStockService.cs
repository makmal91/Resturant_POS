using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Common.Helpers;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Application.OpeningStock.DTOs;
using POSSystem.Application.OpeningStock.Interfaces;
using POSSystem.Application.Product.Interfaces;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Domain;
using OpeningStockVoucherEntity = POSSystem.Domain.OpeningStockVoucher;

namespace POSSystem.Application.OpeningStock.Services;

public class OpeningStockService : IOpeningStockService
{
    private readonly IOpeningStockRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly IStockLedgerRepository _stockLedgerRepository;
    private readonly ILowStockAlertService _lowStockAlertService;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IAccountingIntegrationService _accountingIntegration;
    private readonly IFeaturePermissionService _featurePermission;

    public OpeningStockService(
        IOpeningStockRepository repository,
        IProductRepository productRepository,
        IStockLedgerRepository stockLedgerRepository,
        ILowStockAlertService lowStockAlertService,
        ICodeGeneratorService codeGenerator,
        IAccountingIntegrationService accountingIntegration,
        IFeaturePermissionService featurePermission)
    {
        _repository = repository;
        _productRepository = productRepository;
        _stockLedgerRepository = stockLedgerRepository;
        _lowStockAlertService = lowStockAlertService;
        _codeGenerator = codeGenerator;
        _accountingIntegration = accountingIntegration;
        _featurePermission = featurePermission;
    }

    public async Task<PagedResultDto<OpeningStockVoucherDto>> GetPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null)
    {
        var result = await _repository.GetPagedAsync(businessId, branchId, page, pageSize, search);
        return new PagedResultDto<OpeningStockVoucherDto>
        {
            Data = result.Data.Select(MapListDto).ToList(),
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage
        };
    }

    public async Task<OpeningStockVoucherDetailDto?> GetByIdAsync(int id, int businessId, int branchId)
    {
        var entity = await _repository.GetByIdWithLinesAsync(id, businessId, branchId);
        return entity == null ? null : MapDetailDto(entity);
    }

    public async Task<OpeningStockVoucherDetailDto> CreateAsync(CreateOpeningStockVoucherDto dto)
    {
        ValidateDto(dto);

        OpeningStockVoucherEntity? created = null;

        await _stockLedgerRepository.RunInSerializableTransactionAsync(async () =>
        {
            var voucherNo = await _codeGenerator.GenerateAsync(CodeModuleNames.OpeningStock, dto.BranchId);

            if (await _repository.VoucherNoExistsAsync(voucherNo, dto.BusinessId, dto.BranchId))
                throw new InvalidOperationException($"Voucher number '{voucherNo}' already exists.");

            var lines = await BuildLinesAsync(dto);
            var totalAmount = Math.Round(lines.Sum(l => l.TotalAmount), 2, MidpointRounding.AwayFromZero);

            var voucher = new OpeningStockVoucherEntity
            {
                VoucherNo = voucherNo,
                VoucherDate = dto.VoucherDate,
                Description = dto.Description?.Trim(),
                WarehouseId = dto.WarehouseId,
                TotalAmount = totalAmount,
                BusinessId = dto.BusinessId,
                BranchId = dto.BranchId,
                CreatedBy = dto.CreatedBy,
                Lines = lines
            };

            await _repository.AddAsync(voucher);
            await _repository.SaveChangesAsync();

            var stockChanges = await PostStockForVoucherAsync(
                voucher, lines, dto.WarehouseId, dto.BusinessId, dto.BranchId);

            await _stockLedgerRepository.SaveChangesAsync();
            await _accountingIntegration.PostOpeningStockVoucherAsync(voucher, totalAmount);

            created = voucher;

            await _lowStockAlertService.EvaluateAfterStockChangeAsync(
                dto.BusinessId, dto.BranchId, stockChanges);
        });

        var saved = await _repository.GetByIdWithLinesAsync(created!.Id, dto.BusinessId, dto.BranchId);
        return MapDetailDto(saved!);
    }

    public async Task<OpeningStockVoucherDetailDto> UpdateAsync(int id, UpdateOpeningStockVoucherDto dto)
    {
        ValidateWriteDto(dto);

        var entity = await _repository.GetByIdWithLinesAsync(id, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Opening stock voucher not found.");

        if (entity.IsReversed)
            throw new InvalidOperationException("Reversed vouchers cannot be edited.");

        await _stockLedgerRepository.RunInSerializableTransactionAsync(async () =>
        {
            await _accountingIntegration.ReverseTransactionAsync(
                id,
                GlTransactionType.OpeningStockVoucher,
                $"Edit — {entity.VoucherNo}");

            var stockChanges = await ReverseStockForVoucherAsync(
                entity, dto.BusinessId, dto.BranchId, $"Edit — {entity.VoucherNo}");

            foreach (var line in entity.Lines.Where(l => !l.IsDeleted))
            {
                line.IsDeleted = true;
                line.ModifiedAt = DateTime.UtcNow;
                line.ModifiedBy = dto.ModifiedBy;
            }

            var writeDto = new CreateOpeningStockVoucherDto
            {
                BusinessId = dto.BusinessId,
                BranchId = dto.BranchId,
                VoucherDate = dto.VoucherDate,
                Description = dto.Description,
                WarehouseId = dto.WarehouseId,
                CreatedBy = dto.ModifiedBy,
                Lines = dto.Lines
            };

            var newLines = await BuildLinesAsync(writeDto);
            var totalAmount = Math.Round(newLines.Sum(l => l.TotalAmount), 2, MidpointRounding.AwayFromZero);

            entity.VoucherDate = dto.VoucherDate;
            entity.Description = dto.Description?.Trim();
            entity.WarehouseId = dto.WarehouseId;
            entity.TotalAmount = totalAmount;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = dto.ModifiedBy;

            foreach (var line in newLines)
            {
                line.VoucherId = entity.Id;
                entity.Lines.Add(line);
            }

            await _repository.SaveChangesAsync();

            stockChanges.AddRange(await PostStockForVoucherAsync(
                entity, newLines, dto.WarehouseId, dto.BusinessId, dto.BranchId));

            await _stockLedgerRepository.SaveChangesAsync();
            await _accountingIntegration.PostOpeningStockVoucherAsync(entity, totalAmount);

            await _lowStockAlertService.EvaluateAfterStockChangeAsync(
                dto.BusinessId, dto.BranchId, stockChanges);
        });

        var updated = await _repository.GetByIdWithLinesAsync(id, dto.BusinessId, dto.BranchId);
        return MapDetailDto(updated!);
    }

    public async Task<OpeningStockVoucherDetailDto> ReverseAsync(int id, ReverseOpeningStockVoucherDto dto)
    {
        if (dto.BranchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        var entity = await _repository.GetByIdWithLinesAsync(id, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Opening stock voucher not found.");

        if (entity.IsReversed)
            throw new InvalidOperationException("This opening stock voucher has already been reversed.");

        await _stockLedgerRepository.RunInSerializableTransactionAsync(async () =>
        {
            var reasonSuffix = string.IsNullOrWhiteSpace(dto.Reason) ? string.Empty : $" | {dto.Reason.Trim()}";
            await _accountingIntegration.ReverseTransactionAsync(
                id,
                GlTransactionType.OpeningStockVoucher,
                $"Reverse — {entity.VoucherNo}{reasonSuffix}");

            var stockChanges = await ReverseStockForVoucherAsync(
                entity, dto.BusinessId, dto.BranchId, $"Reverse of Opening Stock — Voucher: {entity.VoucherNo}{reasonSuffix}");

            entity.IsReversed = true;
            entity.ReversedAt = DateTime.UtcNow;
            entity.ReversedBy = dto.ReversedBy;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.SaveChangesAsync();

            if (stockChanges.Count > 0)
            {
                await _lowStockAlertService.EvaluateAfterStockChangeAsync(
                    dto.BusinessId, dto.BranchId, stockChanges);
            }
        });

        var updated = await _repository.GetByIdWithLinesAsync(id, dto.BusinessId, dto.BranchId);
        return MapDetailDto(updated!);
    }

    private static void ValidateDto(CreateOpeningStockVoucherDto dto) => ValidateWriteDto(dto);

    private static void ValidateWriteDto(CreateOpeningStockVoucherDto dto)
    {
        if (dto.BranchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        if (dto.WarehouseId <= 0)
            throw new InvalidOperationException("Warehouse is required.");

        if (dto.Lines == null || dto.Lines.Count == 0)
            throw new InvalidOperationException("At least one product line is required.");

        var duplicateLines = dto.Lines
            .GroupBy(l => (l.ProductId, l.VariantId))
            .Where(g => g.Key.ProductId > 0 && g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateLines.Count > 0)
            throw new InvalidOperationException("Each product and variant combination may appear only once.");

        foreach (var line in dto.Lines)
        {
            if (line.ProductId <= 0)
                throw new InvalidOperationException("Product is required on every line.");

            if (line.UnitId <= 0)
                throw new InvalidOperationException("Unit is required on every line.");

            if (line.Quantity <= 0)
                throw new InvalidOperationException("Quantity must be greater than zero.");

            if (line.CostPrice <= 0)
                throw new InvalidOperationException("Cost price must be greater than zero.");
        }
    }

    private static void ValidateWriteDto(UpdateOpeningStockVoucherDto dto)
    {
        if (dto.BranchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        if (dto.WarehouseId <= 0)
            throw new InvalidOperationException("Warehouse is required.");

        if (dto.Lines == null || dto.Lines.Count == 0)
            throw new InvalidOperationException("At least one product line is required.");

        var duplicateLines = dto.Lines
            .GroupBy(l => (l.ProductId, l.VariantId))
            .Where(g => g.Key.ProductId > 0 && g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateLines.Count > 0)
            throw new InvalidOperationException("Each product and variant combination may appear only once.");

        foreach (var line in dto.Lines)
        {
            if (line.ProductId <= 0)
                throw new InvalidOperationException("Product is required on every line.");

            if (line.UnitId <= 0)
                throw new InvalidOperationException("Unit is required on every line.");

            if (line.Quantity <= 0)
                throw new InvalidOperationException("Quantity must be greater than zero.");

            if (line.CostPrice <= 0)
                throw new InvalidOperationException("Cost price must be greater than zero.");
        }
    }

    private async Task<List<StockChangeItem>> PostStockForVoucherAsync(
        OpeningStockVoucherEntity voucher,
        IReadOnlyList<OpeningStockVoucherLine> lines,
        int warehouseId,
        int businessId,
        int branchId)
    {
        var stockChanges = new List<StockChangeItem>();
        var now = DateTime.UtcNow;

        foreach (var line in lines)
        {
            if (await _stockLedgerRepository.HasOpeningEntryForVariantAsync(
                    line.ProductId, line.VariantId, businessId, branchId))
            {
                var variantLabel = line.VariantId.HasValue ? $" variant id {line.VariantId}" : string.Empty;
                throw new InvalidOperationException(
                    $"Opening stock has already been recorded for product id {line.ProductId}{variantLabel}.");
            }

            var product = await _productRepository.GetByIdAsync(line.ProductId, businessId, branchId)
                ?? throw new InvalidOperationException($"Product id {line.ProductId} was not found.");

            var variantName = line.VariantId.HasValue
                ? product.Variants.FirstOrDefault(v => v.Id == line.VariantId)?.VariantName ?? string.Empty
                : string.Empty;

            var remarks = string.IsNullOrWhiteSpace(variantName)
                ? $"Opening Stock — Voucher: {voucher.VoucherNo} | Product: {product.ProductCode}"
                : $"Opening Stock — Voucher: {voucher.VoucherNo} | Product: {product.ProductCode} | Variant: {variantName}";

            await _stockLedgerRepository.AddAsync(new StockLedger
            {
                ProductId = line.ProductId,
                VariantId = line.VariantId,
                WarehouseId = warehouseId,
                Type = StockLedgerType.Opening,
                ReferenceId = voucher.Id,
                QuantityInBaseUnit = line.Quantity,
                UnitId = line.UnitId,
                UnitQuantity = line.UnitQuantity,
                UnitPrice = line.CostPrice,
                TotalAmount = line.TotalAmount,
                Date = now,
                Remarks = remarks,
                BusinessId = businessId,
                BranchId = branchId
            });

            stockChanges.Add(new StockChangeItem(line.ProductId, line.VariantId, warehouseId));
        }

        return stockChanges;
    }

    private async Task<List<StockChangeItem>> ReverseStockForVoucherAsync(
        OpeningStockVoucherEntity voucher,
        int businessId,
        int branchId,
        string remarks)
    {
        var entries = await _stockLedgerRepository.GetByReferenceAsync(
            voucher.Id, businessId, branchId, StockLedgerType.Opening, StockLedgerType.OpeningReversal);

        if (entries.Count == 0)
            return [];

        var now = DateTime.UtcNow;
        var stockChanges = new List<StockChangeItem>();
        var groups = entries.GroupBy(e => (e.ProductId, e.VariantId, e.WarehouseId));

        foreach (var group in groups)
        {
            var netQty = group.Sum(e => e.QuantityInBaseUnit);
            if (netQty <= 0.0001m)
                continue;

            var netAmount = group.Sum(e =>
                e.Type == StockLedgerType.Opening ? e.TotalAmount : -e.TotalAmount);
            if (netAmount <= 0)
                continue;

            var template = group
                .Where(e => e.Type == StockLedgerType.Opening)
                .OrderByDescending(e => e.Id)
                .First();

            decimal? unitQty = null;
            if (template.UnitQuantity.HasValue && template.QuantityInBaseUnit != 0)
            {
                var factor = template.QuantityInBaseUnit / template.UnitQuantity.Value;
                unitQty = factor != 0 ? -(netQty / factor) : -template.UnitQuantity.Value;
            }

            await _stockLedgerRepository.AddAsync(new StockLedger
            {
                ProductId = group.Key.ProductId,
                VariantId = group.Key.VariantId,
                WarehouseId = group.Key.WarehouseId,
                Type = StockLedgerType.OpeningReversal,
                ReferenceId = voucher.Id,
                QuantityInBaseUnit = -netQty,
                UnitId = template.UnitId,
                UnitQuantity = unitQty,
                UnitPrice = template.UnitPrice,
                TotalAmount = netAmount,
                Date = now,
                Remarks = remarks,
                BusinessId = businessId,
                BranchId = branchId
            });

            stockChanges.Add(new StockChangeItem(group.Key.ProductId, group.Key.VariantId, group.Key.WarehouseId));
        }

        return stockChanges;
    }

    private async Task<List<OpeningStockVoucherLine>> BuildLinesAsync(CreateOpeningStockVoucherDto dto)
    {
        var unitEnabled = await _featurePermission.IsUnitEnabledAsync();
        var variantEnabled = await _featurePermission.IsVariantEnabledAsync();
        var lines = new List<OpeningStockVoucherLine>();
        var productCache = new Dictionary<int, Domain.Product>();

        foreach (var item in dto.Lines)
        {
            if (!productCache.TryGetValue(item.ProductId, out var product))
            {
                product = await _productRepository.GetByIdAsync(item.ProductId, dto.BusinessId, dto.BranchId)
                    ?? throw new InvalidOperationException($"Product id {item.ProductId} was not found.");
                productCache[item.ProductId] = product;
            }

            if (!product.Status)
                throw new InvalidOperationException($"Product '{product.ProductName}' is inactive.");

            var unit = unitEnabled
                ? product.Units.FirstOrDefault(u => u.Id == item.UnitId && !u.IsDeleted)
                    ?? throw new InvalidOperationException(
                        $"Unit {item.UnitId} is not valid for product '{product.ProductName}'.")
                : product.Units.FirstOrDefault(u => u.IsBaseUnit && !u.IsDeleted)
                    ?? product.Units.FirstOrDefault(u => !u.IsDeleted)
                    ?? throw new InvalidOperationException(
                        $"Product '{product.ProductName}' has no base unit configured.");

            if (unitEnabled)
                UnitConversionHelper.ValidateConversionFactor(unit.IsBaseUnit, unit.ConversionFactor, unit.UnitName);

            var (variant, variantId) = variantEnabled
                ? ResolveProductVariant(product, item.VariantId)
                : (null, null);

            var conversionFactor = unitEnabled ? unit.ConversionFactor : 1m;
            var baseQty = unitEnabled
                ? UnitConversionHelper.ToBaseQuantity(item.Quantity, conversionFactor)
                : item.Quantity;
            var costPrice = item.CostPrice > 0
                ? item.CostPrice
                : ResolveUnitCostPrice(product, unit, variant);
            var totalAmount = Math.Round(item.Quantity * costPrice, 2, MidpointRounding.AwayFromZero);

            lines.Add(new OpeningStockVoucherLine
            {
                ProductId = item.ProductId,
                VariantId = variantId,
                UnitId = unit.Id,
                UnitQuantity = item.Quantity,
                ConversionFactor = conversionFactor,
                Quantity = baseQty,
                CostPrice = costPrice,
                TotalAmount = totalAmount,
                BusinessId = dto.BusinessId,
                BranchId = dto.BranchId,
                CreatedBy = dto.CreatedBy
            });
        }

        return lines;
    }

    private static (ProductVariant? Variant, int? VariantId) ResolveProductVariant(
        Domain.Product product, int? requestedVariantId)
    {
        var activeVariants = product.Variants.Where(v => !v.IsDeleted && v.Status).ToList();
        var hasVariants = product.IsVariantEnabled || activeVariants.Count > 0;

        if (!hasVariants)
            return (null, null);

        if (requestedVariantId.HasValue)
        {
            var matched = activeVariants.FirstOrDefault(v => v.Id == requestedVariantId.Value)
                ?? throw new InvalidOperationException(
                    $"Variant {requestedVariantId} is not valid for product '{product.ProductName}'.");
            return (matched, matched.Id);
        }

        throw new InvalidOperationException(
            $"Product '{product.ProductName}' requires a variant selection.");
    }

    private static decimal ResolveUnitCostPrice(Domain.Product product, ProductUnit unit, ProductVariant? variant)
    {
        if (unit.CostPrice.HasValue && unit.CostPrice.Value >= 0)
            return unit.CostPrice.Value;

        var perBaseCost = variant?.CostPriceOverride ?? product.CostPrice;
        var factor = unit.IsBaseUnit ? 1m : (unit.ConversionFactor > 0 ? unit.ConversionFactor : 1m);
        return perBaseCost * factor;
    }

    private static OpeningStockVoucherDto MapListDto(OpeningStockVoucherEntity entity) => new()
    {
        Id = entity.Id,
        VoucherNo = entity.VoucherNo,
        VoucherDate = entity.VoucherDate,
        Description = entity.Description,
        WarehouseId = entity.WarehouseId,
        WarehouseName = entity.Warehouse?.Name ?? string.Empty,
        TotalAmount = entity.TotalAmount,
        BranchId = entity.BranchId,
        BranchName = entity.Branch?.Name ?? string.Empty,
        CreatedBy = entity.CreatedBy,
        CreatedAt = entity.CreatedAt,
        IsReversed = entity.IsReversed,
        ReversedAt = entity.ReversedAt,
        ReferenceVoucherId = entity.ReferenceVoucherId,
        ReversalVoucherId = entity.ReversalVoucherId
    };

    private static OpeningStockVoucherDetailDto MapDetailDto(OpeningStockVoucherEntity entity)
    {
        var dto = new OpeningStockVoucherDetailDto
        {
            Id = entity.Id,
            VoucherNo = entity.VoucherNo,
            VoucherDate = entity.VoucherDate,
            Description = entity.Description,
            WarehouseId = entity.WarehouseId,
            WarehouseName = entity.Warehouse?.Name ?? string.Empty,
            TotalAmount = entity.TotalAmount,
            BranchId = entity.BranchId,
            BranchName = entity.Branch?.Name ?? string.Empty,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            IsReversed = entity.IsReversed,
            ReversedAt = entity.ReversedAt,
            ReferenceVoucherId = entity.ReferenceVoucherId,
            ReversalVoucherId = entity.ReversalVoucherId,
            Lines = entity.Lines
                .Where(l => !l.IsDeleted)
                .Select(l => new OpeningStockLineDto
                {
                    Id = l.Id,
                    ProductId = l.ProductId,
                    ProductName = l.Product?.ProductName ?? string.Empty,
                    ProductCode = l.Product?.ProductCode ?? string.Empty,
                    VariantId = l.VariantId,
                    VariantName = l.Variant?.VariantName ?? string.Empty,
                    UnitId = l.UnitId,
                    UnitName = l.Unit?.UnitName ?? string.Empty,
                    UnitQuantity = l.UnitQuantity,
                    ConversionFactor = l.ConversionFactor,
                    Quantity = l.Quantity,
                    BaseUnitName = l.Product?.BaseUnit?.UnitName ?? string.Empty,
                    CostPrice = l.CostPrice,
                    TotalAmount = l.TotalAmount
                })
                .ToList()
        };

        return dto;
    }
}
