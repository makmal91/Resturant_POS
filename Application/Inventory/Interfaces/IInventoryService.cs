using POSSystem.Application.Inventory.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Inventory.Interfaces;

public interface IInventoryService
{
    Task AddStockAsync(AddStockDto dto);
    Task AdjustStockAsync(AdjustStockDto dto);
    Task TransferStockAsync(TransferStockDto dto);
    Task DeductStockAsync(int itemId, decimal quantity, int branchId);
    Task<ICollection<InventoryItem>> GetInventoryItemsAsync(int branchId);
}

public interface IInventoryRepository
{
    Task<InventoryItem?> GetInventoryItemAsync(int id);
    Task<InventoryItem?> GetInventoryItemByNameAndBranchAsync(string name, int branchId);
    Task<ICollection<InventoryItem>> GetInventoryItemsAsync(IEnumerable<int> ids);
    Task<ICollection<InventoryItem>> GetInventoryItemsByBranchAsync(int branchId);
    Task AddStockMovementAsync(StockMovement movement);
    Task SaveChangesAsync();
}