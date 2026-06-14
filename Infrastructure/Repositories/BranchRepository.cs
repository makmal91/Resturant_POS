using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Branch.DTOs;
using POSSystem.Application.Branch.Interfaces;
using POSSystem.Infrastructure.Data;
using BranchEntity = POSSystem.Domain.Branch;

namespace POSSystem.Infrastructure.Repositories;

public class BranchRepository : IBranchRepository
{
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
                CreatedDate = branch.CreatedDate
            };

        return await query.ToListAsync();
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
                CreatedDate = branch.CreatedDate,
                UpdatedDate = branch.UpdatedDate
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
