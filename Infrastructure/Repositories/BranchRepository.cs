using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Branch.DTOs;
using POSSystem.Application.Branch.Interfaces;
using POSSystem.Application.Common.DTOs;
using POSSystem.Infrastructure.Data;
using BranchEntity = POSSystem.Domain.Branch;

namespace POSSystem.Infrastructure.Repositories;

public class BranchRepository : IBranchRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public BranchRepository(POSDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BranchSummaryDto>> GetAllActiveSummariesAsync()
    {
        return await _context.Branches
            .AsNoTracking()
            .Where(b => b.IsActive && !b.IsDeleted)
            .OrderBy(b => b.Name)
            .Select(b => new BranchSummaryDto
            {
                Id = b.Id,
                Name = b.Name
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<BranchListItemDto>> GetByBusinessIdAsync(int businessId)
    {
        var query =
            from branch in _context.Branches.AsNoTracking()
            join business in _context.Businesses.AsNoTracking() on branch.BusinessId equals business.Id
            join country in _context.Countries.AsNoTracking() on branch.CountryId equals country.Id
            join city in _context.Cities.AsNoTracking() on branch.CityId equals city.Id
            where branch.BusinessId == businessId
            orderby branch.Name
            select new BranchListItemDto
            {
                Id = branch.Id,
                Name = branch.Name,
                Code = branch.Code,
                Address = branch.Address,
                Phone = branch.Phone,
                BusinessId = branch.BusinessId,
                BusinessName = business.Name,
                CountryId = branch.CountryId,
                CountryName = country.Name,
                CityId = branch.CityId,
                CityName = city.Name,
                IsActive = branch.IsActive,
                CreatedAt = branch.CreatedAt
            };

        return await query.ToListAsync();
    }

    public async Task<PagedResultDto<BranchListItemDto>> GetPagedAsync(
        int businessId,
        int page,
        int pageSize,
        string? search = null,
        string? sortBy = null,
        string? sortDirection = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query =
            from branch in _context.Branches.AsNoTracking()
            join business in _context.Businesses.AsNoTracking() on branch.BusinessId equals business.Id
            join country in _context.Countries.AsNoTracking() on branch.CountryId equals country.Id
            join city in _context.Cities.AsNoTracking() on branch.CityId equals city.Id
            where branch.BusinessId == businessId && !branch.IsDeleted
            select new BranchListItemDto
            {
                Id = branch.Id,
                Name = branch.Name,
                Code = branch.Code,
                Address = branch.Address,
                Phone = branch.Phone,
                BusinessId = branch.BusinessId,
                BusinessName = business.Name,
                CountryId = branch.CountryId,
                CountryName = country.Name,
                CityId = branch.CityId,
                CityName = city.Name,
                IsActive = branch.IsActive,
                CreatedAt = branch.CreatedAt
            };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(b =>
                b.Name.ToLower().Contains(term) ||
                b.Code.ToLower().Contains(term) ||
                b.Address.ToLower().Contains(term) ||
                b.Phone.Contains(term) ||
                b.BusinessName.ToLower().Contains(term) ||
                b.CountryName.ToLower().Contains(term) ||
                b.CityName.ToLower().Contains(term));
        }

        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
            page = totalPages;

        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var orderedQuery = (sortBy ?? "name").ToLowerInvariant() switch
        {
            "code" => descending ? query.OrderByDescending(b => b.Code) : query.OrderBy(b => b.Code),
            "address" => descending ? query.OrderByDescending(b => b.Address) : query.OrderBy(b => b.Address),
            "phone" => descending ? query.OrderByDescending(b => b.Phone) : query.OrderBy(b => b.Phone),
            "businessname" => descending ? query.OrderByDescending(b => b.BusinessName) : query.OrderBy(b => b.BusinessName),
            "countryname" => descending ? query.OrderByDescending(b => b.CountryName) : query.OrderBy(b => b.CountryName),
            "cityname" => descending ? query.OrderByDescending(b => b.CityName) : query.OrderBy(b => b.CityName),
            "status" or "isactive" => descending ? query.OrderByDescending(b => b.IsActive) : query.OrderBy(b => b.IsActive),
            "createdat" => descending ? query.OrderByDescending(b => b.CreatedAt) : query.OrderBy(b => b.CreatedAt),
            _ => descending ? query.OrderByDescending(b => b.Name) : query.OrderBy(b => b.Name),
        };

        var data = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<BranchListItemDto>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page
        };
    }

    public async Task<BranchDetailDto?> GetDetailByIdAsync(int id, int businessId)
    {
        var query =
            from branch in _context.Branches.AsNoTracking()
            join business in _context.Businesses.AsNoTracking() on branch.BusinessId equals business.Id
            join country in _context.Countries.AsNoTracking() on branch.CountryId equals country.Id
            join city in _context.Cities.AsNoTracking() on branch.CityId equals city.Id
            where branch.Id == id && branch.BusinessId == businessId
            select new BranchDetailDto
            {
                Id = branch.Id,
                Name = branch.Name,
                Code = branch.Code,
                Address = branch.Address,
                Phone = branch.Phone,
                Email = branch.Email,
                BusinessId = branch.BusinessId,
                BusinessName = business.Name,
                CountryId = branch.CountryId,
                CountryName = country.Name,
                CityId = branch.CityId,
                CityName = city.Name,
                IsActive = branch.IsActive,
                CreatedAt = branch.CreatedAt,
                ModifiedAt = branch.ModifiedAt
            };

        return await query.FirstOrDefaultAsync();
    }

    public Task<BranchEntity?> GetTrackedByIdAsync(int id, int businessId)
    {
        return _context.Branches
            .FirstOrDefaultAsync(b => b.Id == id && b.BusinessId == businessId);
    }

    public Task<bool> BusinessExistsAsync(int businessId)
    {
        return _context.Businesses.AnyAsync(b => b.Id == businessId && !b.IsDeleted);
    }

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _context.Branches.Where(b => b.Code == code);
        if (excludeId.HasValue)
            query = query.Where(b => b.Id != excludeId.Value);

        return query.AnyAsync();
    }

    public Task<bool> CountryExistsAsync(int countryId)
    {
        return _context.Countries.AnyAsync(c => c.Id == countryId && c.IsActive);
    }

    public Task<bool> CityBelongsToCountryAsync(int cityId, int countryId)
    {
        return _context.Cities.AnyAsync(c => c.Id == cityId && c.CountryId == countryId && c.IsActive);
    }

    public async Task<IReadOnlyList<CountryListItemDto>> GetCountriesAsync()
    {
        return await _context.Countries
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CountryListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<CityListItemDto>> GetCitiesByCountryIdAsync(int countryId)
    {
        return await _context.Cities
            .AsNoTracking()
            .Where(c => c.CountryId == countryId && c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CityListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                CountryId = c.CountryId
            })
            .ToListAsync();
    }

    public Task<string?> GetCityNameByIdAsync(int cityId)
    {
        return _context.Cities
            .AsNoTracking()
            .Where(c => c.Id == cityId && c.IsActive)
            .Select(c => c.Name)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(BranchEntity branch)
    {
        await _context.Branches.AddAsync(branch);
    }

    public Task DeleteAsync(BranchEntity branch)
    {
        _context.Branches.Remove(branch);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
