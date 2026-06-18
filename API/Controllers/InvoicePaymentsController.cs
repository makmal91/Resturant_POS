using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Payments.DTOs;
using POSSystem.Application.Payments.Interfaces;
using POSSystem.Domain;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/payments")]
public class InvoicePaymentsController : ControllerBase
{
    private readonly IInvoicePaymentService _paymentService;

    public InvoicePaymentsController(IInvoicePaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("customers")]
    public async Task<IActionResult> RecordCustomerPayment([FromBody] RecordCustomerPaymentDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        dto.CreatedBy = this.ResolveUserId();

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (dto.CustomerId <= 0)
            return BadRequest(new { message = "CustomerId is required." });

        try
        {
            var result = await _paymentService.RecordCustomerPaymentAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("suppliers")]
    public async Task<IActionResult> RecordSupplierPayment([FromBody] RecordSupplierPaymentDto dto)
    {
        dto.BusinessId = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
        dto.BranchId = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
        dto.CreatedBy = this.ResolveUserId();

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (dto.SupplierId <= 0)
            return BadRequest(new { message = "SupplierId is required." });

        try
        {
            var result = await _paymentService.RecordSupplierPaymentAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("customers/{customerId:int}/outstanding-invoices")]
    public async Task<IActionResult> GetCustomerOutstandingInvoices(
        int customerId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        if (branch <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (customerId <= 0)
            return BadRequest(new { message = "CustomerId is required." });

        var invoices = await _paymentService.GetOutstandingSaleInvoicesAsync(customerId, biz, branch);
        return Ok(invoices);
    }

    [HttpGet("suppliers/{supplierId:int}/outstanding-invoices")]
    public async Task<IActionResult> GetSupplierOutstandingInvoices(
        int supplierId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        if (branch <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (supplierId <= 0)
            return BadRequest(new { message = "SupplierId is required." });

        var invoices = await _paymentService.GetOutstandingPurchaseInvoicesAsync(supplierId, biz, branch);
        return Ok(invoices);
    }

    [HttpGet("sales/{saleInvoiceId:int}")]
    public async Task<IActionResult> GetSaleInvoicePayments(
        int saleInvoiceId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        if (branch <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var payments = await _paymentService.GetPaymentsForSaleInvoiceAsync(saleInvoiceId, biz, branch);
            var balance = await _paymentService.GetSaleInvoiceBalanceAsync(saleInvoiceId, biz, branch);
            return Ok(new { balance, payments });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("purchases/{purchaseId:int}")]
    public async Task<IActionResult> GetPurchasePayments(
        int purchaseId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        if (branch <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var payments = await _paymentService.GetPaymentsForPurchaseAsync(purchaseId, biz, branch);
            var balance = await _paymentService.GetPurchaseBalanceAsync(purchaseId, biz, branch);
            return Ok(new { balance, payments });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("sales/{saleInvoiceId:int}/balance")]
    public async Task<IActionResult> GetSaleInvoiceBalance(
        int saleInvoiceId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        if (branch <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var balance = await _paymentService.GetSaleInvoiceBalanceAsync(saleInvoiceId, biz, branch);
            return Ok(balance);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("purchases/{purchaseId:int}/balance")]
    public async Task<IActionResult> GetPurchaseBalance(
        int purchaseId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        if (branch <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var balance = await _paymentService.GetPurchaseBalanceAsync(purchaseId, biz, branch);
            return Ok(balance);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
