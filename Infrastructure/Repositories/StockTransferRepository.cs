using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.StockTransfer.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class StockTransferRepository : IStockTransferRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public StockTransferRepository(POSDbContext context) => _context = context;

    public async Task<PagedResultDto<StockTransferVoucher>> GetPagedAsync(
        int businessId, int branchId, int page, int pageSize, string? search = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _context.StockTransferVouchers
            .IgnoreQueryFilters()
            .Where(v => !v.IsDeleted && v.BusinessId == businessId);

        if (branchId > 0)
            query = query.Where(v => v.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(v =>
                v.TransferNo.ToLower().Contains(term)
                || (v.Description != null && v.Description.ToLower().Contains(term)));
        }

        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);
        if (totalPages > 0 && page > totalPages)
            page = totalPages;

        var data = await query
            .Include(v => v.FromWarehouse)
            .Include(v => v.ToWarehouse)
            .Include(v => v.Branch)
            .Include(v => v.Lines.Where(l => !l.IsDeleted))
            .OrderByDescending(v => v.TransferDate)
            .ThenByDescending(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<StockTransferVoucher>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page
        };
    }

    public async Task<StockTransferVoucher?> GetByIdWithLinesAsync(int id, int businessId, int branchId)
    {
        var query = _context.StockTransferVouchers
            .IgnoreQueryFilters()
            .Where(v => v.Id == id && !v.IsDeleted && v.BusinessId == businessId);

        if (branchId > 0)
            query = query.Where(v => v.BranchId == branchId);

        return await query
            .Include(v => v.FromWarehouse)
            .Include(v => v.ToWarehouse)
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

    public async Task<bool> TransferNoExistsAsync(string transferNo, int businessId, int branchId, int? excludeId = null)
    {
        var normalized = transferNo.Trim().ToLower();
        return await _context.StockTransferVouchers
            .IgnoreQueryFilters()
            .AnyAsync(v => !v.IsDeleted
                           && v.BusinessId == businessId
                           && v.BranchId == branchId
                           && v.TransferNo.ToLower() == normalized
                           && (!excludeId.HasValue || v.Id != excludeId.Value));
    }

    public async Task AddAsync(StockTransferVoucher voucher) =>
        await _context.StockTransferVouchers.AddAsync(voucher);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
