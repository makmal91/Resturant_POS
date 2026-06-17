using Microsoft.EntityFrameworkCore;
using POSSystem.Application.License.DTOs;
using POSSystem.Application.License.Interfaces;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.License;

public sealed class LicenseUsageProvider : ILicenseUsageProvider
{
    private readonly POSDbContext _context;
    private readonly ILicenseService _licenseService;

    public LicenseUsageProvider(POSDbContext context, ILicenseService licenseService)
    {
        _context = context;
        _licenseService = licenseService;
    }

    public Task<int> GetBusinessCountAsync(CancellationToken cancellationToken = default)
    {
        return _context.Businesses
            .AsNoTracking()
            .CountAsync(b => !b.IsDeleted, cancellationToken);
    }

    public Task<int> GetBranchCountAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return _context.Branches
            .AsNoTracking()
            .CountAsync(b => !b.IsDeleted && b.BusinessId == businessId, cancellationToken);
    }

    public Task<int> GetTotalUserCountAsync(CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .CountAsync(cancellationToken);
    }

    public async Task<LicenseUsageDto> GetUsageSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var payload = _licenseService.GetActivePayload();
        var maxBusinesses = payload?.MaxBusinesses ?? 0;
        var maxBranches = payload?.MaxBranchesPerBusiness ?? 0;
        var maxUsers = payload?.MaxUsers ?? 0;

        var currentBusinesses = await GetBusinessCountAsync(cancellationToken);
        var totalUsers = await GetTotalUserCountAsync(cancellationToken);

        var branchUsage = await (
            from business in _context.Businesses.AsNoTracking()
            where !business.IsDeleted
            let branchCount = _context.Branches.Count(b => !b.IsDeleted && b.BusinessId == business.Id)
            orderby business.Name
            select new LicenseBranchUsageDto
            {
                BusinessId = business.Id,
                BusinessName = business.Name,
                CurrentBranches = branchCount,
                MaxBranchesPerBusiness = maxBranches
            }).ToListAsync(cancellationToken);

        return new LicenseUsageDto
        {
            CurrentBusinesses = currentBusinesses,
            MaxBusinesses = maxBusinesses,
            TotalUsers = totalUsers,
            MaxUsers = maxUsers,
            BranchUsageByBusiness = branchUsage
        };
    }
}
