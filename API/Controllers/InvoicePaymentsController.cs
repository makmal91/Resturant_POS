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

    [HttpGet]
    public async Task<IActionResult> ListPayments(
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] InvoicePaymentModule? module,
        [FromQuery] int? customerId,
        [FromQuery] int? supplierId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] bool includeReversed = false)
    {
        var filter = new PaymentListFilterDto
        {
            BusinessId = this.ResolveBusinessId(businessId),
            BranchId = this.ResolveBranchId(branchId),
            Module = module,
            CustomerId = customerId,
            SupplierId = supplierId,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize,
            IncludeReversed = includeReversed,
        };

        if (filter.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        var result = await _paymentService.ListPaymentsAsync(filter);
        return Ok(new
        {
            payments = result.Data,
            totalRecords = result.TotalRecords,
            totalPages = result.TotalPages,
            currentPage = result.CurrentPage,
            pageSize,
        });
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

    [HttpPut("{paymentId:int}")]
    public async Task<IActionResult> UpdatePayment(
        int paymentId,
        [FromBody] UpdatePaymentDto dto,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        dto.BusinessId = this.ResolveBusinessId(businessId ?? (dto.BusinessId > 0 ? dto.BusinessId : null));
        dto.BranchId = this.ResolveBranchId(branchId ?? (dto.BranchId > 0 ? dto.BranchId : null));
        dto.ModifiedBy = this.ResolveUserId();

        if (dto.BranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var result = await _paymentService.UpdatePaymentAsync(paymentId, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{paymentId:int}/reverse")]
    public async Task<IActionResult> ReversePayment(
        int paymentId,
        [FromBody] ReversePaymentRequest? request,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        if (branch <= 0)
            return BadRequest(new { message = "BranchId is required." });

        try
        {
            var result = await _paymentService.ReversePaymentAsync(new ReversePaymentDto
            {
                PaymentId = paymentId,
                Reason = request?.Reason,
                BusinessId = biz,
                BranchId = branch,
                ReversedBy = this.ResolveUserId()
            });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{paymentId:int}")]
    public async Task<IActionResult> GetPayment(
        int paymentId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        if (branch <= 0)
            return BadRequest(new { message = "BranchId is required." });

        var payment = await _paymentService.GetPaymentByIdAsync(paymentId, biz, branch);
        return payment == null ? NotFound() : Ok(payment);
    }

    [HttpGet("customers/{customerId:int}/outstanding-invoices")]
    public async Task<IActionResult> GetCustomerOutstandingInvoices(
        int customerId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int? excludePaymentId)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        if (branch <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (customerId <= 0)
            return BadRequest(new { message = "CustomerId is required." });

        var invoices = await _paymentService.GetOutstandingSaleInvoicesAsync(customerId, biz, branch, excludePaymentId);
        return Ok(invoices);
    }

    [HttpGet("suppliers/{supplierId:int}/outstanding-invoices")]
    public async Task<IActionResult> GetSupplierOutstandingInvoices(
        int supplierId,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int? excludePaymentId)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);

        if (branch <= 0)
            return BadRequest(new { message = "BranchId is required." });

        if (supplierId <= 0)
            return BadRequest(new { message = "SupplierId is required." });

        var invoices = await _paymentService.GetOutstandingPurchaseInvoicesAsync(supplierId, biz, branch, excludePaymentId);
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
