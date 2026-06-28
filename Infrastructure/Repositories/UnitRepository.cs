using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Unit.Interfaces;
using POSSystem.Infrastructure.Data;
using MeasurementUnitEntity = POSSystem.Domain.MeasurementUnit;

namespace POSSystem.Infrastructure.Repositories;

public class UnitRepository : IUnitRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public UnitRepository(POSDbContext context)
    {
        _context = context;
    }

    public Task<List<MeasurementUnitEntity>> GetAllAsync(int businessId, int branchId, bool? status = null)
    {
        var query = BuildQuery(businessId, branchId, status, null);
        return query.OrderBy(u => u.Name).ToListAsync();
    }

    public async Task<PagedResultDto<MeasurementUnitEntity>> GetPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null,
        bool? status = null,
        string? sortBy = null,
        string? sortDirection = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = BuildQuery(businessId, branchId, status, search);
        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
            page = totalPages;

        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var orderedQuery = (sortBy ?? "name").ToLowerInvariant() switch
        {
            "code" => descending ? query.OrderByDescending(u => u.Code) : query.OrderBy(u => u.Code),
            "defaultconversionfactor" or "conversionfactor" => descending
                ? query.OrderByDescending(u => u.DefaultConversionFactor)
                : query.OrderBy(u => u.DefaultConversionFactor),
            "status" or "isactive" => descending ? query.OrderByDescending(u => u.Status) : query.OrderBy(u => u.Status),
            "branchname" => descending
                ? query.OrderByDescending(u => u.Branch!.Name).ThenByDescending(u => u.Name)
                : query.OrderBy(u => u.Branch!.Name).ThenBy(u => u.Name),
            _ => descending ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name),
        };

        var data = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<MeasurementUnitEntity>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page
        };
    }

    private IQueryable<MeasurementUnitEntity> BuildQuery(int businessId, int branchId, bool? status, string? search)
    {
        var query = _context.Units
            .IgnoreQueryFilters()
            .Include(u => u.Branch)
            .Where(u => !u.IsDeleted && u.BusinessId == businessId);

        if (branchId > 0)
            query = query.Where(u => u.BranchId == branchId);

        if (status.HasValue)
            query = query.Where(u => u.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Name.ToLower().Contains(term) ||
                u.Code.ToLower().Contains(term));
        }

        return query;
    }

    public Task<MeasurementUnitEntity?> GetByIdAsync(int id, int businessId, int branchId)
    {
        return _context.Units
            .IgnoreQueryFilters()
            .Include(u => u.Branch)
            .Where(u => u.Id == id && !u.IsDeleted && u.BusinessId == businessId && (branchId == 0 || u.BranchId == branchId))
            .FirstOrDefaultAsync();
    }

    public Task<MeasurementUnitEntity?> GetByNameAsync(string name, int businessId, int branchId, int? excludeId = null)
    {
        var normalizedName = name.Trim().ToLower();
        return _context.Units
            .IgnoreQueryFilters()
            .Where(u =>
                !u.IsDeleted &&
                u.BusinessId == businessId &&
                u.BranchId == branchId &&
                u.Name.ToLower() == normalizedName &&
                (!excludeId.HasValue || u.Id != excludeId.Value))
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(MeasurementUnitEntity unit)
    {
        await _context.Units.AddAsync(unit);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
