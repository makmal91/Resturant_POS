using Microsoft.AspNetCore.SignalR;

namespace POSSystem.API.Hubs;

public class NotificationHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task SendOrderUpdate(string orderId, string status)
    {
        await Clients.All.SendAsync("OrderUpdate", orderId, status);
    }
}