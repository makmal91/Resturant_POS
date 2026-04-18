using Microsoft.AspNetCore.SignalR;
using POSSystem.Domain;

namespace POSSystem.API.Hubs;

public class OrderHub : Hub
{
    public async Task NotifyNewOrder(NewOrderNotification notification)
    {
        await Clients.All.SendAsync("NewOrderReceived", notification);
    }

    public async Task UpdateOrderStatus(UpdateOrderStatusRequest request)
    {
        await Clients.All.SendAsync("OrderStatusUpdated", new
        {
            request.OrderId,
            Status = request.Status.ToString(),
            request.Notes,
            request.UpdatedAt
        });

        if (request.TableId.HasValue && request.TableStatus.HasValue)
        {
            await Clients.All.SendAsync("TableStatusUpdated", new
            {
                TableId = request.TableId.Value,
                Status = request.TableStatus.Value.ToString(),
                request.UpdatedAt
            });
        }
    }

    public sealed record NewOrderNotification(
        int OrderId,
        int? TableId,
        OrderStatus Status,
        string? Notes,
        decimal? TotalAmount,
        DateTime CreatedAt);

    public sealed record UpdateOrderStatusRequest(
        int OrderId,
        OrderStatus Status,
        int? TableId,
        TableStatus? TableStatus,
        string? Notes,
        DateTime UpdatedAt);
}
