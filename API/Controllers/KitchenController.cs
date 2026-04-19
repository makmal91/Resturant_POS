using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KitchenController : ControllerBase
{
    private readonly POSDbContext _db;

    public KitchenController(POSDbContext db)
    {
        _db = db;
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetKitchenOrders([FromQuery] int branchId)
    {
        var activeStatuses = new[] { OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.InProgress };

        var orders = await _db.Orders
            .Where(o => o.BranchId == branchId && activeStatuses.Contains(o.Status))
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                    .ThenInclude(mi => mi.MenuCategory)
            .ToListAsync();

        var kitchenItems = orders
            .SelectMany(order => order.OrderItems.Select(item => new
            {
                order.Id,
                order.Status,
                MenuItemId = item.MenuItemId,
                ItemName = item.MenuItem.Name,
                item.Quantity,
                item.Notes,
                KitchenStation = item.MenuItem.MenuCategory.Name,
                item.MenuItem.ProductType,
                item.MenuItem.IsSaleable
            }))
            .Where(item => item.IsSaleable && item.ProductType == ProductType.FinishedGood)
            .GroupBy(item => item.KitchenStation)
            .Select(group => new
            {
                kitchenStation = group.Key,
                items = group.Select(x => new
                {
                    orderId = x.Id,
                    status = x.Status,
                    menuItemId = x.MenuItemId,
                    name = x.ItemName,
                    quantity = x.Quantity,
                    notes = x.Notes
                }).ToList()
            })
            .ToList();

        return Ok(new { stations = kitchenItems });
    }
}
