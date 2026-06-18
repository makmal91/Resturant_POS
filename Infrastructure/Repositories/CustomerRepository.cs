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
        var query = _db.Customers
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted && c.BusinessId == filter.BusinessId);

        if (filter.BranchId > 0)
            query = query.Where(c => c.BranchId == filter.BranchId);

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

        if (filter.BranchId > 0 && page == 1)
            data = await EnsureWalkInOnFirstPageAsync(data, filter.BusinessId, filter.BranchId, filter.Search, pageSize);

        return new PagedResultDto<CustomerEntity>
        { Data = data, TotalRecords = total, TotalPages = totalPages, CurrentPage = page };
    }

    public Task<CustomerEntity?> GetByIdAsync(int id, int businessId, int branchId) =>
        _db.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                !c.IsDeleted &&
                c.BusinessId == businessId &&
                (branchId == 0 || c.BranchId == branchId));

    public Task<CustomerEntity?> GetWalkInAsync(int businessId, int branchId) =>
        _db.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c =>
                c.IsWalkIn &&
                !c.IsDeleted &&
                c.BusinessId == businessId &&
                (branchId == 0 || c.BranchId == branchId));

    public async Task<List<CustomerEntity>> SearchAsync(string query, int businessId, int branchId, int take = 10)
    {
        var q = query.ToLower();
        var results = await _db.Customers
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted
                && c.BusinessId == businessId
                && (branchId == 0 || c.BranchId == branchId)
                && c.Status
                && (c.Name.ToLower().Contains(q)
                    || (c.Phone != null && c.Phone.Contains(q))
                    || c.CustomerCode.ToLower().Contains(q)))
            .OrderBy(c => c.IsWalkIn ? 0 : 1)
            .ThenBy(c => c.Name)
            .Take(take)
            .ToListAsync();

        if (branchId > 0 && results.All(c => !c.IsWalkIn))
        {
            var walkIn = await GetWalkInAsync(businessId, branchId);
            if (walkIn != null && MatchesSearch(walkIn, query))
            {
                results.Insert(0, walkIn);
                if (results.Count > take)
                    results = results.Take(take).ToList();
            }
        }

        return results;
    }

    public Task<bool> PhoneExistsAsync(string phone, int businessId, int branchId, int? excludeId = null) =>
        _db.Customers
            .IgnoreQueryFilters()
            .AnyAsync(c =>
                c.Phone == phone &&
                c.BusinessId == businessId &&
                (branchId == 0 || c.BranchId == branchId) &&
                !c.IsDeleted &&
                (excludeId == null || c.Id != excludeId));

    public Task<bool> CustomerCodeExistsAsync(string customerCode, int businessId, int branchId, int? excludeId = null)
    {
        var normalized = customerCode.Trim().ToLower();
        return _db.Customers
            .IgnoreQueryFilters()
            .AnyAsync(c =>
                !c.IsDeleted &&
                c.BusinessId == businessId &&
                c.BranchId == branchId &&
                c.CustomerCode.ToLower() == normalized &&
                (excludeId == null || c.Id != excludeId));
    }

    public async Task AddAsync(CustomerEntity customer) =>
        await _db.Customers.AddAsync(customer);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    private async Task<List<CustomerEntity>> EnsureWalkInOnFirstPageAsync(
        List<CustomerEntity> data,
        int businessId,
        int branchId,
        string? search,
        int pageSize)
    {
        if (data.Any(c => c.IsWalkIn))
            return data;

        var walkIn = await GetWalkInAsync(businessId, branchId);
        if (walkIn == null || !MatchesSearch(walkIn, search))
            return data;

        data.Insert(0, walkIn);
        return data.Count > pageSize ? data.Take(pageSize).ToList() : data;
    }

    private static bool MatchesSearch(CustomerEntity customer, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        var term = search.Trim().ToLower();
        return customer.Name.ToLower().Contains(term)
               || customer.CustomerCode.ToLower().Contains(term)
               || (customer.Phone != null && customer.Phone.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
