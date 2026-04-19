using POSSystem.Application.Orders.DTOs;
using POSSystem.Application.Orders.Interfaces;
using POSSystem.Application.Inventory.Interfaces;
using POSSystem.Domain;
using System.Linq;
using System.Text.Json;

namespace POSSystem.Application.Orders.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly IInventoryService _inventoryService;

    public OrderService(IOrderRepository repository, IInventoryService inventoryService)
    {
        _repository = repository;
        _inventoryService = inventoryService;
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
            if (!menuItemDict.TryGetValue(itemDto.MenuItemId, out var menuItem))
                throw new Exception($"Menu item {itemDto.MenuItemId} not found");

            if (!menuItem.IsSaleable || menuItem.ProductType != ProductType.FinishedGood)
                throw new Exception($"Menu item {menuItem.Name} is not allowed for POS order");

            var price = menuItem.Price;
            if (itemDto.VariantId.HasValue)
            {
                var variant = await _repository.GetVariantAsync(itemDto.VariantId.Value);
                if (variant == null || variant.MenuItemId != menuItem.Id)
                    throw new Exception("Invalid variant for selected menu item");

                price = variant.Price;
            }

            var orderItem = new OrderItem
            {
                MenuItemId = itemDto.MenuItemId,
                VariantId = itemDto.VariantId,
                ModifiersJson = JsonSerializer.Serialize(itemDto.ModifierIds),
                Notes = itemDto.Notes,
                Quantity = itemDto.Quantity,
                Price = price,
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

    public async Task<Order> CreateEmptyOrderAsync(CreateEmptyOrderDto dto)
    {
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

        if (!menuItem.IsSaleable || menuItem.ProductType != ProductType.FinishedGood)
            throw new Exception("Only saleable FinishedGood items can be added to order");

        var price = menuItem.Price;
        if (dto.VariantId.HasValue)
        {
            var variant = await _repository.GetVariantAsync(dto.VariantId.Value);
            if (variant == null || variant.MenuItemId != menuItem.Id)
                throw new Exception("Invalid variant for selected menu item");

            price = variant.Price;
        }

        var orderItem = new OrderItem
        {
            OrderId = orderId,
            MenuItemId = dto.MenuItemId,
            VariantId = dto.VariantId,
            ModifiersJson = JsonSerializer.Serialize(dto.ModifierIds),
            Notes = dto.Notes,
            Quantity = dto.Quantity,
            Price = price,
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

    public async Task<Order> CompleteOrderAsync(int orderId)
    {
        var order = await _repository.GetOrderWithItemsAsync(orderId);
        if (order == null) throw new Exception("Order not found");

        if (order.Status == OrderStatus.Completed) throw new Exception("Order already completed");

        // Calculate totals
        await CalculateTotalsAsync(order);

        // Deduct inventory
        var menuItemIds = order.OrderItems.Select(oi => oi.MenuItemId).Distinct().ToList();
        var recipes = await _repository.GetRecipesByMenuItemIdsAsync(menuItemIds);

        foreach (var orderItem in order.OrderItems)
        {
            if (!orderItem.MenuItem.IsSaleable || orderItem.MenuItem.ProductType != ProductType.FinishedGood)
                throw new Exception($"Invalid order item type for {orderItem.MenuItem.Name}");

            var itemRecipes = recipes.Where(r => r.MenuItemId == orderItem.MenuItemId);
            foreach (var recipe in itemRecipes)
            {
                var deductQuantity = recipe.QuantityRequired * orderItem.Quantity;
                await _inventoryService.DeductStockAsync(recipe.IngredientId, deductQuantity, order.BranchId);
            }
        }

        order.Status = OrderStatus.Completed;
        await _repository.SaveChangesAsync();

        return order;
    }

    public async Task<Order?> GetOrderAsync(int id)
    {
        return await _repository.GetOrderWithItemsAsync(id);
    }

    private async Task<Order> CalculateTotalsAsync(Order order)
    {
        order.Subtotal = order.OrderItems.Sum(oi => oi.Total);
        order.Tax = order.OrderItems.Sum(oi => (oi.Price * oi.Quantity * oi.MenuItem.TaxPercentage / 100));
        order.TotalAmount = order.Subtotal + order.Tax - order.Discount;

        return order;
    }
}