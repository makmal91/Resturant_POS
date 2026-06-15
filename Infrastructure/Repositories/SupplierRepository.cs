using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Supplier.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public SupplierRepository(POSDbContext context) => _context = context;

    public async Task<PagedResultDto<Supplier>> GetPagedAsync(
        int businessId, int branchId, int page, int pageSize, string? search = null, bool? isActive = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _context.Suppliers
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.BusinessId == businessId);

        if (branchId > 0) query = query.Where(s => s.BranchId == branchId);
        if (isActive.HasValue) query = query.Where(s => s.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term) || s.Phone.Contains(term) || s.Email.ToLower().Contains(term));
        }

        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);
        if (totalPages > 0 && page > totalPages) page = totalPages;

        var data = await query
            .Include(s => s.Branch)
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<Supplier> { Data = data, TotalRecords = totalRecords, TotalPages = totalPages, CurrentPage = page };
    }

    public async Task<Supplier?> GetByIdAsync(int id, int businessId, int branchId)
    {
        var query = _context.Suppliers
            .IgnoreQueryFilters()
            .Include(s => s.Branch)
            .Where(s => s.Id == id && !s.IsDeleted && s.BusinessId == businessId);

        if (branchId > 0) query = query.Where(s => s.BranchId == branchId);
        return await query.FirstOrDefaultAsync();
    }

    public async Task<Supplier?> GetByNameAsync(string name, int businessId, int branchId, int? excludeId = null)
    {
        var normalized = name.Trim().ToLower();
        return await _context.Suppliers
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.BusinessId == businessId && s.BranchId == branchId
                && s.Name.ToLower() == normalized
                && (!excludeId.HasValue || s.Id != excludeId.Value))
            .FirstOrDefaultAsync();
    }

    public async Task<List<Supplier>> GetAllActiveAsync(int businessId, int branchId)
    {
        var query = _context.Suppliers
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.BusinessId == businessId && s.IsActive);

        if (branchId > 0) query = query.Where(s => s.BranchId == branchId);
        return await query.Include(s => s.Branch).OrderBy(s => s.Name).ToListAsync();
    }

    public async Task AddAsync(Supplier supplier) => await _context.Suppliers.AddAsync(supplier);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
