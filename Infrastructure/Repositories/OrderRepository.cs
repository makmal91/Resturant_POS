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

    public async Task<Order?> GetOrderWithItemsAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<MenuItem?> GetMenuItemAsync(int id)
    {
        return await _context.MenuItems.FindAsync(id);
    }

    public async Task<ICollection<MenuItem>> GetMenuItemsAsync(IEnumerable<int> ids)
    {
        return await _context.MenuItems
            .Where(m => ids.Contains(m.Id))
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

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}