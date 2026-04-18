using POSSystem.Application.Inventory.DTOs;
using POSSystem.Application.Inventory.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Inventory.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _repository;

    public InventoryService(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task AddStockAsync(AddStockDto dto)
    {
        var item = await _repository.GetInventoryItemAsync(dto.ItemId);
        if (item == null) throw new Exception("Inventory item not found");

        item.CurrentStock += dto.Quantity;

        var movement = new StockMovement
        {
            ItemId = dto.ItemId,
            Quantity = dto.Quantity,
            Type = StockMovementType.In,
            BranchToId = dto.BranchId
        };

        await _repository.AddStockMovementAsync(movement);
        await _repository.SaveChangesAsync();
    }

    public async Task TransferStockAsync(TransferStockDto dto)
    {
        var fromItem = await _repository.GetInventoryItemAsync(dto.ItemId);
        if (fromItem == null) throw new Exception("Inventory item not found");
        if (fromItem.BranchId != dto.FromBranchId) throw new Exception("Item not in from branch");

        if (fromItem.CurrentStock < dto.Quantity) throw new Exception("Insufficient stock for transfer");

        fromItem.CurrentStock -= dto.Quantity;

        var toItem = await _repository.GetInventoryItemByNameAndBranchAsync(fromItem.Name, dto.ToBranchId);
        if (toItem == null) throw new Exception("Item not found in to branch");

        toItem.CurrentStock += dto.Quantity;

        var movementOut = new StockMovement
        {
            ItemId = fromItem.Id,
            Quantity = -dto.Quantity,
            Type = StockMovementType.Transfer,
            BranchFromId = dto.FromBranchId,
            BranchToId = dto.ToBranchId
        };

        var movementIn = new StockMovement
        {
            ItemId = toItem.Id,
            Quantity = dto.Quantity,
            Type = StockMovementType.Transfer,
            BranchFromId = dto.FromBranchId,
            BranchToId = dto.ToBranchId
        };

        await _repository.AddStockMovementAsync(movementOut);
        await _repository.AddStockMovementAsync(movementIn);
        await _repository.SaveChangesAsync();
    }

    public async Task DeductStockAsync(int itemId, decimal quantity, int branchId)
    {
        var item = await _repository.GetInventoryItemAsync(itemId);
        if (item == null) throw new Exception("Inventory item not found");
        if (item.BranchId != branchId) throw new Exception("Item not in branch");

        if (item.CurrentStock < quantity) throw new Exception("Insufficient stock");

        item.CurrentStock -= quantity;

        var movement = new StockMovement
        {
            ItemId = itemId,
            Quantity = -quantity,
            Type = StockMovementType.Out,
            BranchFromId = branchId
        };

        await _repository.AddStockMovementAsync(movement);
        await _repository.SaveChangesAsync();
    }
}