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

    public async Task<IReadOnlyList<BranchListItemDto>> GetByBusinessIdAsync(int businessId)
    {
        return await _context.Branches
            .AsNoTracking()
            .Where(b => b.BusinessId == businessId)
            .OrderBy(b => b.Name)
            .Select(b => new BranchListItemDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                Address = b.Address,
                Phone = b.Phone,
                BusinessId = b.BusinessId,
                BusinessName = b.Business.Name,
                CountryId = b.CountryId,
                CountryName = b.Country.Name,
                CityId = b.CityId,
                CityName = b.City.Name,
                IsActive = b.IsActive,
                CreatedDate = b.CreatedDate
            })
            .ToListAsync();
    }

    public async Task<BranchDetailDto?> GetDetailByIdAsync(int id, int businessId)
    {
        return await _context.Branches
            .AsNoTracking()
            .Where(b => b.Id == id && b.BusinessId == businessId)
            .Select(b => new BranchDetailDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                Address = b.Address,
                Phone = b.Phone,
                Email = b.Email,
                BusinessId = b.BusinessId,
                BusinessName = b.Business.Name,
                CountryId = b.CountryId,
                CountryName = b.Country.Name,
                CityId = b.CityId,
                CityName = b.City.Name,
                IsActive = b.IsActive,
                CreatedDate = b.CreatedDate,
                UpdatedDate = b.UpdatedDate
            })
            .FirstOrDefaultAsync();
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
