using POSSystem.Application.Inventory.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Inventory.Interfaces;

public interface IInventoryService
{
    Task AddStockAsync(AddStockDto dto);
    Task AdjustStockAsync(AdjustStockDto dto);
    Task TransferStockAsync(TransferStockDto dto);
    Task DeductStockAsync(int itemId, decimal quantity, int businessId, int branchId);
    Task<ICollection<InventoryItem>> GetInventoryItemsAsync(int businessId, int branchId);
}

public interface IInventoryRepository
{
    Task<InventoryItem?> GetInventoryItemAsync(int id, int businessId, int branchId);
    Task<InventoryItem?> GetInventoryItemByNameAndBranchAsync(string name, int businessId, int branchId);
    Task<ICollection<InventoryItem>> GetInventoryItemsAsync(IEnumerable<int> ids, int businessId, int branchId);
    Task<ICollection<InventoryItem>> GetInventoryItemsByBranchAsync(int businessId, int branchId);
    Task AddStockMovementAsync(StockMovement movement);
    Task SaveChangesAsync();
}