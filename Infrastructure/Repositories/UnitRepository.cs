using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Unit.Interfaces;
using POSSystem.Infrastructure.Data;
using MeasurementUnitEntity = POSSystem.Domain.MeasurementUnit;

namespace POSSystem.Infrastructure.Repositories;

public class UnitRepository : IUnitRepository
{
    private readonly POSDbContext _context;

    public UnitRepository(POSDbContext context)
    {
        _context = context;
    }

    public Task<List<MeasurementUnitEntity>> GetAllAsync(int businessId, int branchId, bool? status = null)
    {
        var query = _context.Units
            .IgnoreQueryFilters()
            .Include(u => u.Branch)
            .Where(u => !u.IsDeleted && u.BusinessId == businessId);

        if (branchId > 0)
            query = query.Where(u => u.BranchId == branchId);

        if (status.HasValue)
            query = query.Where(u => u.Status == status.Value);

        return query.OrderBy(u => u.Name).ToListAsync();
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
