using POSSystem.Application.Orders.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Orders.Interfaces;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(CreateOrderDto dto);
    Task<OrderItem> AddOrderItemAsync(int orderId, AddOrderItemDto dto);
    Task<Order> CalculateTotalsAsync(int orderId);
}

public interface IOrderRepository
{
    Task<Order?> GetOrderWithItemsAsync(int id);
    Task<MenuItem?> GetMenuItemAsync(int id);
    Task<ICollection<MenuItem>> GetMenuItemsAsync(IEnumerable<int> ids);
    Task AddOrderAsync(Order order);
    Task AddOrderItemAsync(OrderItem orderItem);
    Task SaveChangesAsync();
}