using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Common.Helpers;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Application.Ledger.Interfaces;
using POSSystem.Application.Payments.DTOs;
using POSSystem.Application.Payments.Interfaces;
using POSSystem.Application.Product.Interfaces;
using POSSystem.Application.Purchase.DTOs;
using POSSystem.Application.Purchase.Interfaces;
using POSSystem.Application.Sales.DTOs;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Purchase.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockLedgerRepository _stockLedgerRepository;
    private readonly ILowStockAlertService _lowStockAlertService;
    private readonly IPartyLedgerService _partyLedgerService;
    private readonly IInvoicePaymentService _invoicePaymentService;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IFeaturePermissionService _featurePermission;

    public PurchaseService(
        IPurchaseRepository purchaseRepository,
        IProductRepository productRepository,
        IStockLedgerRepository stockLedgerRepository,
        ILowStockAlertService lowStockAlertService,
        IPartyLedgerService partyLedgerService,
        IInvoicePaymentService invoicePaymentService,
        ICodeGeneratorService codeGenerator,
        IFeaturePermissionService featurePermission)
    {
        _purchaseRepository = purchaseRepository;
        _productRepository = productRepository;
        _stockLedgerRepository = stockLedgerRepository;
        _lowStockAlertService = lowStockAlertService;
        _partyLedgerService = partyLedgerService;
        _invoicePaymentService = invoicePaymentService;
        _codeGenerator = codeGenerator;
        _featurePermission = featurePermission;
    }

    public async Task<PagedResultDto<PurchaseDto>> GetPurchasesPagedAsync(
        int businessId, int branchId, int page, int pageSize, string? search = null, PurchaseStatus? status = null)
    {
        var result = await _purchaseRepository.GetPagedAsync(businessId, branchId, page, pageSize, search, status);
        var purchaseIds = result.Data.Select(p => p.Id).ToList();
        var paidMap = await _invoicePaymentService.GetPaidTotalsForPurchasesAsync(purchaseIds, businessId, branchId);

        return new PagedResultDto<PurchaseDto>
        {
            Data = result.Data.Select(p => MapDto(p, paidMap.GetValueOrDefault(p.Id))).ToList(),
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage
        };
    }

    public async Task<PurchaseDetailDto?> GetPurchaseByIdAsync(int id, int businessId, int branchId)
    {
        var entity = await _purchaseRepository.GetByIdWithItemsAsync(id, businessId, branchId);
        if (entity == null) return null;

        var paid = await _invoicePaymentService.GetTotalPaidForPurchaseAsync(id, businessId, branchId);
        var payments = await _invoicePaymentService.GetPaymentsForPurchaseAsync(id, businessId, branchId);
        return MapDetailDto(entity, paid, payments);
    }

    public async Task<PurchaseDetailDto> CreatePurchaseAsync(CreatePurchaseDto dto)
    {
        ValidatePurchaseDto(dto.BranchId, dto.SupplierId, dto.WarehouseId, dto.Items);

        var invoiceNo = await _codeGenerator.ResolveAsync(CodeModuleNames.Purchase, dto.BranchId, dto.InvoiceNo);

        if (await _purchaseRepository.InvoiceExistsAsync(invoiceNo, dto.BusinessId, dto.BranchId))
            throw new InvalidOperationException($"Invoice number '{invoiceNo}' already exists.");

        var purchase = new Domain.Purchase
        {
            InvoiceNo = invoiceNo,
            SupplierId = dto.SupplierId,
            WarehouseId = dto.WarehouseId,
            PurchaseDate = dto.PurchaseDate,
            Notes = dto.Notes?.Trim() ?? string.Empty,
            IsCreditPurchase = dto.IsCreditPurchase,
            Status = PurchaseStatus.Draft,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId
        };

        await BuildItemsAsync(purchase, dto.Items, dto.BusinessId, dto.BranchId);

        await _purchaseRepository.AddAsync(purchase);
        await _purchaseRepository.SaveChangesAsync();

        var created = await _purchaseRepository.GetByIdWithItemsAsync(purchase.Id, dto.BusinessId, dto.BranchId);
        return await MapDetailDtoAsync(created!, dto.BusinessId, dto.BranchId);
    }

    public async Task<PurchaseDetailDto?> UpdatePurchaseAsync(int id, UpdatePurchaseDto dto)
    {
        var entity = await _purchaseRepository.GetByIdWithItemsAsync(id, dto.BusinessId, dto.BranchId);
        if (entity == null)
            throw new InvalidOperationException("Purchase not found.");

        ValidatePurchaseDto(dto.BranchId, dto.SupplierId, dto.WarehouseId, dto.Items);

        if (await _purchaseRepository.InvoiceExistsAsync(dto.InvoiceNo, dto.BusinessId, dto.BranchId, id))
            throw new InvalidOperationException($"Invoice number '{dto.InvoiceNo}' already exists.");

        // If purchase is already Posted, reverse existing stock ledger entries first
        if (entity.Status == PurchaseStatus.Posted && await _featurePermission.IsStockEnabledAsync())
        {
            var originalEntries = await _stockLedgerRepository.GetByReferenceAsync(
                id, dto.BusinessId, dto.BranchId, StockLedgerType.PurchaseEntry);

            var reversals = originalEntries.Select(e => new StockLedger
            {
                ProductId          = e.ProductId,
                VariantId          = e.VariantId,
                WarehouseId        = e.WarehouseId,
                Type               = StockLedgerType.PurchaseReversal,
                ReferenceId        = id,
                QuantityInBaseUnit = -e.QuantityInBaseUnit,
                UnitId             = e.UnitId,
                UnitQuantity       = e.UnitQuantity.HasValue ? -e.UnitQuantity.Value : null,
                UnitPrice          = e.UnitPrice,
                TotalAmount        = e.TotalAmount,
                Date               = DateTime.UtcNow,
                Remarks            = $"Correction Reversal — Invoice: {entity.InvoiceNo}",
                BusinessId         = dto.BusinessId,
                BranchId           = dto.BranchId
            }).ToList();

            await _stockLedgerRepository.AddRangeAsync(reversals);
        }

        entity.InvoiceNo    = dto.InvoiceNo.Trim();
        entity.SupplierId   = dto.SupplierId;
        entity.WarehouseId  = dto.WarehouseId;
        entity.PurchaseDate = dto.PurchaseDate;
        entity.Notes        = dto.Notes?.Trim() ?? string.Empty;
        entity.IsCreditPurchase = dto.IsCreditPurchase;
        foreach (var item in entity.Items)
            item.IsDeleted = true;

        await BuildItemsAsync(entity, dto.Items, dto.BusinessId, dto.BranchId);

        // If was Posted, re-apply new stock entries and keep Posted status
        if (entity.Status == PurchaseStatus.Posted && await _featurePermission.IsStockEnabledAsync())
        {
            var activeItems = entity.Items.Where(i => !i.IsDeleted).ToList();
            foreach (var item in activeItems)
            {
                await _stockLedgerRepository.AddAsync(CreatePurchaseStockLedgerEntry(
                    item, entity.WarehouseId, entity.Id, entity.InvoiceNo, entity.PurchaseDate,
                    dto.BusinessId, dto.BranchId, corrected: true));
            }
        }

        await _purchaseRepository.SaveChangesAsync();
        if (entity.Status == PurchaseStatus.Posted && await _featurePermission.IsStockEnabledAsync())
            await _stockLedgerRepository.SaveChangesAsync();

        if (entity.Status == PurchaseStatus.Posted && await _featurePermission.IsStockEnabledAsync())
        {
            await _lowStockAlertService.EvaluateAfterStockChangeAsync(
                dto.BusinessId,
                dto.BranchId,
                entity.Items
                    .Where(i => !i.IsDeleted)
                    .Select(i => new StockChangeItem(i.ProductId, i.VariantId, entity.WarehouseId)));
        }

        var updated = await _purchaseRepository.GetByIdWithItemsAsync(id, dto.BusinessId, dto.BranchId);
        return await MapDetailDtoAsync(updated!, dto.BusinessId, dto.BranchId);
    }

    public async Task<PurchaseDetailDto> PostPurchaseAsync(int id, PostPurchaseDto dto)
    {
        var entity = await _purchaseRepository.GetByIdWithItemsAsync(id, dto.BusinessId, dto.BranchId);
        if (entity == null)
            throw new InvalidOperationException("Purchase not found.");

        if (entity.Status == PurchaseStatus.Posted)
            throw new InvalidOperationException("Purchase is already posted.");

        if (!entity.Items.Any(i => !i.IsDeleted))
            throw new InvalidOperationException("Cannot post a purchase with no items.");

        var activeItems = entity.Items.Where(i => !i.IsDeleted).ToList();

        if (await _featurePermission.IsStockEnabledAsync())
        {
            foreach (var item in activeItems)
            {
                item.BaseQuantity = UnitConversionHelper.ToBaseQuantity(item.Quantity, item.ConversionFactor);

                await _stockLedgerRepository.AddAsync(CreatePurchaseStockLedgerEntry(
                    item, entity.WarehouseId, entity.Id, entity.InvoiceNo, entity.PurchaseDate,
                    dto.BusinessId, dto.BranchId));
            }

            await _stockLedgerRepository.SaveChangesAsync();

            await _lowStockAlertService.EvaluateAfterStockChangeAsync(
                dto.BusinessId,
                dto.BranchId,
                activeItems.Select(i => new StockChangeItem(i.ProductId, i.VariantId, entity.WarehouseId)));
        }
        else
        {
            foreach (var item in activeItems)
                item.BaseQuantity = item.Quantity;
        }

        entity.TotalAmount = activeItems.Sum(i => i.TotalCost);
        entity.Status = PurchaseStatus.Posted;

        await _purchaseRepository.SaveChangesAsync();

        if (entity.IsCreditPurchase)
        {
            await _partyLedgerService.RecordCreditPurchaseAsync(
                dto.BusinessId, dto.BranchId, entity.SupplierId,
                entity.Id, entity.InvoiceNo, entity.TotalAmount, entity.PurchaseDate);
        }

        var posted = await _purchaseRepository.GetByIdWithItemsAsync(id, dto.BusinessId, dto.BranchId);
        return await MapDetailDtoAsync(posted!, dto.BusinessId, dto.BranchId);
    }

    public async Task DeletePurchaseAsync(int id, int businessId, int branchId)
    {
        var entity = await _purchaseRepository.GetByIdAsync(id, businessId, branchId);
        if (entity == null)
            throw new InvalidOperationException("Purchase not found.");

        if (entity.Status == PurchaseStatus.Posted)
            throw new InvalidOperationException("Cannot delete a posted purchase. Cancel it instead.");

        entity.IsDeleted = true;
        await _purchaseRepository.SaveChangesAsync();
    }

    private static void ValidatePurchaseDto(int branchId, int supplierId, int warehouseId, List<CreatePurchaseItemDto> items)
    {
        if (branchId <= 0) throw new InvalidOperationException("BranchId is required.");
        if (supplierId <= 0) throw new InvalidOperationException("SupplierId is required.");
        if (warehouseId <= 0) throw new InvalidOperationException("WarehouseId is required.");
        if (items == null || items.Count == 0) throw new InvalidOperationException("At least one purchase item is required.");

        foreach (var item in items)
        {
            if (item.ProductId <= 0) throw new InvalidOperationException("ProductId is required for all items.");
            if (item.UnitId <= 0) throw new InvalidOperationException("UnitId is required for all items.");
            if (item.Quantity <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");
        }
    }

    private async Task BuildItemsAsync(
        Domain.Purchase purchase, List<CreatePurchaseItemDto> dtoItems, int businessId, int branchId)
    {
        var unitEnabled = await _featurePermission.IsUnitEnabledAsync();
        var variantEnabled = await _featurePermission.IsVariantEnabledAsync();
        var productCache = new Dictionary<int, Domain.Product>();
        decimal total = 0;

        foreach (var i in dtoItems)
        {
            if (!productCache.TryGetValue(i.ProductId, out var product))
            {
                product = await _productRepository.GetByIdAsync(i.ProductId, businessId, branchId)
                    ?? throw new InvalidOperationException($"Product {i.ProductId} not found.");
                productCache[i.ProductId] = product;
            }

            var unit = unitEnabled
                ? product.Units.FirstOrDefault(u => u.Id == i.UnitId && !u.IsDeleted)
                    ?? throw new InvalidOperationException(
                        $"Unit {i.UnitId} is not valid for product '{product.ProductName}'. No conversion mapping exists.")
                : product.Units.FirstOrDefault(u => u.IsBaseUnit && !u.IsDeleted)
                    ?? product.Units.FirstOrDefault(u => !u.IsDeleted)
                    ?? throw new InvalidOperationException(
                        $"Product '{product.ProductName}' has no base unit configured.");

            if (unitEnabled)
                UnitConversionHelper.ValidateConversionFactor(unit.IsBaseUnit, unit.ConversionFactor, unit.UnitName);

            ProductVariant? variant = null;
            int? variantId = null;
            if (variantEnabled)
                (variant, variantId) = ResolveProductVariant(product, i.VariantId);

            var conversionFactor = unitEnabled ? unit.ConversionFactor : 1m;
            var costPrice = ResolveUnitCostPrice(product, unit, variant);
            var baseQty = unitEnabled
                ? UnitConversionHelper.ToBaseQuantity(i.Quantity, conversionFactor)
                : i.Quantity;
            var totalCost = i.Quantity * costPrice;
            total += totalCost;

            purchase.Items.Add(new PurchaseItem
            {
                ProductId = i.ProductId,
                VariantId = variantId,
                UnitId = unit.Id,
                Quantity = i.Quantity,
                ConversionFactor = conversionFactor,
                BaseQuantity = baseQty,
                CostPrice = costPrice,
                TotalCost = totalCost,
                BusinessId = businessId,
                BranchId = branchId
            });
        }

        purchase.TotalAmount = total;
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

        var fallback = activeVariants.FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"Product '{product.ProductName}' requires a variant selection.");

        return (fallback, fallback.Id);
    }

    private static decimal ResolveUnitCostPrice(Domain.Product product, ProductUnit unit, ProductVariant? variant)
    {
        if (unit.CostPrice.HasValue && unit.CostPrice.Value >= 0)
            return unit.CostPrice.Value;

        var baseCost = variant?.CostPriceOverride ?? product.CostPrice;
        var factor = unit.ConversionFactor > 0 ? unit.ConversionFactor : 1m;
        return Math.Round(baseCost / factor, 2, MidpointRounding.AwayFromZero);
    }

    private static StockLedger CreatePurchaseStockLedgerEntry(
        PurchaseItem item,
        int warehouseId,
        int purchaseId,
        string invoiceNo,
        DateTime purchaseDate,
        int businessId,
        int branchId,
        bool corrected = false)
    {
        return new StockLedger
        {
            ProductId = item.ProductId,
            VariantId = item.VariantId,
            WarehouseId = warehouseId,
            Type = StockLedgerType.PurchaseEntry,
            ReferenceId = purchaseId,
            QuantityInBaseUnit = item.BaseQuantity,
            UnitId = item.UnitId,
            UnitQuantity = item.Quantity,
            UnitPrice = item.CostPrice,
            TotalAmount = item.TotalCost,
            Date = purchaseDate,
            Remarks = corrected
                ? $"Purchase (Corrected) — Invoice: {invoiceNo}"
                : $"Purchase Entry — Invoice: {invoiceNo}",
            BusinessId = businessId,
            BranchId = branchId
        };
    }

    public async Task<PurchaseDetailDto> VoidPurchaseAsync(int id, VoidPurchaseDto dto)
    {
        var entity = await _purchaseRepository.GetByIdWithItemsAsync(id, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Purchase not found.");

        if (entity.Status != PurchaseStatus.Posted)
            throw new InvalidOperationException(
                $"Only posted purchases can be voided. Current status: {entity.Status}.");

        // Reverse all PurchaseEntry records for this purchase
        var originalEntries = await _stockLedgerRepository.GetByReferenceAsync(
            id, dto.BusinessId, dto.BranchId, StockLedgerType.PurchaseEntry);

        var reversals = originalEntries.Select(e => new StockLedger
        {
            ProductId          = e.ProductId,
            VariantId          = e.VariantId,
            WarehouseId        = e.WarehouseId,
            Type               = StockLedgerType.PurchaseReversal,
            ReferenceId        = id,
            QuantityInBaseUnit = -e.QuantityInBaseUnit,
            UnitId             = e.UnitId,
            UnitQuantity       = e.UnitQuantity.HasValue ? -e.UnitQuantity.Value : null,
            UnitPrice          = e.UnitPrice,
            TotalAmount        = e.TotalAmount,
            Date               = DateTime.UtcNow,
            Remarks            = $"Void of Purchase — Invoice: {entity.InvoiceNo}" +
                                 (string.IsNullOrWhiteSpace(dto.Reason) ? "" : $" | Reason: {dto.Reason}"),
            BusinessId         = dto.BusinessId,
            BranchId           = dto.BranchId
        }).ToList();

        await _stockLedgerRepository.AddRangeAsync(reversals);

        if (entity.IsCreditPurchase)
        {
            await _partyLedgerService.ReverseCreditPurchaseAsync(
                dto.BusinessId, dto.BranchId, entity.SupplierId,
                entity.Id, entity.InvoiceNo, entity.TotalAmount, DateTime.UtcNow, dto.Reason);
        }

        entity.Status       = PurchaseStatus.Cancelled;
        entity.VoidedAt     = DateTime.UtcNow;
        entity.VoidedByName = dto.VoidedByName;

        await _purchaseRepository.SaveChangesAsync();
        await _stockLedgerRepository.SaveChangesAsync();

        await _lowStockAlertService.EvaluateAfterStockChangeAsync(
            dto.BusinessId,
            dto.BranchId,
            reversals.Select(r => new StockChangeItem(r.ProductId, r.VariantId, r.WarehouseId)));

        var result = await _purchaseRepository.GetByIdWithItemsAsync(id, dto.BusinessId, dto.BranchId);
        return await MapDetailDtoAsync(result!, dto.BusinessId, dto.BranchId);
    }

    public async Task<List<SaleLedgerEntryDto>> GetPurchaseLedgerHistoryAsync(
        int purchaseId, int businessId, int branchId)
    {
        var entries = await _stockLedgerRepository.GetByReferenceAsync(
            purchaseId, businessId, branchId);

        return entries
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Id)
            .Select(e => new SaleLedgerEntryDto
            {
                Id                 = e.Id,
                Type               = e.Type.ToString(),
                ProductId          = e.ProductId,
                ProductName        = e.Product?.ProductName ?? string.Empty,
                VariantId          = e.VariantId,
                VariantName        = e.Variant?.VariantName,
                WarehouseId        = e.WarehouseId,
                WarehouseName      = e.Warehouse?.Name ?? string.Empty,
                QuantityInBaseUnit = e.QuantityInBaseUnit,
                UnitPrice          = e.UnitPrice,
                TotalAmount        = e.TotalAmount,
                Date               = e.Date,
                Remarks            = e.Remarks
            }).ToList();
    }

    private async Task<PurchaseDetailDto> MapDetailDtoAsync(Domain.Purchase p, int businessId, int branchId)
    {
        var paid = await _invoicePaymentService.GetTotalPaidForPurchaseAsync(p.Id, businessId, branchId);
        var payments = await _invoicePaymentService.GetPaymentsForPurchaseAsync(p.Id, businessId, branchId);
        return MapDetailDto(p, paid, payments);
    }

    private static PurchaseDto MapDto(Domain.Purchase p, decimal paidAmount = 0) => new()
    {
        Id = p.Id,
        InvoiceNo = p.InvoiceNo,
        SupplierId = p.SupplierId,
        SupplierName = p.Supplier?.Name ?? string.Empty,
        WarehouseId = p.WarehouseId,
        WarehouseName = p.Warehouse?.Name ?? string.Empty,
        BranchId = p.BranchId,
        BranchName = p.Branch?.Name ?? string.Empty,
        PurchaseDate = p.PurchaseDate,
        TotalAmount = p.TotalAmount,
        PaidAmount = paidAmount,
        BalanceDue = p.TotalAmount - paidAmount,
        Status = p.Status,
        IsCreditPurchase = p.IsCreditPurchase,
        Notes = p.Notes,
        ItemCount    = p.Items.Count(i => !i.IsDeleted),
        CreatedAt    = p.CreatedAt,
        ModifiedAt   = p.ModifiedAt,
        VoidedAt     = p.VoidedAt,
        VoidedByName = p.VoidedByName
    };

    private static PurchaseDetailDto MapDetailDto(
        Domain.Purchase p, decimal paidAmount, List<InvoicePaymentDto> payments) => new()
    {
        Id           = p.Id,
        InvoiceNo    = p.InvoiceNo,
        SupplierId   = p.SupplierId,
        SupplierName = p.Supplier?.Name ?? string.Empty,
        WarehouseId  = p.WarehouseId,
        WarehouseName = p.Warehouse?.Name ?? string.Empty,
        BranchId     = p.BranchId,
        BranchName   = p.Branch?.Name ?? string.Empty,
        PurchaseDate = p.PurchaseDate,
        TotalAmount  = p.TotalAmount,
        PaidAmount   = paidAmount,
        BalanceDue   = p.TotalAmount - paidAmount,
        Status       = p.Status,
        IsCreditPurchase = p.IsCreditPurchase,
        Notes        = p.Notes,
        ItemCount    = p.Items.Count(i => !i.IsDeleted),
        CreatedAt    = p.CreatedAt,
        ModifiedAt   = p.ModifiedAt,
        VoidedAt     = p.VoidedAt,
        VoidedByName = p.VoidedByName,
        Items = p.Items
            .Where(i => !i.IsDeleted)
            .Select(i => new PurchaseItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.ProductName ?? string.Empty,
                VariantId = i.VariantId,
                VariantName = i.Variant?.VariantName,
                UnitId = i.UnitId,
                UnitName = i.Unit?.UnitName ?? string.Empty,
                Quantity = i.Quantity,
                ConversionFactor = i.ConversionFactor,
                BaseQuantity = i.BaseQuantity,
                CostPrice = i.CostPrice,
                TotalCost = i.TotalCost
            }).ToList(),
        Payments = payments
    };
}
