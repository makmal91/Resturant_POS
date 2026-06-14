using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Orders.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly POSDbContext _context;

    public OrderRepository(POSDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetOrderWithItemsAsync(int id, int businessId, int branchId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .FirstOrDefaultAsync(o => o.Id == id && o.BusinessId == businessId && o.BranchId == branchId);
    }

    public async Task<MenuItem?> GetMenuItemAsync(int id, int businessId, int branchId)
    {
        return await _context.MenuItems
            .FirstOrDefaultAsync(m => m.Id == id && m.BusinessId == businessId && m.BranchId == branchId);
    }

    public async Task<MenuItemVariant?> GetVariantAsync(int id, int businessId, int branchId)
    {
        return await _context.MenuItemVariants
            .FirstOrDefaultAsync(v => v.Id == id && v.BusinessId == businessId && v.BranchId == branchId);
    }

    public async Task<ICollection<MenuItem>> GetMenuItemsAsync(IEnumerable<int> ids, int businessId, int branchId)
    {
        return await _context.MenuItems
            .Where(m => ids.Contains(m.Id) && m.BusinessId == businessId && m.BranchId == branchId)
            .ToListAsync();
    }

    public async Task AddOrderAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task AddOrderItemAsync(OrderItem orderItem)
    {
        await _context.OrderItems.AddAsync(orderItem);
    }

        public async Task<ICollection<Recipe>> GetRecipesByMenuItemIdsAsync(IEnumerable<int> menuItemIds, int businessId, int branchId)
    {
        return await _context.Recipes
            .Where(r => menuItemIds.Contains(r.MenuItemId) && r.BusinessId == businessId && r.BranchId == branchId)
            .Include(r => r.Ingredient)
            .ToListAsync();
    }

        public async Task<ICollection<InventoryItem>> GetInventoryItemsAsync(IEnumerable<int> ids, int businessId, int branchId)
    {
        return await _context.InventoryItems
            .Where(i => ids.Contains(i.Id) && i.BusinessId == businessId && i.BranchId == branchId)
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