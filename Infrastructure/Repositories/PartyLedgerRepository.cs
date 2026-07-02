using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Ledger.DTOs;
using POSSystem.Application.Ledger.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;
using CustomerEntity = POSSystem.Domain.Customer;
using SupplierEntity = POSSystem.Domain.Supplier;

namespace POSSystem.Infrastructure.Repositories;

/// <summary>
/// Party lookup and document-based activity for customer/supplier ledger screens.
/// </summary>
public class PartyLedgerRepository : IPartyLedgerRepository
{
    private readonly POSDbContext _db;

    public PartyLedgerRepository(POSDbContext db) => _db = db;

    public Task<CustomerEntity?> GetCustomerAsync(int customerId, int businessId, int branchId) =>
        _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == customerId
                && c.BusinessId == businessId
                && c.BranchId == branchId
                && !c.IsDeleted);

    public Task<SupplierEntity?> GetSupplierAsync(int supplierId, int businessId, int branchId) =>
        _db.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.Id == supplierId
                && s.BusinessId == businessId
                && s.BranchId == branchId
                && !s.IsDeleted);

    public async Task<List<PartyLedgerSourceDto>> GetSupplierActivityAsync(
        int supplierId, int businessId, int branchId, bool includeReversals)
    {
        var sources = new List<PartyLedgerSourceDto>();

        var purchases = await _db.Purchases.AsNoTracking()
            .Where(p => p.SupplierId == supplierId
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted
                        && p.Status == PurchaseStatus.Posted)
            .OrderBy(p => p.PurchaseDate)
            .ThenBy(p => p.Id)
            .Select(p => new { p.Id, p.InvoiceNo, p.PurchaseDate, p.TotalAmount, p.IsCreditPurchase, p.Notes })
            .ToListAsync();

        foreach (var purchase in purchases)
        {
            var isCredit = purchase.IsCreditPurchase;
            sources.Add(new PartyLedgerSourceDto
            {
                Id = purchase.Id,
                Date = purchase.PurchaseDate,
                Type = isCredit
                    ? nameof(SupplierLedgerTransactionType.CreditPurchase)
                    : nameof(SupplierLedgerTransactionType.CashPurchase),
                Description = BuildPurchaseDescription(purchase.InvoiceNo, purchase.Notes, isCredit),
                Amount = purchase.TotalAmount,
                ReferenceId = purchase.Id,
                AffectsBalance = isCredit,
            });
        }

        sources.AddRange(await LoadSupplierPaymentsAsync(
            supplierId, businessId, branchId, includeReversals));

        return sources;
    }

    public async Task<List<PartyLedgerSourceDto>> GetCustomerActivityAsync(
        int customerId, int businessId, int branchId, bool includeReversals)
    {
        var sources = new List<PartyLedgerSourceDto>();

        var sales = await _db.SaleInvoices.AsNoTracking()
            .Where(s => s.CustomerId == customerId
                        && s.BusinessId == businessId
                        && s.BranchId == branchId
                        && !s.IsDeleted
                        && s.Status == SaleInvoiceStatus.Completed)
            .OrderBy(s => s.SaleDate)
            .ThenBy(s => s.Id)
            .Select(s => new { s.Id, s.InvoiceNo, s.SaleDate, s.GrandTotal, s.IsCreditSale, s.Notes })
            .ToListAsync();

        foreach (var sale in sales)
        {
            var isCredit = sale.IsCreditSale;
            sources.Add(new PartyLedgerSourceDto
            {
                Id = sale.Id,
                Date = sale.SaleDate,
                Type = isCredit
                    ? nameof(CustomerLedgerTransactionType.CreditSale)
                    : nameof(CustomerLedgerTransactionType.CashSale),
                Description = BuildSaleDescription(sale.InvoiceNo, sale.Notes, isCredit),
                Amount = sale.GrandTotal,
                ReferenceId = sale.Id,
                AffectsBalance = isCredit,
            });
        }

        sources.AddRange(await LoadCustomerPaymentsAsync(
            customerId, businessId, branchId, includeReversals));

        return sources;
    }

    private async Task<List<PartyLedgerSourceDto>> LoadSupplierPaymentsAsync(
        int supplierId, int businessId, int branchId, bool includeReversals)
    {
        var query = _db.InvoicePayments.AsNoTracking()
            .Include(p => p.Allocations.Where(a => !a.IsDeleted))
            .ThenInclude(a => a.Purchase)
            .Include(p => p.Purchase)
            .Where(p => p.SupplierId == supplierId
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && p.Module == InvoicePaymentModule.Purchase
                        && !p.IsDeleted);

        if (!includeReversals)
            query = query.Where(p => !p.IsReversed);

        var payments = await query
            .OrderBy(p => p.PaymentDate)
            .ThenBy(p => p.Id)
            .ToListAsync();

        return payments.Select(MapSupplierPayment).ToList();
    }

    private async Task<List<PartyLedgerSourceDto>> LoadCustomerPaymentsAsync(
        int customerId, int businessId, int branchId, bool includeReversals)
    {
        var query = _db.InvoicePayments.AsNoTracking()
            .Include(p => p.Allocations.Where(a => !a.IsDeleted))
            .ThenInclude(a => a.SaleInvoice)
            .Include(p => p.SaleInvoice)
            .Where(p => p.CustomerId == customerId
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && p.Module == InvoicePaymentModule.Sale
                        && !p.IsDeleted);

        if (!includeReversals)
            query = query.Where(p => !p.IsReversed);

        var payments = await query
            .OrderBy(p => p.PaymentDate)
            .ThenBy(p => p.Id)
            .ToListAsync();

        return payments.Select(MapCustomerPayment).ToList();
    }

    private static PartyLedgerSourceDto MapSupplierPayment(InvoicePayment payment)
    {
        var allocations = payment.Allocations
            .Where(a => !a.IsDeleted && a.PurchaseId.HasValue)
            .Select(a => new PartyLedgerInvoiceAllocationDto
            {
                InvoiceId = a.PurchaseId!.Value,
                InvoiceNo = a.Purchase?.InvoiceNo ?? string.Empty,
                AppliedAmount = a.AppliedAmount,
            })
            .ToList();

        return new PartyLedgerSourceDto
        {
            Id = payment.Id,
            Date = payment.PaymentDate,
            Type = payment.IsReversed
                ? nameof(SupplierLedgerTransactionType.Reversal)
                : nameof(SupplierLedgerTransactionType.PaymentMade),
            Description = BuildPaymentDescription(payment, allocations, isCustomer: false),
            Amount = payment.Amount,
            ReferenceId = payment.PurchaseId ?? payment.Id,
            PaymentId = payment.Id,
            AffectsBalance = true,
            IsReversal = payment.IsReversed,
            HasInvoiceBreakdown = allocations.Count > 0,
            InvoiceAllocations = allocations,
        };
    }

    private static PartyLedgerSourceDto MapCustomerPayment(InvoicePayment payment)
    {
        var allocations = payment.Allocations
            .Where(a => !a.IsDeleted && a.SaleInvoiceId.HasValue)
            .Select(a => new PartyLedgerInvoiceAllocationDto
            {
                InvoiceId = a.SaleInvoiceId!.Value,
                InvoiceNo = a.SaleInvoice?.InvoiceNo ?? string.Empty,
                AppliedAmount = a.AppliedAmount,
            })
            .ToList();

        return new PartyLedgerSourceDto
        {
            Id = payment.Id,
            Date = payment.PaymentDate,
            Type = payment.IsReversed
                ? nameof(CustomerLedgerTransactionType.Reversal)
                : nameof(CustomerLedgerTransactionType.PaymentReceived),
            Description = BuildPaymentDescription(payment, allocations, isCustomer: true),
            Amount = payment.Amount,
            ReferenceId = payment.SaleInvoiceId ?? payment.Id,
            PaymentId = payment.Id,
            AffectsBalance = true,
            IsReversal = payment.IsReversed,
            HasInvoiceBreakdown = allocations.Count > 0,
            InvoiceAllocations = allocations,
        };
    }

    private static string BuildPurchaseDescription(string invoiceNo, string? notes, bool isCredit)
    {
        var prefix = isCredit ? "Credit purchase" : "Cash purchase";
        var text = $"{prefix} — {invoiceNo}";
        return string.IsNullOrWhiteSpace(notes) ? text : $"{text} | {notes.Trim()}";
    }

    private static string BuildSaleDescription(string invoiceNo, string? notes, bool isCredit)
    {
        var prefix = isCredit ? "Credit sale" : "Cash sale";
        var text = $"{prefix} — {invoiceNo}";
        return string.IsNullOrWhiteSpace(notes) ? text : $"{text} | {notes.Trim()}";
    }

    private static string BuildPaymentDescription(
        InvoicePayment payment,
        IReadOnlyList<PartyLedgerInvoiceAllocationDto> allocations,
        bool isCustomer)
    {
        if (!string.IsNullOrWhiteSpace(payment.Notes))
            return payment.Notes.Trim();

        if (allocations.Count == 1)
            return $"Invoice: {allocations[0].InvoiceNo}";

        if (payment.PurchaseId.HasValue && payment.Purchase != null)
            return $"Invoice: {payment.Purchase.InvoiceNo}";

        if (payment.SaleInvoiceId.HasValue && payment.SaleInvoice != null)
            return $"Invoice: {payment.SaleInvoice.InvoiceNo}";

        return isCustomer ? "Payment received" : "Payment made";
    }
}
