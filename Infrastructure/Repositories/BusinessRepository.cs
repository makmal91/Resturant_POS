using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Business.DTOs;
using POSSystem.Application.Business.Interfaces;
using POSSystem.Application.Common.DTOs;
using POSSystem.Infrastructure.Data;
using BusinessEntity = POSSystem.Domain.Business;

namespace POSSystem.Infrastructure.Repositories;

public class BusinessRepository : IBusinessRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public BusinessRepository(POSDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<BusinessListItemDto>> GetPagedAsync(int page, int pageSize, string? search, string? sortBy, string? sortDirection)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _context.Businesses
            .AsNoTracking()
            .Where(b => !b.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b =>
                b.Name.Contains(term) ||
                b.LegalName.Contains(term) ||
                b.Email.Contains(term) ||
                b.Phone.Contains(term));
        }

        var totalRecords = await query.CountAsync();
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
            page = totalPages;

        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var orderedQuery = (sortBy ?? "name").ToLowerInvariant() switch
        {
            "legalname" => descending ? query.OrderByDescending(b => b.LegalName) : query.OrderBy(b => b.LegalName),
            "email" => descending ? query.OrderByDescending(b => b.Email) : query.OrderBy(b => b.Email),
            "phone" => descending ? query.OrderByDescending(b => b.Phone) : query.OrderBy(b => b.Phone),
            "timezone" => descending ? query.OrderByDescending(b => b.TimeZone) : query.OrderBy(b => b.TimeZone),
            "isactive" => descending ? query.OrderByDescending(b => b.IsActive) : query.OrderBy(b => b.IsActive),
            _ => descending ? query.OrderByDescending(b => b.Name) : query.OrderBy(b => b.Name),
        };

        var data = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BusinessListItemDto
            {
                Id = b.Id,
                Name = b.Name,
                LegalName = b.LegalName,
                Phone = b.Phone,
                Email = b.Email,
                TimeZone = b.TimeZone,
                IsActive = b.IsActive,
                HasLogo = b.Logo != null && b.Logo.Length > 0
            })
            .ToListAsync();

        return new PagedResultDto<BusinessListItemDto>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            CurrentPage = page
        };
    }

    public async Task<BusinessDetailDto?> GetDetailByIdAsync(int id)
    {
        return await _context.Businesses
            .AsNoTracking()
            .Where(b => b.Id == id && !b.IsDeleted)
            .Select(b => new BusinessDetailDto
            {
                Id = b.Id,
                Name = b.Name,
                LegalName = b.LegalName,
                Phone = b.Phone,
                Email = b.Email,
                Address = b.Address,
                TaxNumber = b.TaxNumber,
                Currency = b.Currency,
                TimeZone = b.TimeZone,
                IsActive = b.IsActive,
                HasLogo = b.Logo != null && b.Logo.Length > 0,
                LogoFileName = b.LogoFileName,
                LogoContentType = b.LogoContentType,
                CreatedDate = b.CreatedDate,
                UpdatedDate = b.UpdatedDate
            })
            .FirstOrDefaultAsync();
    }

    public async Task<BusinessLogoDto?> GetLogoByIdAsync(int id)
    {
        return await _context.Businesses
            .AsNoTracking()
            .Where(b => b.Id == id && !b.IsDeleted && b.Logo != null)
            .Select(b => new BusinessLogoDto
            {
                Logo = b.Logo!,
                LogoFileName = b.LogoFileName,
                LogoContentType = b.LogoContentType
            })
            .FirstOrDefaultAsync();
    }

    public Task<BusinessEntity?> GetTrackedByIdAsync(int id)
    {
        return _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
    }

    public Task<BusinessEntity?> GetTrackedWithBranchesAsync(int id)
    {
        return GetTrackedWithBranchesInternalAsync(id);
    }

    private async Task<BusinessEntity?> GetTrackedWithBranchesInternalAsync(int id)
    {
        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

        if (business == null)
            return null;

        business.Branches = await _context.Branches
            .Where(b => b.BusinessId == id)
            .ToListAsync();

        return business;
    }

    public async Task AddAsync(BusinessEntity business)
    {
        await _context.Businesses.AddAsync(business);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public void Remove(BusinessEntity business)
    {
        _context.Businesses.Remove(business);
    }
}
