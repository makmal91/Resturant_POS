using POSSystem.Application.Common.Constants;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Application.Purchase.DTOs;
using POSSystem.Application.Purchase.Interfaces;
using POSSystem.Application.Sales.DTOs;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Purchase.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IStockLedgerRepository _stockLedgerRepository;
    private readonly ICodeGeneratorService _codeGenerator;

    public PurchaseService(
        IPurchaseRepository purchaseRepository,
        IStockLedgerRepository stockLedgerRepository,
        ICodeGeneratorService codeGenerator)
    {
        _purchaseRepository = purchaseRepository;
        _stockLedgerRepository = stockLedgerRepository;
        _codeGenerator = codeGenerator;
    }

    public async Task<PagedResultDto<PurchaseDto>> GetPurchasesPagedAsync(
        int businessId, int branchId, int page, int pageSize, string? search = null, PurchaseStatus? status = null)
    {
        var result = await _purchaseRepository.GetPagedAsync(businessId, branchId, page, pageSize, search, status);
        return new PagedResultDto<PurchaseDto>
        {
            Data = result.Data.Select(MapDto).ToList(),
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage
        };
    }

    public async Task<PurchaseDetailDto?> GetPurchaseByIdAsync(int id, int businessId, int branchId)
    {
        var entity = await _purchaseRepository.GetByIdWithItemsAsync(id, businessId, branchId);
        return entity == null ? null : MapDetailDto(entity);
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
            Status = PurchaseStatus.Draft,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId
        };

        BuildItems(purchase, dto.Items, dto.BusinessId, dto.BranchId);

        await _purchaseRepository.AddAsync(purchase);
        await _purchaseRepository.SaveChangesAsync();

        var created = await _purchaseRepository.GetByIdWithItemsAsync(purchase.Id, dto.BusinessId, dto.BranchId);
        return MapDetailDto(created!);
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
        if (entity.Status == PurchaseStatus.Posted)
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
                QuantityInBaseUnit = -e.QuantityInBaseUnit,   // flip: was +, now -
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
        foreach (var item in entity.Items)
            item.IsDeleted = true;

        BuildItems(entity, dto.Items, dto.BusinessId, dto.BranchId);

        // If was Posted, re-apply new stock entries and keep Posted status
        if (entity.Status == PurchaseStatus.Posted)
        {
            var activeItems = entity.Items.Where(i => !i.IsDeleted).ToList();
            foreach (var item in activeItems)
            {
                await _stockLedgerRepository.AddAsync(new StockLedger
                {
                    ProductId          = item.ProductId,
                    VariantId          = item.VariantId,
                    WarehouseId        = entity.WarehouseId,
                    Type               = StockLedgerType.PurchaseEntry,
                    ReferenceId        = entity.Id,
                    QuantityInBaseUnit = item.BaseQuantity,
                    UnitPrice          = item.CostPrice,
                    TotalAmount        = item.TotalCost,
                    Date               = entity.PurchaseDate,
                    Remarks            = $"Purchase (Corrected) — Invoice: {entity.InvoiceNo}",
                    BusinessId         = dto.BusinessId,
                    BranchId           = dto.BranchId
                });
            }
        }

        await _purchaseRepository.SaveChangesAsync();
        await _stockLedgerRepository.SaveChangesAsync();

        var updated = await _purchaseRepository.GetByIdWithItemsAsync(id, dto.BusinessId, dto.BranchId);
        return MapDetailDto(updated!);
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

        foreach (var item in activeItems)
        {
            var baseQty = item.Quantity * item.ConversionFactor;
            item.BaseQuantity = baseQty;

            var ledgerEntry = new StockLedger
            {
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                WarehouseId = entity.WarehouseId,
                Type = StockLedgerType.PurchaseEntry,
                ReferenceId = entity.Id,
                QuantityInBaseUnit = baseQty,
                UnitPrice = item.CostPrice,
                TotalAmount = item.TotalCost,
                Date = entity.PurchaseDate,
                Remarks = $"Purchase Entry — Invoice: {entity.InvoiceNo}",
                BusinessId = dto.BusinessId,
                BranchId = dto.BranchId
            };

            await _stockLedgerRepository.AddAsync(ledgerEntry);
        }

        entity.TotalAmount = activeItems.Sum(i => i.TotalCost);
        entity.Status = PurchaseStatus.Posted;

        await _purchaseRepository.SaveChangesAsync();

        var posted = await _purchaseRepository.GetByIdWithItemsAsync(id, dto.BusinessId, dto.BranchId);
        return MapDetailDto(posted!);
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
            if (item.ConversionFactor <= 0) throw new InvalidOperationException("ConversionFactor must be greater than zero.");
            if (item.CostPrice < 0) throw new InvalidOperationException("CostPrice cannot be negative.");
        }
    }

    private static void BuildItems(Domain.Purchase purchase, List<CreatePurchaseItemDto> dtoItems, int businessId, int branchId)
    {
        decimal total = 0;
        foreach (var i in dtoItems)
        {
            var baseQty = i.Quantity * i.ConversionFactor;
            var totalCost = i.Quantity * i.CostPrice;
            total += totalCost;

            purchase.Items.Add(new PurchaseItem
            {
                ProductId = i.ProductId,
                VariantId = i.VariantId,
                UnitId = i.UnitId,
                Quantity = i.Quantity,
                ConversionFactor = i.ConversionFactor,
                BaseQuantity = baseQty,
                CostPrice = i.CostPrice,
                TotalCost = totalCost,
                BusinessId = businessId,
                BranchId = branchId
            });
        }
        purchase.TotalAmount = total;
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
            QuantityInBaseUnit = -e.QuantityInBaseUnit,   // was positive, now negative → stock out
            UnitPrice          = e.UnitPrice,
            TotalAmount        = e.TotalAmount,
            Date               = DateTime.UtcNow,
            Remarks            = $"Void of Purchase — Invoice: {entity.InvoiceNo}" +
                                 (string.IsNullOrWhiteSpace(dto.Reason) ? "" : $" | Reason: {dto.Reason}"),
            BusinessId         = dto.BusinessId,
            BranchId           = dto.BranchId
        }).ToList();

        await _stockLedgerRepository.AddRangeAsync(reversals);

        entity.Status       = PurchaseStatus.Cancelled;
        entity.VoidedAt     = DateTime.UtcNow;
        entity.VoidedByName = dto.VoidedByName;

        await _purchaseRepository.SaveChangesAsync();
        await _stockLedgerRepository.SaveChangesAsync();

        var result = await _purchaseRepository.GetByIdWithItemsAsync(id, dto.BusinessId, dto.BranchId);
        return MapDetailDto(result!);
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

    private static PurchaseDto MapDto(Domain.Purchase p) => new()
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
        Status = p.Status,
        Notes = p.Notes,
        ItemCount    = p.Items.Count(i => !i.IsDeleted),
        CreatedAt    = p.CreatedAt,
        ModifiedAt   = p.ModifiedAt,
        VoidedAt     = p.VoidedAt,
        VoidedByName = p.VoidedByName
    };

    private static PurchaseDetailDto MapDetailDto(Domain.Purchase p) => new()
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
        Status       = p.Status,
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
            }).ToList()
    };
}
