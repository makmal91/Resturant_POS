using POSSystem.Application.Orders.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Orders.Interfaces;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(CreateOrderDto dto);
    Task<Order> CreateEmptyOrderAsync(CreateEmptyOrderDto dto);
    Task<OrderItem> AddOrderItemAsync(int orderId, AddOrderItemDto dto);
    Task<Order> CompleteOrderAsync(int orderId);
    Task<Order> CalculateTotalsAsync(int orderId);
    Task<Order?> GetOrderAsync(int id);
}

public interface IOrderRepository
{
    Task<Order?> GetOrderWithItemsAsync(int id);
    Task<MenuItem?> GetMenuItemAsync(int id);
    Task<MenuItemVariant?> GetVariantAsync(int id);
    Task<ICollection<MenuItem>> GetMenuItemsAsync(IEnumerable<int> ids);
    Task<ICollection<POSSystem.Domain.Recipe>> GetRecipesByMenuItemIdsAsync(IEnumerable<int> menuItemIds);
    Task<ICollection<InventoryItem>> GetInventoryItemsAsync(IEnumerable<int> ids);
    Task AddOrderAsync(Order order);
    Task AddOrderItemAsync(OrderItem orderItem);
    Task AddStockMovementAsync(StockMovement movement);
    Task SaveChangesAsync();
}