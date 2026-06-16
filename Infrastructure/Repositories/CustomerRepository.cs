using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Customer.DTOs;
using POSSystem.Application.Customer.Interfaces;
using POSSystem.Infrastructure.Data;
using CustomerEntity = POSSystem.Domain.Customer;

namespace POSSystem.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _db;

    public CustomerRepository(POSDbContext db) => _db = db;

    public async Task<PagedResultDto<CustomerEntity>> GetPagedAsync(CustomerFilterDto filter)
    {
        var page     = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

        // BranchId == 0 means "All Branches" — skip branch filter, show entire business
        var query = filter.BranchId > 0
            ? _db.Customers.Where(c => c.BusinessId == filter.BusinessId && c.BranchId == filter.BranchId)
            : _db.Customers.Where(c => c.BusinessId == filter.BusinessId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var q = filter.Search.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(q) ||
                (c.Phone != null && c.Phone.Contains(q)) ||
                (c.Email != null && c.Email.ToLower().Contains(q)) ||
                c.CustomerCode.ToLower().Contains(q));
        }

        if (filter.Type.HasValue)
            query = query.Where(c => c.CustomerType == filter.Type.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(c => c.Status == filter.IsActive.Value);

        var total      = await query.CountAsync();
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        if (totalPages > 0 && page > totalPages) page = totalPages;

        var data = await query
            .OrderBy(c => c.IsWalkIn ? 0 : 1)
            .ThenBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<CustomerEntity>
        { Data = data, TotalRecords = total, TotalPages = totalPages, CurrentPage = page };
    }

    public Task<CustomerEntity?> GetByIdAsync(int id, int businessId, int branchId) =>
        _db.Customers.FirstOrDefaultAsync(c =>
            c.Id == id && c.BusinessId == businessId && (branchId == 0 || c.BranchId == branchId));

    public Task<CustomerEntity?> GetWalkInAsync(int businessId, int branchId) =>
        _db.Customers.FirstOrDefaultAsync(c =>
            c.IsWalkIn && c.BusinessId == businessId &&
            (branchId == 0 || c.BranchId == branchId) && !c.IsDeleted);

    public async Task<List<CustomerEntity>> SearchAsync(string query, int businessId, int branchId, int take = 10)
    {
        var q = query.ToLower();
        return await _db.Customers
            .Where(c => c.BusinessId == businessId && (branchId == 0 || c.BranchId == branchId)
                && c.Status && !c.IsDeleted
                && (c.Name.ToLower().Contains(q) || (c.Phone != null && c.Phone.Contains(q))))
            .OrderBy(c => c.Name)
            .Take(take)
            .ToListAsync();
    }

    public Task<bool> PhoneExistsAsync(string phone, int businessId, int branchId, int? excludeId = null) =>
        _db.Customers.AnyAsync(c =>
            c.Phone == phone &&
            c.BusinessId == businessId &&
            (branchId == 0 || c.BranchId == branchId) &&
            !c.IsDeleted &&
            (excludeId == null || c.Id != excludeId));

    public async Task<int> GetNextCodeSequenceAsync(int businessId, int branchId)
    {
        var count = await _db.Customers
            .IgnoreQueryFilters()
            .CountAsync(c => c.BusinessId == businessId && (branchId == 0 || c.BranchId == branchId));
        return count + 1;
    }

    public async Task AddAsync(CustomerEntity customer) =>
        await _db.Customers.AddAsync(customer);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
