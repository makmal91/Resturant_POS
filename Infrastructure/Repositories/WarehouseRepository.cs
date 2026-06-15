using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Warehouse.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public WarehouseRepository(POSDbContext context) => _context = context;

    public async Task<PagedResultDto<Warehouse>> GetPagedAsync(
        int businessId, int branchId, int page, int pageSize, string? search = null, bool? isActive = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _context.Warehouses
            .IgnoreQueryFilters()
            .Where(w => !w.IsDeleted && w.BusinessId == businessId);

        if (branchId > 0) query = query.Where(w => w.BranchId == branchId);
        if (isActive.HasValue) query = query.Where(w => w.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(term) || w.Code.ToLower().Contains(term));
        }

        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);
        if (totalPages > 0 && page > totalPages) page = totalPages;

        var data = await query
            .Include(w => w.Branch)
            .OrderBy(w => w.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<Warehouse> { Data = data, TotalRecords = totalRecords, TotalPages = totalPages, CurrentPage = page };
    }

    public async Task<Warehouse?> GetByIdAsync(int id, int businessId, int branchId)
    {
        var query = _context.Warehouses
            .IgnoreQueryFilters()
            .Include(w => w.Branch)
            .Where(w => w.Id == id && !w.IsDeleted && w.BusinessId == businessId);

        if (branchId > 0) query = query.Where(w => w.BranchId == branchId);
        return await query.FirstOrDefaultAsync();
    }

    public async Task<Warehouse?> GetByNameAsync(string name, int businessId, int branchId, int? excludeId = null)
    {
        var normalized = name.Trim().ToLower();
        return await _context.Warehouses
            .IgnoreQueryFilters()
            .Where(w => !w.IsDeleted && w.BusinessId == businessId && w.BranchId == branchId
                && w.Name.ToLower() == normalized
                && (!excludeId.HasValue || w.Id != excludeId.Value))
            .FirstOrDefaultAsync();
    }

    public async Task<List<Warehouse>> GetAllActiveAsync(int businessId, int branchId)
    {
        var query = _context.Warehouses
            .IgnoreQueryFilters()
            .Where(w => !w.IsDeleted && w.BusinessId == businessId && w.IsActive);

        if (branchId > 0) query = query.Where(w => w.BranchId == branchId);
        return await query.Include(w => w.Branch).OrderBy(w => w.Name).ToListAsync();
    }

    public async Task AddAsync(Warehouse warehouse) => await _context.Warehouses.AddAsync(warehouse);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
