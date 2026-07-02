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

    public async Task<PaymentAllocation> AddAllocationAsync(PaymentAllocation allocation)
    {
        await _db.PaymentAllocations.AddAsync(allocation);
        return allocation;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public Task<InvoicePayment?> GetByIdAsync(int paymentId, int businessId, int branchId)
        => _db.InvoicePayments
            .Include(p => p.Customer)
            .Include(p => p.Supplier)
            .Include(p => p.SaleInvoice)
            .Include(p => p.Purchase)
            .Include(p => p.Allocations.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == paymentId
                                      && p.BusinessId == businessId
                                      && p.BranchId == branchId
                                      && !p.IsDeleted);

    public Task<List<PaymentAllocation>> GetAllocationsByPaymentIdAsync(int paymentId, int businessId, int branchId)
        => _db.PaymentAllocations
            .Include(a => a.SaleInvoice)
            .Include(a => a.Purchase)
            .Where(a => a.InvoicePaymentId == paymentId
                        && a.BusinessId == businessId
                        && a.BranchId == branchId
                        && !a.IsDeleted)
            .ToListAsync();

    public async Task<decimal> GetTotalPaidForSaleInvoiceAsync(int saleInvoiceId, int businessId, int branchId)
    {
        var direct = await _db.InvoicePayments
            .Where(p => p.SaleInvoiceId == saleInvoiceId
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted
                        && !p.IsReversed)
            .SumAsync(p => p.Amount);

        var allocated = await _db.PaymentAllocations
            .Where(a => a.SaleInvoiceId == saleInvoiceId
                        && a.BusinessId == businessId
                        && a.BranchId == branchId
                        && !a.IsDeleted)
            .Join(
                _db.InvoicePayments.Where(p => !p.IsDeleted && !p.IsReversed),
                a => a.InvoicePaymentId,
                p => p.Id,
                (a, _) => a.AppliedAmount)
            .SumAsync();

        return direct + allocated;
    }

    public async Task<decimal> GetTotalPaidForPurchaseAsync(int purchaseId, int businessId, int branchId)
    {
        var direct = await _db.InvoicePayments
            .Where(p => p.PurchaseId == purchaseId
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted
                        && !p.IsReversed)
            .SumAsync(p => p.Amount);

        var allocated = await _db.PaymentAllocations
            .Where(a => a.PurchaseId == purchaseId
                        && a.BusinessId == businessId
                        && a.BranchId == branchId
                        && !a.IsDeleted)
            .Join(
                _db.InvoicePayments.Where(p => !p.IsDeleted && !p.IsReversed),
                a => a.InvoicePaymentId,
                p => p.Id,
                (a, _) => a.AppliedAmount)
            .SumAsync();

        return direct + allocated;
    }

    public async Task<Dictionary<int, decimal>> GetPaidTotalsForSaleInvoicesAsync(
        IEnumerable<int> saleInvoiceIds, int businessId, int branchId)
    {
        var ids = saleInvoiceIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, decimal>();

        var direct = await _db.InvoicePayments
            .Where(p => p.SaleInvoiceId.HasValue
                        && ids.Contains(p.SaleInvoiceId.Value)
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted
                        && !p.IsReversed)
            .GroupBy(p => p.SaleInvoiceId!.Value)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Total);

        var allocated = await _db.PaymentAllocations
            .Where(a => a.SaleInvoiceId.HasValue
                        && ids.Contains(a.SaleInvoiceId.Value)
                        && a.BusinessId == businessId
                        && a.BranchId == branchId
                        && !a.IsDeleted)
            .Join(
                _db.InvoicePayments.Where(p => !p.IsDeleted && !p.IsReversed),
                a => a.InvoicePaymentId,
                p => p.Id,
                (a, _) => a)
            .GroupBy(a => a.SaleInvoiceId!.Value)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(x => x.AppliedAmount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Total);

        return ids.ToDictionary(
            id => id,
            id => direct.GetValueOrDefault(id) + allocated.GetValueOrDefault(id));
    }

    public async Task<Dictionary<int, decimal>> GetPaidTotalsForPurchasesAsync(
        IEnumerable<int> purchaseIds, int businessId, int branchId)
    {
        var ids = purchaseIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, decimal>();

        var direct = await _db.InvoicePayments
            .Where(p => p.PurchaseId.HasValue
                        && ids.Contains(p.PurchaseId.Value)
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted
                        && !p.IsReversed)
            .GroupBy(p => p.PurchaseId!.Value)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Total);

        var allocated = await _db.PaymentAllocations
            .Where(a => a.PurchaseId.HasValue
                        && ids.Contains(a.PurchaseId.Value)
                        && a.BusinessId == businessId
                        && a.BranchId == branchId
                        && !a.IsDeleted)
            .Join(
                _db.InvoicePayments.Where(p => !p.IsDeleted && !p.IsReversed),
                a => a.InvoicePaymentId,
                p => p.Id,
                (a, _) => a)
            .GroupBy(a => a.PurchaseId!.Value)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(x => x.AppliedAmount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Total);

        return ids.ToDictionary(
            id => id,
            id => direct.GetValueOrDefault(id) + allocated.GetValueOrDefault(id));
    }

    public async Task<Dictionary<int, decimal>> GetPaidTotalsForSaleInvoicesAsOfAsync(
        IEnumerable<int> saleInvoiceIds, int businessId, int branchId, DateTime asOfDate)
    {
        var ids = saleInvoiceIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, decimal>();

        var asOfExclusive = asOfDate.Date.AddDays(1);

        var direct = await _db.InvoicePayments
            .Where(p => p.SaleInvoiceId.HasValue
                        && ids.Contains(p.SaleInvoiceId.Value)
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted
                        && !p.IsReversed
                        && p.PaymentDate < asOfExclusive)
            .GroupBy(p => p.SaleInvoiceId!.Value)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Total);

        var allocated = await _db.PaymentAllocations
            .Where(a => a.SaleInvoiceId.HasValue
                        && ids.Contains(a.SaleInvoiceId.Value)
                        && a.BusinessId == businessId
                        && a.BranchId == branchId
                        && !a.IsDeleted)
            .Join(
                _db.InvoicePayments.Where(p =>
                    !p.IsDeleted && !p.IsReversed && p.PaymentDate < asOfExclusive),
                a => a.InvoicePaymentId,
                p => p.Id,
                (a, _) => a)
            .GroupBy(a => a.SaleInvoiceId!.Value)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(x => x.AppliedAmount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Total);

        return ids.ToDictionary(
            id => id,
            id => direct.GetValueOrDefault(id) + allocated.GetValueOrDefault(id));
    }

    public async Task<Dictionary<int, decimal>> GetPaidTotalsForPurchasesAsOfAsync(
        IEnumerable<int> purchaseIds, int businessId, int branchId, DateTime asOfDate)
    {
        var ids = purchaseIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, decimal>();

        var asOfExclusive = asOfDate.Date.AddDays(1);

        var direct = await _db.InvoicePayments
            .Where(p => p.PurchaseId.HasValue
                        && ids.Contains(p.PurchaseId.Value)
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted
                        && !p.IsReversed
                        && p.PaymentDate < asOfExclusive)
            .GroupBy(p => p.PurchaseId!.Value)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Total);

        var allocated = await _db.PaymentAllocations
            .Where(a => a.PurchaseId.HasValue
                        && ids.Contains(a.PurchaseId.Value)
                        && a.BusinessId == businessId
                        && a.BranchId == branchId
                        && !a.IsDeleted)
            .Join(
                _db.InvoicePayments.Where(p =>
                    !p.IsDeleted && !p.IsReversed && p.PaymentDate < asOfExclusive),
                a => a.InvoicePaymentId,
                p => p.Id,
                (a, _) => a)
            .GroupBy(a => a.PurchaseId!.Value)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(x => x.AppliedAmount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Total);

        return ids.ToDictionary(
            id => id,
            id => direct.GetValueOrDefault(id) + allocated.GetValueOrDefault(id));
    }

    public Task<List<InvoicePayment>> GetBySaleInvoiceIdAsync(int saleInvoiceId, int businessId, int branchId)
        => _db.InvoicePayments
            .Include(p => p.Customer)
            .Include(p => p.SaleInvoice)
            .Include(p => p.Allocations.Where(a => !a.IsDeleted))
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
            .Include(p => p.Allocations.Where(a => !a.IsDeleted))
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
            .Include(p => p.Allocations.Where(a => !a.IsDeleted))
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

    public async Task SyncSaleInvoicePaidCacheAsync(
        int saleInvoiceId, int businessId, int branchId, decimal paidTotal, InvoiceSettlementStatus status)
    {
        var invoice = await _db.SaleInvoices
            .FirstOrDefaultAsync(i => i.Id == saleInvoiceId
                                      && i.BusinessId == businessId
                                      && i.BranchId == branchId
                                      && !i.IsDeleted);

        if (invoice == null) return;

        invoice.PaidAmount = paidTotal;
        invoice.SettlementStatus = status;
        await _db.SaveChangesAsync();
    }

    public async Task SyncPurchasePaidCacheAsync(
        int purchaseId, int businessId, int branchId, decimal paidTotal, InvoiceSettlementStatus status)
    {
        var purchase = await _db.Purchases
            .FirstOrDefaultAsync(p => p.Id == purchaseId
                                      && p.BusinessId == businessId
                                      && p.BranchId == branchId
                                      && !p.IsDeleted);

        if (purchase == null) return;

        purchase.PaidAmount = paidTotal;
        purchase.SettlementStatus = status;
        await _db.SaveChangesAsync();
    }

    public async Task<List<OutstandingInvoiceOptionDto>> GetOutstandingSaleInvoicesAsync(
        int customerId, int businessId, int branchId, int? excludePaymentId = null)
    {
        var invoices = await _db.SaleInvoices
            .Where(i => i.CustomerId == customerId
                        && i.BusinessId == businessId
                        && i.BranchId == branchId
                        && !i.IsDeleted
                        && i.Status == SaleInvoiceStatus.Completed
                        && i.IsCreditSale)
            .OrderBy(i => i.SaleDate)
            .ThenBy(i => i.Id)
            .Select(i => new { i.Id, i.InvoiceNo, i.SaleDate, i.GrandTotal })
            .ToListAsync();

        if (invoices.Count == 0) return new List<OutstandingInvoiceOptionDto>();

        var paidMap = await GetPaidTotalsForSaleInvoicesAsync(
            invoices.Select(i => i.Id), businessId, branchId);

        if (excludePaymentId.HasValue)
            await ApplyPaymentExclusionToPaidMapAsync(paidMap, excludePaymentId.Value, isSale: true, businessId, branchId);

        return invoices
            .Select(i =>
            {
                var paid = paidMap.GetValueOrDefault(i.Id);
                var balance = i.GrandTotal - paid;
                return new OutstandingInvoiceOptionDto
                {
                    InvoiceId = i.Id,
                    InvoiceNo = i.InvoiceNo,
                    InvoiceDate = i.SaleDate,
                    InvoiceTotal = i.GrandTotal,
                    PaidAmount = paid,
                    BalanceDue = balance,
                    SettlementStatus = ResolveSettlementStatus(paid, i.GrandTotal).ToString()
                };
            })
            .Where(x => x.BalanceDue > 0.005m)
            .ToList();
    }

    public async Task<List<OutstandingInvoiceOptionDto>> GetOutstandingPurchaseInvoicesAsync(
        int supplierId, int businessId, int branchId, int? excludePaymentId = null)
    {
        var purchases = await _db.Purchases
            .Where(p => p.SupplierId == supplierId
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted
                        && p.Status == PurchaseStatus.Posted)
            .OrderBy(p => p.PurchaseDate)
            .ThenBy(p => p.Id)
            .Select(p => new { p.Id, p.InvoiceNo, p.PurchaseDate, p.TotalAmount })
            .ToListAsync();

        if (purchases.Count == 0) return new List<OutstandingInvoiceOptionDto>();

        var paidMap = await GetPaidTotalsForPurchasesAsync(
            purchases.Select(p => p.Id), businessId, branchId);

        if (excludePaymentId.HasValue)
            await ApplyPaymentExclusionToPaidMapAsync(paidMap, excludePaymentId.Value, isSale: false, businessId, branchId);

        return purchases
            .Select(p =>
            {
                var paid = paidMap.GetValueOrDefault(p.Id);
                var balance = p.TotalAmount - paid;
                return new OutstandingInvoiceOptionDto
                {
                    InvoiceId = p.Id,
                    InvoiceNo = p.InvoiceNo,
                    InvoiceDate = p.PurchaseDate,
                    InvoiceTotal = p.TotalAmount,
                    PaidAmount = paid,
                    BalanceDue = balance,
                    SettlementStatus = ResolveSettlementStatus(paid, p.TotalAmount).ToString()
                };
            })
            .Where(x => x.BalanceDue > 0.005m)
            .ToList();
    }

    public async Task<decimal> GetSupplierOutstandingPayableAsync(
        int supplierId, int businessId, int branchId, DateTime? asOfDate = null)
    {
        var purchaseQuery = _db.Purchases
            .AsNoTracking()
            .Where(p => p.SupplierId == supplierId
                        && p.BusinessId == businessId
                        && p.BranchId == branchId
                        && !p.IsDeleted
                        && p.Status == PurchaseStatus.Posted);

        if (asOfDate.HasValue)
            purchaseQuery = purchaseQuery.Where(p => p.PurchaseDate < asOfDate.Value.Date);

        var purchases = await purchaseQuery
            .Select(p => new { p.Id, p.TotalAmount })
            .ToListAsync();

        if (purchases.Count == 0)
            return 0m;

        var paidMap = asOfDate.HasValue
            ? await GetPaidTotalsForPurchasesAsOfAsync(
                purchases.Select(p => p.Id), businessId, branchId, asOfDate.Value)
            : await GetPaidTotalsForPurchasesAsync(
                purchases.Select(p => p.Id), businessId, branchId);

        return purchases
            .Select(p => p.TotalAmount - paidMap.GetValueOrDefault(p.Id))
            .Where(due => due > 0.005m)
            .Sum();
    }

    public Task SoftDeletePaymentAsync(InvoicePayment payment, int? deletedBy)
    {
        payment.IsDeleted = true;
        payment.DeletedBy = deletedBy;
        payment.ModifiedAt = DateTime.UtcNow;
        payment.ModifiedBy = deletedBy;
        return Task.CompletedTask;
    }

    public Task SoftDeleteAllocationsAsync(IEnumerable<PaymentAllocation> allocations, int? deletedBy)
    {
        foreach (var allocation in allocations)
        {
            allocation.IsDeleted = true;
            allocation.ModifiedAt = DateTime.UtcNow;
            allocation.ModifiedBy = deletedBy;
        }

        return Task.CompletedTask;
    }

    private async Task ApplyPaymentExclusionToPaidMapAsync(
        Dictionary<int, decimal> paidMap,
        int excludePaymentId,
        bool isSale,
        int businessId,
        int branchId)
    {
        var payment = await _db.InvoicePayments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == excludePaymentId
                                      && p.BusinessId == businessId
                                      && p.BranchId == branchId
                                      && !p.IsDeleted);

        if (payment == null)
            return;

        if (isSale && payment.SaleInvoiceId.HasValue)
            DecrementPaidMap(paidMap, payment.SaleInvoiceId.Value, payment.Amount);
        if (!isSale && payment.PurchaseId.HasValue)
            DecrementPaidMap(paidMap, payment.PurchaseId.Value, payment.Amount);

        var allocations = await _db.PaymentAllocations
            .AsNoTracking()
            .Where(a => a.InvoicePaymentId == excludePaymentId
                        && a.BusinessId == businessId
                        && a.BranchId == branchId
                        && !a.IsDeleted)
            .ToListAsync();

        foreach (var allocation in allocations)
        {
            if (isSale && allocation.SaleInvoiceId.HasValue)
                DecrementPaidMap(paidMap, allocation.SaleInvoiceId.Value, allocation.AppliedAmount);
            if (!isSale && allocation.PurchaseId.HasValue)
                DecrementPaidMap(paidMap, allocation.PurchaseId.Value, allocation.AppliedAmount);
        }
    }

    private static void DecrementPaidMap(Dictionary<int, decimal> paidMap, int invoiceId, decimal amount)
    {
        if (!paidMap.TryGetValue(invoiceId, out var current))
            current = 0;

        var next = current - amount;
        if (next <= 0.005m)
            paidMap.Remove(invoiceId);
        else
            paidMap[invoiceId] = next;
    }

    private static InvoiceSettlementStatus ResolveSettlementStatus(decimal paid, decimal total)
    {
        if (paid <= 0.005m) return InvoiceSettlementStatus.Pending;
        if (paid >= total - 0.005m) return InvoiceSettlementStatus.Paid;
        return InvoiceSettlementStatus.Partial;
    }
}
