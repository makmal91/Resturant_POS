using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly POSDbContext _db;

    public ReportsController(POSDbContext db)
    {
        _db = db;
    }

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport([FromQuery] int branchId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var fromDate = from ?? DateTime.UtcNow.Date;
        var toDate = to ?? DateTime.UtcNow;

        var orderItems = await _db.OrderItems
            .Where(oi => oi.BranchId == branchId)
            .Include(oi => oi.Order)
            .Include(oi => oi.MenuItem)
            .Where(oi => oi.Order.Status == OrderStatus.Completed &&
                         oi.Order.CreatedDate >= fromDate &&
                         oi.Order.CreatedDate <= toDate &&
                         oi.MenuItem.ProductType == ProductType.FinishedGood)
            .ToListAsync();

        var menuItemIds = orderItems.Select(i => i.MenuItemId).Distinct().ToList();
        var recipes = await _db.Recipes
            .Where(r => menuItemIds.Contains(r.MenuItemId))
            .Include(r => r.Ingredient)
            .ToListAsync();

        decimal revenue = 0;
        decimal recipeCost = 0;

        foreach (var item in orderItems)
        {
            revenue += item.Total;

            var itemRecipes = recipes.Where(r => r.MenuItemId == item.MenuItemId);
            foreach (var recipe in itemRecipes)
            {
                recipeCost += recipe.QuantityRequired * recipe.Ingredient.PurchasePrice * item.Quantity;
            }
        }

        return Ok(new
        {
            from = fromDate,
            to = toDate,
            totalItems = orderItems.Sum(i => i.Quantity),
            revenue,
            recipeCost,
            grossProfit = revenue - recipeCost
        });
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryReport([FromQuery] int branchId)
    {
        var items = await _db.InventoryItems
            .Where(i => i.BranchId == branchId &&
                        (i.ProductType == ProductType.RawMaterial || i.ProductType == ProductType.SemiFinished))
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.ProductType,
                i.Unit,
                i.CurrentStock,
                i.MinStockLevel,
                i.PurchasePrice,
                stockValue = i.CurrentStock * i.PurchasePrice
            })
            .ToListAsync();

        return Ok(new
        {
            items,
            totalStockValue = items.Sum(i => i.stockValue)
        });
    }
}
