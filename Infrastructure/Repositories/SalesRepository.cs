using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Sales.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class SalesRepository : ISalesRepository
{
    private readonly POSDbContext _db;

    public SalesRepository(POSDbContext db)
    {
        _db = db;
    }

    public async Task<Product?> GetProductByBarcodeAsync(string barcode, int businessId, int branchId)
    {
        var barcodeEntry = await _db.ProductBarcodes
            .Include(b => b.Product)
                .ThenInclude(p => p.Units)
            .Include(b => b.Product)
                .ThenInclude(p => p.Variants)
            .Include(b => b.Product)
                .ThenInclude(p => p.Barcodes)
            .Where(b => b.BarcodeValue == barcode
                && !b.IsDeleted
                && !b.Product.IsDeleted
                && b.Product.Status
                && b.Product.BusinessId == businessId
                && b.Product.BranchId == branchId)
            .FirstOrDefaultAsync();

        return barcodeEntry?.Product;
    }

    public async Task<List<Product>> SearchProductsAsync(string query, int businessId, int branchId, int take = 20)
    {
        var q = query.ToLower();
        return await _db.Products
            .Include(p => p.Units)
            .Include(p => p.Variants)
            .Include(p => p.Barcodes)
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => !p.IsDeleted
                && p.Status
                && p.BusinessId == businessId
                && p.BranchId == branchId
                && (p.ProductName.ToLower().Contains(q)
                    || p.ProductCode.ToLower().Contains(q)
                    || p.SKU.ToLower().Contains(q)
                    || p.Barcodes.Any(b => b.BarcodeValue.Contains(q) && !b.IsDeleted)))
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<Customer>> SearchCustomersAsync(string query, int businessId, int branchId, int take = 10)
    {
        var q = query.ToLower();
        return await _db.Customers
            .Where(c => !c.IsDeleted
                && c.BusinessId == businessId
                && c.BranchId == branchId
                && (c.Name.ToLower().Contains(q)
                    || (c.Phone != null && c.Phone.Contains(query))))
            .Take(take)
            .ToListAsync();
    }

    public async Task<SaleInvoice?> GetByIdAsync(int id, int businessId, int branchId)
    {
        return await _db.SaleInvoices
            .Include(s => s.Customer)
            .Include(s => s.Warehouse)
            .Include(s => s.Branch)
            .Include(s => s.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Product)
            .Include(s => s.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Variant)
            .Include(s => s.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Unit)
            .FirstOrDefaultAsync(s => s.Id == id
                && s.BusinessId == businessId
                && (branchId == 0 || s.BranchId == branchId)
                && !s.IsDeleted);
    }

    public async Task<PagedResultDto<SaleInvoice>> GetPagedAsync(
        int businessId, int branchId, int page, int pageSize,
        string? search, SaleInvoiceStatus? status, DateTime? dateFrom, DateTime? dateTo)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.SaleInvoices
            .Include(s => s.Customer)
            .Include(s => s.Warehouse)
            .Include(s => s.Branch)
            .Where(s => s.BusinessId == businessId && (branchId == 0 || s.BranchId == branchId) && !s.IsDeleted);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (dateFrom.HasValue)
            query = query.Where(s => s.SaleDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(s => s.SaleDate <= dateTo.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            query = query.Where(s =>
                s.InvoiceNo.ToLower().Contains(q) ||
                (s.Customer != null && s.Customer.Name.ToLower().Contains(q)) ||
                (s.CashierName != null && s.CashierName.ToLower().Contains(q)));
        }

        var total     = await query.CountAsync();
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        if (totalPages > 0 && page > totalPages) page = totalPages;

        var data = await query
            .OrderByDescending(s => s.SaleDate)
            .ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<SaleInvoice>
        {
            Data = data, TotalRecords = total, TotalPages = totalPages, CurrentPage = page
        };
    }

    public async Task<List<SaleInvoice>> GetHeldBillsAsync(int businessId, int branchId)
    {
        return await _db.SaleInvoices
            .Include(s => s.Customer)
            .Include(s => s.Warehouse)
            .Include(s => s.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Product)
            .Include(s => s.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Variant)
            .Include(s => s.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Unit)
            .Where(s => s.Status == SaleInvoiceStatus.Held
                && s.BusinessId == businessId
                && s.BranchId == branchId
                && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(SaleInvoice invoice)
    {
        await _db.SaleInvoices.AddAsync(invoice);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
