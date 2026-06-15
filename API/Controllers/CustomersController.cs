using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Customer.DTOs;
using POSSystem.Application.Customer.Interfaces;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
        => _customerService = customerService;

    /// <summary>Get paged customer list with search and filter.</summary>
    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] int? type = null,
        [FromQuery] bool? isActive = null)
    {
        var filter = new CustomerFilterDto
        {
            BusinessId = this.ResolveBusinessId(businessId),
            BranchId   = this.ResolveBranchId(branchId),
            Search     = search,
            Type       = type.HasValue ? (Domain.CustomerType?)type.Value : null,
            IsActive   = isActive,
            Page       = page,
            PageSize   = pageSize
        };

        var result = await _customerService.GetCustomersPagedAsync(filter);
        return Ok(new
        {
            customers    = result.Data,
            totalRecords = result.TotalRecords,
            totalPages   = result.TotalPages,
            currentPage  = result.CurrentPage,
            pageSize
        });
    }

    /// <summary>Get the system walk-in customer for the current branch.</summary>
    [HttpGet("walk-in")]
    public async Task<IActionResult> GetWalkIn([FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var customer = await _customerService.GetWalkInCustomerAsync(
            this.ResolveBusinessId(businessId), this.ResolveBranchId(branchId));

        return customer == null ? NotFound(new { message = "Walk-in customer not found." }) : Ok(customer);
    }

    /// <summary>Fast search by name or phone — used by POS autocomplete.</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(new List<CustomerListDto>());

        var results = await _customerService.SearchCustomersAsync(
            q, this.ResolveBusinessId(businessId), this.ResolveBranchId(branchId));
        return Ok(results);
    }

    /// <summary>Get customer by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var customer = await _customerService.GetByIdAsync(
            id, this.ResolveBusinessId(businessId), this.ResolveBranchId(branchId));
        return customer == null ? NotFound() : Ok(customer);
    }

    /// <summary>Create a new customer.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId   = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        try
        {
            var created = await _customerService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Quick-create a customer from POS screen (minimal fields).</summary>
    [HttpPost("quick-create")]
    public async Task<IActionResult> QuickCreate([FromBody] QuickCreateCustomerDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId   = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        try
        {
            var created = await _customerService.QuickCreateAsync(dto);
            return Ok(created);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Update a customer.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId   = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);

        try
        {
            var updated = await _customerService.UpdateAsync(id, dto);
            return updated == null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Soft-delete a customer (walk-in customer is protected).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        try
        {
            await _customerService.DeleteAsync(
                id, this.ResolveBusinessId(businessId), this.ResolveBranchId(branchId));
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
