using Microsoft.Extensions.Options;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Common.Helpers;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Application.Product.Interfaces;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Application.StockAdjustment.DTOs;
using POSSystem.Application.StockAdjustment.Interfaces;
using POSSystem.Application.StockAdjustment.Options;
using POSSystem.Domain;
using StockAdjustmentEntity = POSSystem.Domain.StockAdjustment;

namespace POSSystem.Application.StockAdjustment.Services;

public class StockAdjustmentService : IStockAdjustmentService
{
    private readonly IStockAdjustmentRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly IStockLedgerRepository _stockLedgerRepository;
    private readonly IStockValidationService _stockValidation;
    private readonly ILowStockAlertService _lowStockAlertService;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IAccountingIntegrationService _accountingIntegration;
    private readonly IFeaturePermissionService _featurePermission;
    private readonly StockAdjustmentOptions _options;

    public StockAdjustmentService(
        IStockAdjustmentRepository repository,
        IProductRepository productRepository,
        IStockLedgerRepository stockLedgerRepository,
        IStockValidationService stockValidation,
        ILowStockAlertService lowStockAlertService,
        ICodeGeneratorService codeGenerator,
        IAccountingIntegrationService accountingIntegration,
        IFeaturePermissionService featurePermission,
        IOptions<StockAdjustmentOptions> options)
    {
        _repository = repository;
        _productRepository = productRepository;
        _stockLedgerRepository = stockLedgerRepository;
        _stockValidation = stockValidation;
        _lowStockAlertService = lowStockAlertService;
        _codeGenerator = codeGenerator;
        _accountingIntegration = accountingIntegration;
        _featurePermission = featurePermission;
        _options = options.Value;
    }

    public async Task<PagedResultDto<StockAdjustmentDto>> GetPagedAsync(StockAdjustmentFilterDto filter)
    {
        var result = await _repository.GetPagedAsync(filter);
        return new PagedResultDto<StockAdjustmentDto>
        {
            Data = result.Data.Select(MapListDto).ToList(),
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage
        };
    }

    public async Task<StockAdjustmentDetailDto?> GetByIdAsync(int id, int businessId, int branchId)
    {
        var entity = await _repository.GetByIdWithLinesAsync(id, businessId, branchId);
        return entity == null ? null : MapDetailDto(entity);
    }

    public async Task<IReadOnlyList<AdjustmentTypeDto>> GetAdjustmentTypesAsync(int businessId, int branchId)
    {
        var types = await _repository.GetActiveAdjustmentTypesAsync(businessId, branchId);
        return types.Select(t => new AdjustmentTypeDto
        {
            Id = t.Id,
            Name = t.Name,
            ExpenseAccountId = t.ExpenseAccountId,
            ExpenseAccountName = t.ExpenseAccount?.Name ?? string.Empty,
            IncomeAccountId = t.IncomeAccountId,
            IncomeAccountName = t.IncomeAccount?.Name ?? string.Empty,
            IsActive = t.IsActive
        }).ToList();
    }

    public async Task<StockAdjustmentDetailDto> CreateAsync(CreateStockAdjustmentDto dto)
    {
        ValidateWriteDto(dto);

        StockAdjustmentEntity? created = null;

        await _stockLedgerRepository.RunInSerializableTransactionAsync(async () =>
        {
            var adjustmentType = await RequireAdjustmentTypeAsync(dto.AdjustmentTypeId, dto.BusinessId, dto.BranchId);
            var adjustmentNo = await _codeGenerator.GenerateAsync(CodeModuleNames.StockAdjustment, dto.BranchId);

            if (await _repository.AdjustmentNoExistsAsync(adjustmentNo, dto.BusinessId, dto.BranchId))
                throw new InvalidOperationException($"Adjustment number '{adjustmentNo}' already exists.");

            var lines = await BuildLinesAsync(dto, dto.BusinessId, dto.BranchId);
            await ValidateStockAsync(dto.BusinessId, dto.BranchId, dto.WarehouseId, lines);

            var (gain, loss, total) = ComputeAmounts(lines);
            var adjustment = new StockAdjustmentEntity
            {
                AdjustmentNo = adjustmentNo,
                AdjustmentDate = dto.AdjustmentDate,
                WarehouseId = dto.WarehouseId,
                AdjustmentTypeId = dto.AdjustmentTypeId,
                Remarks = dto.Remarks?.Trim(),
                TotalAmount = total,
                BusinessId = dto.BusinessId,
                BranchId = dto.BranchId,
                CreatedBy = dto.CreatedBy,
                Lines = lines
            };

            await _repository.AddAsync(adjustment);
            await _repository.SaveChangesAsync();

            var stockChanges = await PostStockAsync(adjustment, lines, dto.WarehouseId, dto.BusinessId, dto.BranchId);
            await _stockLedgerRepository.SaveChangesAsync();

            if (_options.EnableStockAdjustmentAccounting)
                await _accountingIntegration.PostStockAdjustmentAsync(adjustment, adjustmentType, gain, loss);

            created = adjustment;
            await _lowStockAlertService.EvaluateAfterStockChangeAsync(dto.BusinessId, dto.BranchId, stockChanges);
        });

        var saved = await _repository.GetByIdWithLinesAsync(created!.Id, dto.BusinessId, dto.BranchId);
        return MapDetailDto(saved!);
    }

    public async Task<StockAdjustmentDetailDto> UpdateAsync(int id, UpdateStockAdjustmentDto dto)
    {
        ValidateWriteDto(dto);

        var entity = await _repository.GetByIdWithLinesAsync(id, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Stock adjustment not found.");

        if (entity.IsReversed)
            throw new InvalidOperationException("Reversed adjustments cannot be edited.");

        await _stockLedgerRepository.RunInSerializableTransactionAsync(async () =>
        {
            var adjustmentType = await RequireAdjustmentTypeAsync(dto.AdjustmentTypeId, dto.BusinessId, dto.BranchId);

            if (_options.EnableStockAdjustmentAccounting)
            {
                await _accountingIntegration.ReverseTransactionAsync(
                    id, GlTransactionType.StockAdjustmentVoucher, $"Edit — {entity.AdjustmentNo}");
            }

            var stockChanges = await ReverseStockAsync(entity, dto.BusinessId, dto.BranchId, $"Edit — {entity.AdjustmentNo}");

            foreach (var line in entity.Lines.Where(l => !l.IsDeleted))
            {
                line.IsDeleted = true;
                line.ModifiedAt = DateTime.UtcNow;
                line.ModifiedBy = dto.ModifiedBy;
            }

            var createDto = new CreateStockAdjustmentDto
            {
                BusinessId = dto.BusinessId,
                BranchId = dto.BranchId,
                WarehouseId = dto.WarehouseId,
                AdjustmentTypeId = dto.AdjustmentTypeId,
                AdjustmentDate = dto.AdjustmentDate,
                Remarks = dto.Remarks,
                CreatedBy = dto.ModifiedBy,
                Lines = dto.Lines
            };

            var newLines = await BuildLinesAsync(createDto, dto.BusinessId, dto.BranchId);
            await ValidateStockAsync(dto.BusinessId, dto.BranchId, dto.WarehouseId, newLines);

            var (gain, loss, total) = ComputeAmounts(newLines);

            entity.AdjustmentDate = dto.AdjustmentDate;
            entity.WarehouseId = dto.WarehouseId;
            entity.AdjustmentTypeId = dto.AdjustmentTypeId;
            entity.Remarks = dto.Remarks?.Trim();
            entity.TotalAmount = total;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = dto.ModifiedBy;

            foreach (var line in newLines)
            {
                line.StockAdjustmentId = entity.Id;
                entity.Lines.Add(line);
            }

            await _repository.SaveChangesAsync();

            stockChanges.AddRange(await PostStockAsync(entity, newLines, dto.WarehouseId, dto.BusinessId, dto.BranchId));
            await _stockLedgerRepository.SaveChangesAsync();

            if (_options.EnableStockAdjustmentAccounting)
                await _accountingIntegration.PostStockAdjustmentAsync(entity, adjustmentType, gain, loss);

            await _lowStockAlertService.EvaluateAfterStockChangeAsync(dto.BusinessId, dto.BranchId, stockChanges);
        });

        var updated = await _repository.GetByIdWithLinesAsync(id, dto.BusinessId, dto.BranchId);
        return MapDetailDto(updated!);
    }

    public async Task DeleteAsync(int id, int businessId, int branchId, int? deletedBy)
    {
        var entity = await _repository.GetByIdWithLinesAsync(id, businessId, branchId)
            ?? throw new InvalidOperationException("Stock adjustment not found.");

        if (entity.IsReversed)
            return;

        await ReverseInternalAsync(entity, businessId, branchId, deletedBy, "Delete", markDeleted: true);
    }

    public async Task<StockAdjustmentDetailDto> ReverseAsync(
        int id, int businessId, int branchId, int? reversedBy, string? reason = null)
    {
        var entity = await _repository.GetByIdWithLinesAsync(id, businessId, branchId)
            ?? throw new InvalidOperationException("Stock adjustment not found.");

        if (entity.IsReversed)
            throw new InvalidOperationException("This stock adjustment has already been reversed.");

        await ReverseInternalAsync(entity, businessId, branchId, reversedBy, reason, markDeleted: false);

        var saved = await _repository.GetByIdWithLinesAsync(id, businessId, branchId);
        return MapDetailDto(saved!);
    }

    public async Task<IReadOnlyList<StockAdjustmentReportRowDto>> GetReportAsync(StockAdjustmentFilterDto filter)
    {
        filter.Page = 1;
        filter.PageSize = MaxPageSize;
        var result = await _repository.GetPagedAsync(filter);
        return result.Data.Select(a =>
        {
            var activeLines = a.Lines.Where(l => !l.IsDeleted).ToList();
            var gain = activeLines.Where(l => l.BaseQuantity > 0).Sum(l => l.TotalCost);
            var loss = activeLines.Where(l => l.BaseQuantity < 0).Sum(l => l.TotalCost);
            return new StockAdjustmentReportRowDto
            {
                Id = a.Id,
                AdjustmentNo = a.AdjustmentNo,
                AdjustmentDate = a.AdjustmentDate,
                WarehouseName = a.Warehouse?.Name ?? string.Empty,
                AdjustmentTypeName = a.AdjustmentType?.Name ?? string.Empty,
                GainAmount = gain,
                LossAmount = loss,
                NetAmount = gain - loss,
                IsReversed = a.IsReversed
            };
        }).ToList();
    }

    private const int MaxPageSize = 500;

    private async Task ReverseInternalAsync(
        StockAdjustmentEntity entity,
        int businessId,
        int branchId,
        int? userId,
        string? reason,
        bool markDeleted)
    {
        await _stockLedgerRepository.RunInSerializableTransactionAsync(async () =>
        {
            var reasonSuffix = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" | {reason.Trim()}";

            if (_options.EnableStockAdjustmentAccounting)
            {
                await _accountingIntegration.ReverseTransactionAsync(
                    entity.Id,
                    GlTransactionType.StockAdjustmentVoucher,
                    $"{(markDeleted ? "Delete" : "Reverse")} — {entity.AdjustmentNo}{reasonSuffix}");
            }

            var stockChanges = await ReverseStockAsync(
                entity, businessId, branchId, $"{(markDeleted ? "Delete" : "Reverse")} — {entity.AdjustmentNo}{reasonSuffix}");

            entity.IsReversed = true;
            entity.ReversedAt = DateTime.UtcNow;
            entity.ReversedBy = userId;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = userId;

            if (markDeleted)
                entity.IsDeleted = true;

            await _repository.SaveChangesAsync();
            await _stockLedgerRepository.SaveChangesAsync();
            await _lowStockAlertService.EvaluateAfterStockChangeAsync(businessId, branchId, stockChanges);
        });
    }

    private async Task<AdjustmentType> RequireAdjustmentTypeAsync(int id, int businessId, int branchId)
    {
        return await _repository.GetAdjustmentTypeAsync(id, businessId, branchId)
            ?? throw new InvalidOperationException("Adjustment type not found or inactive.");
    }

    private static void ValidateWriteDto(CreateStockAdjustmentDto dto) =>
        ValidateWriteDtoCore(dto.BranchId, dto.WarehouseId, dto.AdjustmentTypeId, dto.Lines);

    private static void ValidateWriteDto(UpdateStockAdjustmentDto dto) =>
        ValidateWriteDtoCore(dto.BranchId, dto.WarehouseId, dto.AdjustmentTypeId, dto.Lines);

    private static void ValidateWriteDtoCore(
        int branchId, int warehouseId, int adjustmentTypeId, List<StockAdjustmentLineWriteDto>? lines)
    {
        if (branchId <= 0)
            throw new InvalidOperationException("BranchId is required.");
        if (warehouseId <= 0)
            throw new InvalidOperationException("Warehouse is required.");
        if (adjustmentTypeId <= 0)
            throw new InvalidOperationException("Adjustment type is required.");
        if (lines == null || lines.Count == 0)
            throw new InvalidOperationException("At least one product line is required.");

        foreach (var line in lines)
        {
            if (line.ProductId <= 0)
                throw new InvalidOperationException("Product is required on every line.");
            if (line.UnitId <= 0)
                throw new InvalidOperationException("Unit is required on every line.");
            if (line.Quantity == 0)
                throw new InvalidOperationException("Quantity cannot be zero.");
            if (line.CostPrice < 0)
                throw new InvalidOperationException("Cost price cannot be negative.");
        }
    }

    private async Task<List<StockAdjustmentLine>> BuildLinesAsync(
        CreateStockAdjustmentDto dto, int businessId, int branchId)
    {
        var unitEnabled = await _featurePermission.IsUnitEnabledAsync();
        var variantEnabled = await _featurePermission.IsVariantEnabledAsync();
        var lines = new List<StockAdjustmentLine>();
        var productCache = new Dictionary<int, Domain.Product>();

        foreach (var item in dto.Lines)
        {
            if (!productCache.TryGetValue(item.ProductId, out var product))
            {
                product = await _productRepository.GetByIdAsync(item.ProductId, businessId, branchId)
                    ?? throw new InvalidOperationException($"Product id {item.ProductId} was not found.");
                productCache[item.ProductId] = product;
            }

            if (!product.Status)
                throw new InvalidOperationException($"Product '{product.ProductName}' is inactive.");

            var unit = unitEnabled
                ? product.Units.FirstOrDefault(u => u.Id == item.UnitId && !u.IsDeleted)
                    ?? throw new InvalidOperationException($"Unit {item.UnitId} is not valid for product '{product.ProductName}'.")
                : product.Units.FirstOrDefault(u => u.IsBaseUnit && !u.IsDeleted)
                    ?? product.Units.FirstOrDefault(u => !u.IsDeleted)
                    ?? throw new InvalidOperationException($"Product '{product.ProductName}' has no unit configured.");

            if (unitEnabled)
                UnitConversionHelper.ValidateConversionFactor(unit.IsBaseUnit, unit.ConversionFactor, unit.UnitName);

            var (variant, variantId) = variantEnabled
                ? ResolveProductVariant(product, item.VariantId)
                : (null, null);

            var signedUnitQty = item.Quantity;
            var conversionFactor = unitEnabled ? unit.ConversionFactor : 1m;
            var baseQty = unitEnabled
                ? UnitConversionHelper.ToBaseQuantity(Math.Abs(signedUnitQty), conversionFactor) * Math.Sign(signedUnitQty)
                : signedUnitQty;

            var costPrice = item.CostPrice > 0
                ? item.CostPrice
                : ResolveUnitCostPrice(product, unit, variant);

            var totalCost = Math.Round(Math.Abs(signedUnitQty) * costPrice, 2, MidpointRounding.AwayFromZero);

            lines.Add(new StockAdjustmentLine
            {
                ProductId = item.ProductId,
                VariantId = variantId,
                UnitId = unit.Id,
                UnitQuantity = signedUnitQty,
                ConversionFactor = conversionFactor,
                BaseQuantity = baseQty,
                CostPrice = costPrice,
                TotalCost = totalCost,
                BusinessId = businessId,
                BranchId = branchId,
                CreatedBy = dto.CreatedBy
            });
        }

        return lines;
    }

    private async Task ValidateStockAsync(
        int businessId, int branchId, int warehouseId, IReadOnlyList<StockAdjustmentLine> lines)
    {
        if (_options.AllowNegativeStock)
            return;

        var requirements = new List<StockRequirement>();
        foreach (var group in lines.Where(l => l.BaseQuantity < 0).GroupBy(l => new { l.ProductId, l.VariantId }))
        {
            var product = await _productRepository.GetByIdAsync(group.Key.ProductId, businessId, branchId);
            var variantName = group.Key.VariantId.HasValue
                ? product?.Variants.FirstOrDefault(v => v.Id == group.Key.VariantId)?.VariantName
                : null;

            requirements.Add(new StockRequirement
            {
                ProductId = group.Key.ProductId,
                VariantId = group.Key.VariantId,
                BaseQuantity = Math.Abs(group.Sum(x => x.BaseQuantity)),
                ProductName = product?.ProductName ?? $"Product #{group.Key.ProductId}",
                VariantName = variantName,
                BaseUnitName = product?.BaseUnit?.UnitName ?? "base unit"
            });
        }

        if (requirements.Count > 0)
            await _stockValidation.ValidateAvailabilityAsync(businessId, branchId, warehouseId, requirements);
    }

    private async Task<List<StockChangeItem>> PostStockAsync(
        StockAdjustmentEntity adjustment,
        IReadOnlyList<StockAdjustmentLine> lines,
        int warehouseId,
        int businessId,
        int branchId)
    {
        var stockChanges = new List<StockChangeItem>();
        var now = DateTime.UtcNow;

        foreach (var line in lines)
        {
            var product = await _productRepository.GetByIdAsync(line.ProductId, businessId, branchId)
                ?? throw new InvalidOperationException($"Product id {line.ProductId} was not found.");

            var variantName = line.VariantId.HasValue
                ? product.Variants.FirstOrDefault(v => v.Id == line.VariantId)?.VariantName ?? string.Empty
                : string.Empty;

            var remarks = string.IsNullOrWhiteSpace(variantName)
                ? $"Stock Adjustment — {adjustment.AdjustmentNo} | Product: {product.ProductCode}"
                : $"Stock Adjustment — {adjustment.AdjustmentNo} | Product: {product.ProductCode} | Variant: {variantName}";

            await _stockLedgerRepository.AddAsync(new StockLedger
            {
                ProductId = line.ProductId,
                VariantId = line.VariantId,
                WarehouseId = warehouseId,
                Type = StockLedgerType.Adjustment,
                ReferenceId = adjustment.Id,
                QuantityInBaseUnit = line.BaseQuantity,
                UnitId = line.UnitId,
                UnitQuantity = line.UnitQuantity,
                UnitPrice = line.CostPrice,
                TotalAmount = line.TotalCost,
                Date = now,
                Remarks = remarks,
                BusinessId = businessId,
                BranchId = branchId
            });

            stockChanges.Add(new StockChangeItem(line.ProductId, line.VariantId, warehouseId));
        }

        return stockChanges;
    }

    private async Task<List<StockChangeItem>> ReverseStockAsync(
        StockAdjustmentEntity adjustment,
        int businessId,
        int branchId,
        string remarks)
    {
        var lineKeys = adjustment.Lines
            .Where(l => !l.IsDeleted)
            .Select(l => (l.ProductId, l.VariantId))
            .ToHashSet();

        var entries = (await _stockLedgerRepository.GetByReferenceAsync(
                adjustment.Id, businessId, branchId, StockLedgerType.Adjustment, StockLedgerType.AdjustmentReversal))
            .Where(e => lineKeys.Contains((e.ProductId, e.VariantId)))
            .ToList();

        if (entries.Count == 0)
            return [];

        var now = DateTime.UtcNow;
        var stockChanges = new List<StockChangeItem>();
        var groups = entries.GroupBy(e => (e.ProductId, e.VariantId, e.WarehouseId));

        foreach (var group in groups)
        {
            var netQty = group.Sum(e => e.QuantityInBaseUnit);
            if (Math.Abs(netQty) <= 0.0001m)
                continue;

            var netAmount = group.Sum(e =>
                e.Type == StockLedgerType.Adjustment ? e.TotalAmount : -e.TotalAmount);
            if (netAmount <= 0)
                continue;

            var template = group
                .Where(e => e.Type == StockLedgerType.Adjustment)
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
                Type = StockLedgerType.AdjustmentReversal,
                ReferenceId = adjustment.Id,
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

    private static (decimal Gain, decimal Loss, decimal Total) ComputeAmounts(IEnumerable<StockAdjustmentLine> lines)
    {
        var active = lines.ToList();
        var gain = active.Where(l => l.BaseQuantity > 0).Sum(l => l.TotalCost);
        var loss = active.Where(l => l.BaseQuantity < 0).Sum(l => l.TotalCost);
        return (gain, loss, gain + loss);
    }

    private static (ProductVariant? Variant, int? VariantId) ResolveProductVariant(Domain.Product product, int? requestedVariantId)
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

        throw new InvalidOperationException($"Product '{product.ProductName}' requires a variant selection.");
    }

    private static decimal ResolveUnitCostPrice(Domain.Product product, ProductUnit unit, ProductVariant? variant)
    {
        if (unit.CostPrice.HasValue && unit.CostPrice.Value >= 0)
            return unit.CostPrice.Value;

        var perBaseCost = variant?.CostPriceOverride ?? product.CostPrice;
        var factor = unit.IsBaseUnit ? 1m : (unit.ConversionFactor > 0 ? unit.ConversionFactor : 1m);
        return perBaseCost * factor;
    }

    private static StockAdjustmentDto MapListDto(StockAdjustmentEntity entity)
    {
        var activeLines = entity.Lines.Where(l => !l.IsDeleted).ToList();
        var gain = activeLines.Where(l => l.BaseQuantity > 0).Sum(l => l.TotalCost);
        var loss = activeLines.Where(l => l.BaseQuantity < 0).Sum(l => l.TotalCost);

        return new StockAdjustmentDto
        {
            Id = entity.Id,
            AdjustmentNo = entity.AdjustmentNo,
            AdjustmentDate = entity.AdjustmentDate,
            WarehouseId = entity.WarehouseId,
            WarehouseName = entity.Warehouse?.Name ?? string.Empty,
            AdjustmentTypeId = entity.AdjustmentTypeId,
            AdjustmentTypeName = entity.AdjustmentType?.Name ?? string.Empty,
            Remarks = entity.Remarks,
            TotalAmount = entity.TotalAmount,
            GainAmount = gain,
            LossAmount = loss,
            LineCount = activeLines.Count,
            BranchId = entity.BranchId,
            BranchName = entity.Branch?.Name ?? string.Empty,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            IsReversed = entity.IsReversed,
            ReversedAt = entity.ReversedAt
        };
    }

    private static StockAdjustmentDetailDto MapDetailDto(StockAdjustmentEntity entity)
    {
        var dto = new StockAdjustmentDetailDto
        {
            Id = entity.Id,
            AdjustmentNo = entity.AdjustmentNo,
            AdjustmentDate = entity.AdjustmentDate,
            WarehouseId = entity.WarehouseId,
            WarehouseName = entity.Warehouse?.Name ?? string.Empty,
            AdjustmentTypeId = entity.AdjustmentTypeId,
            AdjustmentTypeName = entity.AdjustmentType?.Name ?? string.Empty,
            Remarks = entity.Remarks,
            TotalAmount = entity.TotalAmount,
            BranchId = entity.BranchId,
            BranchName = entity.Branch?.Name ?? string.Empty,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            IsReversed = entity.IsReversed,
            ReversedAt = entity.ReversedAt,
            Lines = entity.Lines
                .Where(l => !l.IsDeleted)
                .Select(l => new StockAdjustmentLineDto
                {
                    Id = l.Id,
                    ProductId = l.ProductId,
                    ProductName = l.Product?.ProductName ?? string.Empty,
                    ProductCode = l.Product?.ProductCode ?? string.Empty,
                    VariantId = l.VariantId,
                    VariantName = l.Variant?.VariantName ?? string.Empty,
                    UnitId = l.UnitId,
                    UnitName = l.Unit?.UnitName ?? string.Empty,
                    BaseUnitName = l.Product?.BaseUnit?.UnitName ?? string.Empty,
                    UnitQuantity = l.UnitQuantity,
                    ConversionFactor = l.ConversionFactor,
                    BaseQuantity = l.BaseQuantity,
                    CostPrice = l.CostPrice,
                    TotalCost = l.TotalCost
                }).ToList()
        };

        dto.GainAmount = dto.Lines.Where(l => l.BaseQuantity > 0).Sum(l => l.TotalCost);
        dto.LossAmount = dto.Lines.Where(l => l.BaseQuantity < 0).Sum(l => l.TotalCost);
        dto.LineCount = dto.Lines.Count;
        return dto;
    }
}
