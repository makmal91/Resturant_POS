using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Common.Helpers;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Application.Product.Interfaces;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Application.StockTransfer.DTOs;
using POSSystem.Application.StockTransfer.Interfaces;
using POSSystem.Domain;
using StockTransferVoucherEntity = POSSystem.Domain.StockTransferVoucher;

namespace POSSystem.Application.StockTransfer.Services;

public class StockTransferService : IStockTransferService
{
    private readonly IStockTransferRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly IStockLedgerRepository _stockLedgerRepository;
    private readonly ILowStockAlertService _lowStockAlertService;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IFeaturePermissionService _featurePermission;

    public StockTransferService(
        IStockTransferRepository repository,
        IProductRepository productRepository,
        IStockLedgerRepository stockLedgerRepository,
        ILowStockAlertService lowStockAlertService,
        ICodeGeneratorService codeGenerator,
        IFeaturePermissionService featurePermission)
    {
        _repository = repository;
        _productRepository = productRepository;
        _stockLedgerRepository = stockLedgerRepository;
        _lowStockAlertService = lowStockAlertService;
        _codeGenerator = codeGenerator;
        _featurePermission = featurePermission;
    }

    public async Task<PagedResultDto<StockTransferVoucherDto>> GetPagedAsync(
        int businessId, int branchId, int page, int pageSize, string? search = null)
    {
        var result = await _repository.GetPagedAsync(businessId, branchId, page, pageSize, search);
        return new PagedResultDto<StockTransferVoucherDto>
        {
            Data = result.Data.Select(MapListDto).ToList(),
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage
        };
    }

    public async Task<StockTransferVoucherDetailDto?> GetByIdAsync(int id, int businessId, int branchId)
    {
        var entity = await _repository.GetByIdWithLinesAsync(id, businessId, branchId);
        return entity == null ? null : MapDetailDto(entity);
    }

    public async Task<StockTransferVoucherDetailDto> CreateAsync(CreateStockTransferVoucherDto dto)
    {
        ValidateWriteDto(dto);

        StockTransferVoucherEntity? created = null;

        await _stockLedgerRepository.RunInSerializableTransactionAsync(async () =>
        {
            var transferNo = await _codeGenerator.GenerateAsync(CodeModuleNames.StockTransfer, dto.BranchId);
            if (await _repository.TransferNoExistsAsync(transferNo, dto.BusinessId, dto.BranchId))
                throw new InvalidOperationException($"Transfer number '{transferNo}' already exists.");

            var lines = await BuildLinesAsync(dto);
            var voucher = new StockTransferVoucherEntity
            {
                TransferNo = transferNo,
                TransferDate = dto.TransferDate,
                Description = dto.Description?.Trim(),
                FromWarehouseId = dto.FromWarehouseId,
                ToWarehouseId = dto.ToWarehouseId,
                BusinessId = dto.BusinessId,
                BranchId = dto.BranchId,
                CreatedBy = dto.CreatedBy,
                Lines = lines
            };

            await _repository.AddAsync(voucher);
            await _repository.SaveChangesAsync();

            var stockChanges = await PostTransferLinesAsync(voucher, lines, dto);
            await _stockLedgerRepository.SaveChangesAsync();

            created = voucher;
            await _lowStockAlertService.EvaluateAfterStockChangeAsync(dto.BusinessId, dto.BranchId, stockChanges);
        });

        var saved = await _repository.GetByIdWithLinesAsync(created!.Id, dto.BusinessId, dto.BranchId);
        return MapDetailDto(saved!);
    }

    public async Task<StockTransferVoucherDetailDto> UpdateAsync(int id, UpdateStockTransferVoucherDto dto)
    {
        ValidateWriteDto(dto);

        var entity = await _repository.GetByIdWithLinesAsync(id, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Stock transfer voucher not found.");

        if (entity.IsReversed)
            throw new InvalidOperationException("Reversed transfers cannot be edited.");

        await _stockLedgerRepository.RunInSerializableTransactionAsync(async () =>
        {
            var activeLines = entity.Lines.Where(l => !l.IsDeleted).ToList();
            var stockChanges = await ReverseTransferLinesAsync(entity, activeLines, dto.BusinessId, dto.BranchId,
                $"Edit — {entity.TransferNo}");

            foreach (var line in activeLines)
            {
                line.IsDeleted = true;
                line.ModifiedAt = DateTime.UtcNow;
                line.ModifiedBy = dto.ModifiedBy;
            }

            var writeDto = new CreateStockTransferVoucherDto
            {
                BusinessId = dto.BusinessId,
                BranchId = dto.BranchId,
                TransferDate = dto.TransferDate,
                Description = dto.Description,
                FromWarehouseId = dto.FromWarehouseId,
                ToWarehouseId = dto.ToWarehouseId,
                CreatedBy = dto.ModifiedBy,
                Lines = dto.Lines
            };

            var newLines = await BuildLinesAsync(writeDto);
            entity.TransferDate = dto.TransferDate;
            entity.Description = dto.Description?.Trim();
            entity.FromWarehouseId = dto.FromWarehouseId;
            entity.ToWarehouseId = dto.ToWarehouseId;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = dto.ModifiedBy;

            foreach (var line in newLines)
            {
                line.VoucherId = entity.Id;
                entity.Lines.Add(line);
            }

            await _repository.SaveChangesAsync();

            stockChanges.AddRange(await PostTransferLinesAsync(entity, newLines, writeDto));
            await _stockLedgerRepository.SaveChangesAsync();

            await _lowStockAlertService.EvaluateAfterStockChangeAsync(dto.BusinessId, dto.BranchId, stockChanges);
        });

        var updated = await _repository.GetByIdWithLinesAsync(id, dto.BusinessId, dto.BranchId);
        return MapDetailDto(updated!);
    }

    public async Task<StockTransferVoucherDetailDto> ReverseAsync(int id, ReverseStockTransferVoucherDto dto)
    {
        if (dto.BranchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        var entity = await _repository.GetByIdWithLinesAsync(id, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Stock transfer voucher not found.");

        if (entity.IsReversed)
            throw new InvalidOperationException("This stock transfer has already been reversed.");

        await _stockLedgerRepository.RunInSerializableTransactionAsync(async () =>
        {
            var reasonSuffix = string.IsNullOrWhiteSpace(dto.Reason) ? string.Empty : $" | {dto.Reason.Trim()}";
            var activeLines = entity.Lines.Where(l => !l.IsDeleted).ToList();
            var stockChanges = await ReverseTransferLinesAsync(
                entity,
                activeLines,
                dto.BusinessId,
                dto.BranchId,
                $"Reverse — {entity.TransferNo}{reasonSuffix}");

            entity.IsReversed = true;
            entity.ReversedAt = DateTime.UtcNow;
            entity.ReversedBy = dto.ReversedBy;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.SaveChangesAsync();

            if (stockChanges.Count > 0)
                await _lowStockAlertService.EvaluateAfterStockChangeAsync(dto.BusinessId, dto.BranchId, stockChanges);
        });

        var updated = await _repository.GetByIdWithLinesAsync(id, dto.BusinessId, dto.BranchId);
        return MapDetailDto(updated!);
    }

    private static void ValidateWriteDto(CreateStockTransferVoucherDto dto)
    {
        if (dto.BranchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        if (dto.FromWarehouseId <= 0 || dto.ToWarehouseId <= 0)
            throw new InvalidOperationException("From and to warehouses are required.");

        if (dto.FromWarehouseId == dto.ToWarehouseId)
            throw new InvalidOperationException("Source and destination warehouses must be different.");

        if (dto.Lines == null || dto.Lines.Count == 0)
            throw new InvalidOperationException("At least one product line is required.");

        ValidateLines(dto.Lines);
    }

    private static void ValidateWriteDto(UpdateStockTransferVoucherDto dto)
    {
        if (dto.BranchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        if (dto.FromWarehouseId <= 0 || dto.ToWarehouseId <= 0)
            throw new InvalidOperationException("From and to warehouses are required.");

        if (dto.FromWarehouseId == dto.ToWarehouseId)
            throw new InvalidOperationException("Source and destination warehouses must be different.");

        if (dto.Lines == null || dto.Lines.Count == 0)
            throw new InvalidOperationException("At least one product line is required.");

        ValidateLines(dto.Lines);
    }

    private static void ValidateLines(List<StockTransferLineWriteDto> lines)
    {
        var duplicateLines = lines
            .GroupBy(l => (l.ProductId, l.VariantId))
            .Where(g => g.Key.ProductId > 0 && g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateLines.Count > 0)
            throw new InvalidOperationException("Each product and variant combination may appear only once.");

        foreach (var line in lines)
        {
            if (line.ProductId <= 0)
                throw new InvalidOperationException("Product is required on every line.");

            if (line.UnitId <= 0)
                throw new InvalidOperationException("Unit is required on every line.");

            if (line.Quantity <= 0)
                throw new InvalidOperationException("Quantity must be greater than zero.");
        }
    }

    private async Task<List<StockTransferVoucherLine>> BuildLinesAsync(CreateStockTransferVoucherDto dto)
    {
        var unitEnabled = await _featurePermission.IsUnitEnabledAsync();
        var variantEnabled = await _featurePermission.IsVariantEnabledAsync();
        var lines = new List<StockTransferVoucherLine>();
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

            var (_, variantId) = variantEnabled
                ? ResolveProductVariant(product, item.VariantId)
                : (null, null);

            var conversionFactor = unitEnabled ? unit.ConversionFactor : 1m;
            var baseQty = unitEnabled
                ? UnitConversionHelper.ToBaseQuantity(item.Quantity, conversionFactor)
                : item.Quantity;

            lines.Add(new StockTransferVoucherLine
            {
                ProductId = item.ProductId,
                VariantId = variantId,
                UnitId = unit.Id,
                UnitQuantity = item.Quantity,
                ConversionFactor = conversionFactor,
                Quantity = baseQty,
                BusinessId = dto.BusinessId,
                BranchId = dto.BranchId,
                CreatedBy = dto.CreatedBy
            });
        }

        return lines;
    }

    private async Task<List<StockChangeItem>> PostTransferLinesAsync(
        StockTransferVoucherEntity voucher,
        IReadOnlyList<StockTransferVoucherLine> lines,
        CreateStockTransferVoucherDto dto)
    {
        var stockChanges = new List<StockChangeItem>();
        var now = DateTime.UtcNow;

        foreach (var line in lines)
        {
            var available = await _stockLedgerRepository.GetCurrentStockAsync(
                dto.BusinessId, dto.BranchId, line.ProductId, line.VariantId, dto.FromWarehouseId);

            if (available < line.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for product id {line.ProductId}. Available: {available}, Requested: {line.Quantity}.");
            }

            var product = await _productRepository.GetByIdAsync(line.ProductId, dto.BusinessId, dto.BranchId)
                ?? throw new InvalidOperationException($"Product id {line.ProductId} was not found.");

            var variantName = line.VariantId.HasValue
                ? product.Variants.FirstOrDefault(v => v.Id == line.VariantId)?.VariantName ?? string.Empty
                : string.Empty;

            var remarks = string.IsNullOrWhiteSpace(variantName)
                ? $"Transfer {voucher.TransferNo} — {product.ProductCode}"
                : $"Transfer {voucher.TransferNo} — {product.ProductCode} | {variantName}";

            await _stockLedgerRepository.AddAsync(new StockLedger
            {
                ProductId = line.ProductId,
                VariantId = line.VariantId,
                WarehouseId = dto.FromWarehouseId,
                Type = StockLedgerType.TransferOut,
                ReferenceId = voucher.Id,
                QuantityInBaseUnit = -line.Quantity,
                UnitId = line.UnitId,
                UnitQuantity = line.UnitQuantity > 0 ? -line.UnitQuantity : null,
                UnitPrice = 0,
                TotalAmount = 0,
                Date = now,
                Remarks = remarks,
                BusinessId = dto.BusinessId,
                BranchId = dto.BranchId
            });

            await _stockLedgerRepository.AddAsync(new StockLedger
            {
                ProductId = line.ProductId,
                VariantId = line.VariantId,
                WarehouseId = dto.ToWarehouseId,
                Type = StockLedgerType.TransferIn,
                ReferenceId = voucher.Id,
                QuantityInBaseUnit = line.Quantity,
                UnitId = line.UnitId,
                UnitQuantity = line.UnitQuantity,
                UnitPrice = 0,
                TotalAmount = 0,
                Date = now,
                Remarks = remarks,
                BusinessId = dto.BusinessId,
                BranchId = dto.BranchId
            });

            stockChanges.Add(new StockChangeItem(line.ProductId, line.VariantId, dto.FromWarehouseId));
            stockChanges.Add(new StockChangeItem(line.ProductId, line.VariantId, dto.ToWarehouseId));
        }

        return stockChanges;
    }

    private async Task<List<StockChangeItem>> ReverseTransferLinesAsync(
        StockTransferVoucherEntity voucher,
        IReadOnlyList<StockTransferVoucherLine> lines,
        int businessId,
        int branchId,
        string remarks)
    {
        if (lines.Count == 0)
            return [];

        var stockChanges = new List<StockChangeItem>();
        var now = DateTime.UtcNow;

        foreach (var line in lines)
        {
            await _stockLedgerRepository.AddAsync(new StockLedger
            {
                ProductId = line.ProductId,
                VariantId = line.VariantId,
                WarehouseId = voucher.FromWarehouseId,
                Type = StockLedgerType.TransferIn,
                ReferenceId = voucher.Id,
                QuantityInBaseUnit = line.Quantity,
                UnitId = line.UnitId,
                UnitQuantity = line.UnitQuantity,
                UnitPrice = 0,
                TotalAmount = 0,
                Date = now,
                Remarks = remarks,
                BusinessId = businessId,
                BranchId = branchId
            });

            await _stockLedgerRepository.AddAsync(new StockLedger
            {
                ProductId = line.ProductId,
                VariantId = line.VariantId,
                WarehouseId = voucher.ToWarehouseId,
                Type = StockLedgerType.TransferOut,
                ReferenceId = voucher.Id,
                QuantityInBaseUnit = -line.Quantity,
                UnitId = line.UnitId,
                UnitQuantity = line.UnitQuantity > 0 ? -line.UnitQuantity : null,
                UnitPrice = 0,
                TotalAmount = 0,
                Date = now,
                Remarks = remarks,
                BusinessId = businessId,
                BranchId = branchId
            });

            stockChanges.Add(new StockChangeItem(line.ProductId, line.VariantId, voucher.FromWarehouseId));
            stockChanges.Add(new StockChangeItem(line.ProductId, line.VariantId, voucher.ToWarehouseId));
        }

        return stockChanges;
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

    private static StockTransferVoucherDto MapListDto(StockTransferVoucherEntity entity) => new()
    {
        Id = entity.Id,
        TransferNo = entity.TransferNo,
        TransferDate = entity.TransferDate,
        Description = entity.Description,
        FromWarehouseId = entity.FromWarehouseId,
        FromWarehouseName = entity.FromWarehouse?.Name ?? string.Empty,
        ToWarehouseId = entity.ToWarehouseId,
        ToWarehouseName = entity.ToWarehouse?.Name ?? string.Empty,
        LineCount = entity.Lines.Count(l => !l.IsDeleted),
        BranchId = entity.BranchId,
        BranchName = entity.Branch?.Name ?? string.Empty,
        CreatedBy = entity.CreatedBy,
        CreatedAt = entity.CreatedAt,
        IsReversed = entity.IsReversed,
        ReversedAt = entity.ReversedAt
    };

    private static StockTransferVoucherDetailDto MapDetailDto(StockTransferVoucherEntity entity)
    {
        return new StockTransferVoucherDetailDto
        {
            Id = entity.Id,
            TransferNo = entity.TransferNo,
            TransferDate = entity.TransferDate,
            Description = entity.Description,
            FromWarehouseId = entity.FromWarehouseId,
            FromWarehouseName = entity.FromWarehouse?.Name ?? string.Empty,
            ToWarehouseId = entity.ToWarehouseId,
            ToWarehouseName = entity.ToWarehouse?.Name ?? string.Empty,
            LineCount = entity.Lines.Count(l => !l.IsDeleted),
            BranchId = entity.BranchId,
            BranchName = entity.Branch?.Name ?? string.Empty,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            IsReversed = entity.IsReversed,
            ReversedAt = entity.ReversedAt,
            Lines = entity.Lines
                .Where(l => !l.IsDeleted)
                .Select(l => new StockTransferLineDto
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
                    BaseUnitName = l.Product?.BaseUnit?.UnitName ?? string.Empty
                })
                .ToList()
        };
    }
}
