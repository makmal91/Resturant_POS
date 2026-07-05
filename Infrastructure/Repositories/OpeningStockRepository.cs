using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.OpeningStock.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class OpeningStockRepository : IOpeningStockRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public OpeningStockRepository(POSDbContext context) => _context = context;

    public async Task<PagedResultDto<OpeningStockVoucher>> GetPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _context.OpeningStockVouchers
            .IgnoreQueryFilters()
            .Where(v => !v.IsDeleted && v.BusinessId == businessId);

        if (branchId > 0)
            query = query.Where(v => v.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(v =>
                v.VoucherNo.ToLower().Contains(term)
                || (v.Description != null && v.Description.ToLower().Contains(term)));
        }

        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);
        if (totalPages > 0 && page > totalPages)
            page = totalPages;

        var data = await query
            .Include(v => v.Warehouse)
            .Include(v => v.Branch)
            .Include(v => v.Lines.Where(l => !l.IsDeleted))
            .OrderByDescending(v => v.VoucherDate)
            .ThenByDescending(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<OpeningStockVoucher>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page
        };
    }

    public async Task<OpeningStockVoucher?> GetByIdWithLinesAsync(int id, int businessId, int branchId)
    {
        var query = _context.OpeningStockVouchers
            .IgnoreQueryFilters()
            .Where(v => v.Id == id && !v.IsDeleted && v.BusinessId == businessId);

        if (branchId > 0)
            query = query.Where(v => v.BranchId == branchId);

        return await query
            .Include(v => v.Warehouse)
            .Include(v => v.Branch)
            .Include(v => v.Lines.Where(l => !l.IsDeleted))
                .ThenInclude(l => l.Product)
                    .ThenInclude(p => p!.BaseUnit)
            .Include(v => v.Lines.Where(l => !l.IsDeleted))
                .ThenInclude(l => l.Variant)
            .Include(v => v.Lines.Where(l => !l.IsDeleted))
                .ThenInclude(l => l.Unit)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> VoucherNoExistsAsync(string voucherNo, int businessId, int branchId, int? excludeId = null)
    {
        var normalized = voucherNo.Trim().ToLower();
        return await _context.OpeningStockVouchers
            .IgnoreQueryFilters()
            .AnyAsync(v => !v.IsDeleted
                           && v.BusinessId == businessId
                           && v.BranchId == branchId
                           && v.VoucherNo.ToLower() == normalized
                           && (!excludeId.HasValue || v.Id != excludeId.Value));
    }

    public async Task AddAsync(OpeningStockVoucher voucher) =>
        await _context.OpeningStockVouchers.AddAsync(voucher);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
