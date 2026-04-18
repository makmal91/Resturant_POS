using POSSystem.Application.Inventory.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace POSSystem.Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly ApplicationDbContext _context;

    public InventoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryItem?> GetInventoryItemAsync(int id)
    {
        return await _context.InventoryItems.FindAsync(id);
    }

    public async Task<InventoryItem?> GetInventoryItemByNameAndBranchAsync(string name, int branchId)
    {
        return await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.Name == name && i.BranchId == branchId);
    }

    public async Task<ICollection<InventoryItem>> GetInventoryItemsAsync(IEnumerable<int> ids)
    {
        return await _context.InventoryItems
            .Where(i => ids.Contains(i.Id))
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