using POSSystem.Application.Orders.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Orders.Interfaces;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(CreateOrderDto dto);
    Task<Order> CreateEmptyOrderAsync(CreateEmptyOrderDto dto);
    Task<OrderItem> AddOrderItemAsync(int orderId, int businessId, int branchId, AddOrderItemDto dto);
    Task<Order> CompleteOrderAsync(int orderId, int businessId, int branchId);
    Task<Order> CalculateTotalsAsync(int orderId, int businessId, int branchId);
    Task<Order?> GetOrderAsync(int id, int businessId, int branchId);
}

public interface IOrderRepository
{
    Task<Order?> GetOrderWithItemsAsync(int id, int businessId, int branchId);
    Task<MenuItem?> GetMenuItemAsync(int id, int businessId, int branchId);
    Task<MenuItemVariant?> GetVariantAsync(int id, int businessId, int branchId);
    Task<ICollection<MenuItem>> GetMenuItemsAsync(IEnumerable<int> ids, int businessId, int branchId);
    Task<ICollection<POSSystem.Domain.Recipe>> GetRecipesByMenuItemIdsAsync(IEnumerable<int> menuItemIds, int businessId, int branchId);
    Task<ICollection<InventoryItem>> GetInventoryItemsAsync(IEnumerable<int> ids, int businessId, int branchId);
    Task AddOrderAsync(Order order);
    Task AddOrderItemAsync(OrderItem orderItem);
    Task AddStockMovementAsync(StockMovement movement);
    Task SaveChangesAsync();
}