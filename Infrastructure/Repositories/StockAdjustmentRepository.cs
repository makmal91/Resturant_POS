using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.StockAdjustment.DTOs;
using POSSystem.Application.StockAdjustment.Interfaces;
using POSSystem.Domain;
using StockAdjustmentEntity = POSSystem.Domain.StockAdjustment;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class StockAdjustmentRepository : IStockAdjustmentRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public StockAdjustmentRepository(POSDbContext context) => _context = context;

    public async Task<PagedResultDto<StockAdjustmentEntity>> GetPagedAsync(StockAdjustmentFilterDto filter)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

        var query = _context.StockAdjustments
            .IgnoreQueryFilters()
            .Where(a => !a.IsDeleted && a.BusinessId == filter.BusinessId);

        if (filter.BranchId > 0)
            query = query.Where(a => a.BranchId == filter.BranchId);

        if (filter.WarehouseId is > 0)
            query = query.Where(a => a.WarehouseId == filter.WarehouseId.Value);

        if (filter.AdjustmentTypeId is > 0)
            query = query.Where(a => a.AdjustmentTypeId == filter.AdjustmentTypeId.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(a => a.AdjustmentDate >= filter.FromDate.Value.Date);

        if (filter.ToDate.HasValue)
            query = query.Where(a => a.AdjustmentDate < filter.ToDate.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(a =>
                a.AdjustmentNo.ToLower().Contains(term)
                || (a.Remarks != null && a.Remarks.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Direction))
        {
            var dir = filter.Direction.Trim().ToLowerInvariant();
            if (dir == "gain")
                query = query.Where(a => a.Lines.Any(l => !l.IsDeleted && l.BaseQuantity > 0));
            else if (dir == "loss")
                query = query.Where(a => a.Lines.Any(l => !l.IsDeleted && l.BaseQuantity < 0));
        }

        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);
        if (totalPages > 0 && page > totalPages)
            page = totalPages;

        var data = await query
            .Include(a => a.Warehouse)
            .Include(a => a.AdjustmentType)
            .Include(a => a.Branch)
            .Include(a => a.Lines.Where(l => !l.IsDeleted))
            .OrderByDescending(a => a.AdjustmentDate)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<StockAdjustmentEntity>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page
        };
    }

    public async Task<StockAdjustmentEntity?> GetByIdWithLinesAsync(int id, int businessId, int branchId)
    {
        var query = _context.StockAdjustments
            .IgnoreQueryFilters()
            .Where(a => a.Id == id && !a.IsDeleted && a.BusinessId == businessId);

        if (branchId > 0)
            query = query.Where(a => a.BranchId == branchId);

        return await query
            .Include(a => a.Warehouse)
            .Include(a => a.AdjustmentType)
            .Include(a => a.Branch)
            .Include(a => a.Lines.Where(l => !l.IsDeleted))
                .ThenInclude(l => l.Product)
            .Include(a => a.Lines.Where(l => !l.IsDeleted))
                .ThenInclude(l => l.Variant)
            .Include(a => a.Lines.Where(l => !l.IsDeleted))
                .ThenInclude(l => l.Unit)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> AdjustmentNoExistsAsync(string adjustmentNo, int businessId, int branchId, int? excludeId = null)
    {
        var normalized = adjustmentNo.Trim().ToLower();
        return await _context.StockAdjustments
            .IgnoreQueryFilters()
            .AnyAsync(a => !a.IsDeleted
                           && a.BusinessId == businessId
                           && a.BranchId == branchId
                           && a.AdjustmentNo.ToLower() == normalized
                           && (!excludeId.HasValue || a.Id != excludeId.Value));
    }

    public async Task AddAsync(StockAdjustmentEntity adjustment) =>
        await _context.StockAdjustments.AddAsync(adjustment);

    public async Task<List<AdjustmentType>> GetActiveAdjustmentTypesAsync(int businessId, int branchId)
    {
        return await _context.AdjustmentTypes
            .IgnoreQueryFilters()
            .Include(t => t.ExpenseAccount)
            .Include(t => t.IncomeAccount)
            .Where(t => !t.IsDeleted && t.IsActive && t.BusinessId == businessId
                        && (branchId <= 0 || t.BranchId == branchId))
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<AdjustmentType?> GetAdjustmentTypeAsync(int id, int businessId, int branchId)
    {
        return await _context.AdjustmentTypes
            .IgnoreQueryFilters()
            .Include(t => t.ExpenseAccount)
            .Include(t => t.IncomeAccount)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.IsActive
                                      && t.BusinessId == businessId
                                      && (branchId <= 0 || t.BranchId == branchId));
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
