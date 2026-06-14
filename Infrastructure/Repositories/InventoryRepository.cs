using POSSystem.Application.Inventory.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace POSSystem.Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly POSDbContext _context;

    public InventoryRepository(POSDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryItem?> GetInventoryItemAsync(int id, int businessId, int branchId)
    {
        return await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id && i.BusinessId == businessId && i.BranchId == branchId);
    }

    public async Task<InventoryItem?> GetInventoryItemByNameAndBranchAsync(string name, int businessId, int branchId)
    {
        return await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.Name == name && i.BusinessId == businessId && i.BranchId == branchId);
    }

    public async Task<ICollection<InventoryItem>> GetInventoryItemsAsync(IEnumerable<int> ids, int businessId, int branchId)
    {
        return await _context.InventoryItems
            .Where(i => ids.Contains(i.Id) && i.BusinessId == businessId && i.BranchId == branchId)
            .ToListAsync();
    }

    public async Task<ICollection<InventoryItem>> GetInventoryItemsByBranchAsync(int businessId, int branchId)
    {
        return await _context.InventoryItems
            .Where(i => i.BusinessId == businessId &&
                        i.BranchId == branchId &&
                        i.IsInventoryItem &&
                        (i.ProductType == ProductType.RawMaterial || i.ProductType == ProductType.SemiFinished))
            .ToListAsync();
    }

    public async Task AddStockMovementAsync(StockMovement movement)
    {
        await _context.StockMovements.AddAsync(movement);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}