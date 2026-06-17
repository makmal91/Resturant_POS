using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Stock.DTOs;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Stock.Services;

public class StockService : IStockService
{
    private readonly IStockLedgerRepository _ledgerRepository;

    public StockService(IStockLedgerRepository ledgerRepository)
    {
        _ledgerRepository = ledgerRepository;
    }

    public async Task<PagedResultDto<StockLedgerDto>> GetLedgerAsync(StockLedgerFilterDto filter)
    {
        var result = await _ledgerRepository.GetPagedAsync(filter);
        return new PagedResultDto<StockLedgerDto>
        {
            Data = result.Data.Select(MapLedgerDto).ToList(),
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage
        };
    }

    public Task<List<StockBalanceDto>> GetStockBalancesAsync(
        int businessId, int branchId, int? warehouseId = null, int? productId = null, int? variantId = null, bool variantWise = false)
    {
        return _ledgerRepository.GetStockBalancesAsync(businessId, branchId, warehouseId, productId, variantId, variantWise);
    }

    public Task<decimal> GetCurrentStockAsync(int businessId, int branchId, int productId, int? variantId, int warehouseId)
    {
        return _ledgerRepository.GetCurrentStockAsync(businessId, branchId, productId, variantId, warehouseId);
    }

    public async Task TransferStockAsync(StockTransferDto dto)
    {
        if (dto.FromWarehouseId == dto.ToWarehouseId)
            throw new InvalidOperationException("Source and destination warehouses must be different.");

        if (dto.Quantity <= 0)
            throw new InvalidOperationException("Transfer quantity must be greater than zero.");

        var currentStock = await _ledgerRepository.GetCurrentStockAsync(
            dto.BusinessId, dto.BranchId, dto.ProductId, dto.VariantId, dto.FromWarehouseId);

        if (currentStock < dto.Quantity)
            throw new InvalidOperationException(
                $"Insufficient stock. Available: {currentStock}, Requested: {dto.Quantity}.");

        var now = DateTime.UtcNow;
        var remarks = string.IsNullOrWhiteSpace(dto.Remarks)
            ? $"Stock Transfer: Warehouse #{dto.FromWarehouseId} → Warehouse #{dto.ToWarehouseId}"
            : dto.Remarks;

        var outEntry = new StockLedger
        {
            ProductId = dto.ProductId,
            VariantId = dto.VariantId,
            WarehouseId = dto.FromWarehouseId,
            Type = StockLedgerType.TransferOut,
            QuantityInBaseUnit = -dto.Quantity,
            UnitPrice = 0,
            TotalAmount = 0,
            Date = now,
            Remarks = remarks,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId
        };

        var inEntry = new StockLedger
        {
            ProductId = dto.ProductId,
            VariantId = dto.VariantId,
            WarehouseId = dto.ToWarehouseId,
            Type = StockLedgerType.TransferIn,
            QuantityInBaseUnit = dto.Quantity,
            UnitPrice = 0,
            TotalAmount = 0,
            Date = now,
            Remarks = remarks,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId
        };

        await _ledgerRepository.AddRangeAsync(new[] { outEntry, inEntry });
        await _ledgerRepository.SaveChangesAsync();
    }

    private static StockLedgerDto MapLedgerDto(StockLedger e) => new()
    {
        Id = e.Id,
        ProductId = e.ProductId,
        ProductName = e.Product?.ProductName ?? string.Empty,
        VariantId = e.VariantId,
        VariantName = e.Variant?.VariantName,
        WarehouseId = e.WarehouseId,
        WarehouseName = e.Warehouse?.Name ?? string.Empty,
        Type = e.Type,
        ReferenceId = e.ReferenceId,
        QuantityInBaseUnit = e.QuantityInBaseUnit,
        UnitPrice = e.UnitPrice,
        TotalAmount = e.TotalAmount,
        Date = e.Date,
        Remarks = e.Remarks,
        BranchId = e.BranchId,
        BranchName = e.Branch?.Name ?? string.Empty,
        CreatedAt = e.CreatedAt
    };
}
