using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Payments.DTOs;
using POSSystem.Application.Payments.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;
using CustomerEntity = POSSystem.Domain.Customer;
using SupplierEntity = POSSystem.Domain.Supplier;

namespace POSSystem.Infrastructure.Repositories;

public class InvoicePaymentRepository : IInvoicePaymentRepository
{
    private readonly POSDbContext _db;

    public InvoicePaymentRepository(POSDbContext db) => _db = db;

    public async Task<InvoicePayment> AddAsync(InvoicePayment payment)
    {
        await _db.InvoicePayments.AddAsync(payment);
        return payment;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public Task<decimal> GetTotalPaidForSaleInvoiceAsync(int saleInvoiceId, int businessId, int branchId)
        => _db.InvoicePayments
            .Where(p => p.SaleInvoiceId == saleInvoiceId
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted)
            .SumAsync(p => p.Amount);

    public Task<decimal> GetTotalPaidForPurchaseAsync(int purchaseId, int businessId, int branchId)
        => _db.InvoicePayments
            .Where(p => p.PurchaseId == purchaseId
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted)
            .SumAsync(p => p.Amount);

    public async Task<Dictionary<int, decimal>> GetPaidTotalsForSaleInvoicesAsync(
        IEnumerable<int> saleInvoiceIds, int businessId, int branchId)
    {
        var ids = saleInvoiceIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, decimal>();

        return await _db.InvoicePayments
            .Where(p => p.SaleInvoiceId.HasValue
                        && ids.Contains(p.SaleInvoiceId.Value)
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted)
            .GroupBy(p => p.SaleInvoiceId!.Value)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Total);
    }

    public async Task<Dictionary<int, decimal>> GetPaidTotalsForPurchasesAsync(
        IEnumerable<int> purchaseIds, int businessId, int branchId)
    {
        var ids = purchaseIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, decimal>();

        return await _db.InvoicePayments
            .Where(p => p.PurchaseId.HasValue
                        && ids.Contains(p.PurchaseId.Value)
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted)
            .GroupBy(p => p.PurchaseId!.Value)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Total);
    }

    public Task<List<InvoicePayment>> GetBySaleInvoiceIdAsync(int saleInvoiceId, int businessId, int branchId)
        => _db.InvoicePayments
            .Include(p => p.Customer)
            .Include(p => p.SaleInvoice)
            .Where(p => p.SaleInvoiceId == saleInvoiceId
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

    public Task<List<InvoicePayment>> GetByPurchaseIdAsync(int purchaseId, int businessId, int branchId)
        => _db.InvoicePayments
            .Include(p => p.Supplier)
            .Include(p => p.Purchase)
            .Where(p => p.PurchaseId == purchaseId
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

    public Task<List<InvoicePayment>> GetFilteredAsync(InvoicePaymentFilterDto filter)
    {
        var query = _db.InvoicePayments
            .Include(p => p.Customer)
            .Include(p => p.Supplier)
            .Include(p => p.SaleInvoice)
            .Include(p => p.Purchase)
            .Where(p => p.BusinessId == filter.BusinessId
                        && p.BranchId == filter.BranchId
                        && !p.IsDeleted);

        if (filter.Module.HasValue)
            query = query.Where(p => p.Module == filter.Module.Value);

        if (filter.SaleInvoiceId.HasValue)
            query = query.Where(p => p.SaleInvoiceId == filter.SaleInvoiceId.Value);

        if (filter.PurchaseId.HasValue)
            query = query.Where(p => p.PurchaseId == filter.PurchaseId.Value);

        if (filter.CustomerId.HasValue)
            query = query.Where(p => p.CustomerId == filter.CustomerId.Value);

        if (filter.SupplierId.HasValue)
            query = query.Where(p => p.SupplierId == filter.SupplierId.Value);

        return query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync();
    }

    public Task<SaleInvoice?> GetSaleInvoiceAsync(int saleInvoiceId, int businessId, int branchId)
        => _db.SaleInvoices
            .FirstOrDefaultAsync(i => i.Id == saleInvoiceId
                                      && i.BusinessId == businessId
                                      && i.BranchId == branchId
                                      && !i.IsDeleted);

    public Task<Purchase?> GetPurchaseAsync(int purchaseId, int businessId, int branchId)
        => _db.Purchases
            .FirstOrDefaultAsync(p => p.Id == purchaseId
                                      && p.BusinessId == businessId
                                      && p.BranchId == branchId
                                      && !p.IsDeleted);

    public Task<CustomerEntity?> GetCustomerAsync(int customerId, int businessId, int branchId)
        => _db.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId
                                      && c.BusinessId == businessId
                                      && c.BranchId == branchId
                                      && !c.IsDeleted);

    public Task<SupplierEntity?> GetSupplierAsync(int supplierId, int businessId, int branchId)
        => _db.Suppliers
            .FirstOrDefaultAsync(s => s.Id == supplierId
                                      && s.BusinessId == businessId
                                      && s.BranchId == branchId
                                      && !s.IsDeleted);

    public async Task SyncSaleInvoicePaidCacheAsync(int saleInvoiceId, int businessId, int branchId, decimal paidTotal)
    {
        var invoice = await _db.SaleInvoices
            .FirstOrDefaultAsync(i => i.Id == saleInvoiceId
                                      && i.BusinessId == businessId
                                      && i.BranchId == branchId
                                      && !i.IsDeleted);

        if (invoice == null) return;

        invoice.PaidAmount = paidTotal;
        await _db.SaveChangesAsync();
    }

    public async Task<List<OutstandingInvoiceOptionDto>> GetOutstandingSaleInvoicesAsync(
        int customerId, int businessId, int branchId)
    {
        var invoices = await _db.SaleInvoices
            .Where(i => i.CustomerId == customerId
                        && i.BusinessId == businessId
                        && i.BranchId == branchId
                        && !i.IsDeleted
                        && i.Status == SaleInvoiceStatus.Completed)
            .OrderByDescending(i => i.SaleDate)
            .Select(i => new { i.Id, i.InvoiceNo, i.SaleDate, i.GrandTotal })
            .ToListAsync();

        if (invoices.Count == 0) return new List<OutstandingInvoiceOptionDto>();

        var paidMap = await GetPaidTotalsForSaleInvoicesAsync(
            invoices.Select(i => i.Id), businessId, branchId);

        return invoices
            .Select(i =>
            {
                var paid = paidMap.GetValueOrDefault(i.Id);
                return new OutstandingInvoiceOptionDto
                {
                    InvoiceId = i.Id,
                    InvoiceNo = i.InvoiceNo,
                    InvoiceDate = i.SaleDate,
                    InvoiceTotal = i.GrandTotal,
                    PaidAmount = paid,
                    BalanceDue = i.GrandTotal - paid
                };
            })
            .Where(x => x.BalanceDue > 0.005m)
            .ToList();
    }

    public async Task<List<OutstandingInvoiceOptionDto>> GetOutstandingPurchaseInvoicesAsync(
        int supplierId, int businessId, int branchId)
    {
        var purchases = await _db.Purchases
            .Where(p => p.SupplierId == supplierId
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted
                        && p.Status == PurchaseStatus.Posted)
            .OrderByDescending(p => p.PurchaseDate)
            .Select(p => new { p.Id, p.InvoiceNo, p.PurchaseDate, p.TotalAmount })
            .ToListAsync();

        if (purchases.Count == 0) return new List<OutstandingInvoiceOptionDto>();

        var paidMap = await GetPaidTotalsForPurchasesAsync(
            purchases.Select(p => p.Id), businessId, branchId);

        return purchases
            .Select(p =>
            {
                var paid = paidMap.GetValueOrDefault(p.Id);
                return new OutstandingInvoiceOptionDto
                {
                    InvoiceId = p.Id,
                    InvoiceNo = p.InvoiceNo,
                    InvoiceDate = p.PurchaseDate,
                    InvoiceTotal = p.TotalAmount,
                    PaidAmount = paid,
                    BalanceDue = p.TotalAmount - paid
                };
            })
            .Where(x => x.BalanceDue > 0.005m)
            .ToList();
    }
}
