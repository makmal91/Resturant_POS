using POSSystem.Application.Orders.DTOs;
using POSSystem.Application.Orders.Interfaces;
using POSSystem.Domain;
using System.Linq;

namespace POSSystem.Application.Orders.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
    {
        var menuItemIds = dto.OrderItems.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = await _repository.GetMenuItemsAsync(menuItemIds);
        var menuItemDict = menuItems.ToDictionary(m => m.Id, m => m);

        var order = new Order
        {
            OrderType = dto.OrderType,
            TableId = dto.TableId,
            CustomerId = dto.CustomerId,
            WaiterId = dto.WaiterId,
            Notes = dto.Notes,
            BranchId = dto.BranchId,
            Status = OrderStatus.Pending,
            OrderItems = new List<OrderItem>()
        };

        foreach (var itemDto in dto.OrderItems)
        {
            var menuItem = menuItemDict[itemDto.MenuItemId];
            var orderItem = new OrderItem
            {
                MenuItemId = itemDto.MenuItemId,
                Quantity = itemDto.Quantity,
                Price = menuItem.Price,
                Discount = itemDto.Discount,
                BranchId = dto.BranchId
            };
            orderItem.Total = (orderItem.Price * orderItem.Quantity) - orderItem.Discount;
            order.OrderItems.Add(orderItem);
        }

        await CalculateTotalsAsync(order);

        await _repository.AddOrderAsync(order);
        await _repository.SaveChangesAsync();

        return order;
    }

    public async Task<OrderItem> AddOrderItemAsync(int orderId, AddOrderItemDto dto)
    {
        var order = await _repository.GetOrderWithItemsAsync(orderId);
        if (order == null) throw new Exception("Order not found");

        var menuItem = await _repository.GetMenuItemAsync(dto.MenuItemId);
        if (menuItem == null) throw new Exception("Menu item not found");

        var orderItem = new OrderItem
        {
            OrderId = orderId,
            MenuItemId = dto.MenuItemId,
            Quantity = dto.Quantity,
            Price = menuItem.Price,
            Discount = dto.Discount,
            BranchId = order.BranchId
        };
        orderItem.Total = (orderItem.Price * orderItem.Quantity) - orderItem.Discount;
        order.OrderItems.Add(orderItem);

        await CalculateTotalsAsync(order);

        await _repository.AddOrderItemAsync(orderItem);
        await _repository.SaveChangesAsync();

        return orderItem;
    }

    public async Task<Order> CalculateTotalsAsync(int orderId)
    {
        var order = await _repository.GetOrderWithItemsAsync(orderId);
        if (order == null) throw new Exception("Order not found");

        return await CalculateTotalsAsync(order);
    }

    private async Task<Order> CalculateTotalsAsync(Order order)
    {
        order.Subtotal = order.OrderItems.Sum(oi => oi.Total);
        order.Tax = order.OrderItems.Sum(oi => (oi.Price * oi.Quantity * oi.MenuItem.TaxPercentage / 100));
        order.TotalAmount = order.Subtotal + order.Tax - order.Discount;

        return order;
    }
}