using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Orders.DTOs;
using POSSystem.Application.Orders.Interfaces;
using POSSystem.Domain;

namespace POSSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateEmptyOrderDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
            dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
            var order = await _orderService.CreateEmptyOrderAsync(dto);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("add-item")]
    public async Task<IActionResult> AddItem([FromBody] AddItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var businessId = this.ResolveBusinessId();
            var branchId = this.ResolveBranchId();
            var orderItem = await _orderService.AddOrderItemAsync(request.OrderId, businessId, branchId, request.Item);
            return Ok(orderItem);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompleteOrder([FromBody] CompleteOrderRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var businessId = this.ResolveBusinessId();
            var branchId = this.ResolveBranchId();
            var order = await _orderService.CompleteOrderAsync(request.OrderId, businessId, branchId);
            return Ok(order);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id, [FromQuery] int? businessId = null, [FromQuery] int? branchId = null)
    {
        try
        {
            var resolvedBusinessId = this.ResolveBusinessId(businessId);
            var resolvedBranchId = this.ResolveBranchId(branchId);
            var order = await _orderService.GetOrderAsync(id, resolvedBusinessId, resolvedBranchId);
            if (order == null)
                return NotFound();

            return Ok(order);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public class AddItemRequest
{
    public int OrderId { get; set; }
    public AddOrderItemDto Item { get; set; } = null!;
}

public class CompleteOrderRequest
{
    public int OrderId { get; set; }
}