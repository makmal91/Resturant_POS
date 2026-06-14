using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Users.DTOs;
using POSSystem.Application.Users.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly POSDbContext _context;
    private readonly IMemoryCache _cache;

    public RoleRepository(POSDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<IReadOnlyList<RoleListItemDto>> GetAllAsync()
    {
        return await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleListItemDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsActive = r.IsActive
            })
            .ToListAsync();
    }

    public async Task<RoleDetailDto?> GetByIdAsync(int id)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        return role == null ? null : MapDetail(role);
    }

    public async Task<Role?> GetTrackedByIdAsync(int id)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeRoleId = null)
    {
        var normalized = name.Trim().ToLower();
        return await _context.Roles
            .IgnoreQueryFilters()
            .AnyAsync(r =>
                r.Name.ToLower() == normalized &&
                !r.IsDeleted &&
                (!excludeRoleId.HasValue || r.Id != excludeRoleId.Value));
    }

    public async Task AddAsync(Role role)
    {
        await _context.Roles.AddAsync(role);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
        InvalidatePermissionCache();
    }

    public async Task<IReadOnlyList<RolePermissionDto>> GetPermissionsAsync(int roleId)
    {
        var cacheKey = GetPermissionCacheKey(roleId);
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<RolePermissionDto>? cached) && cached != null)
            return cached;

        var permissions = await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .OrderBy(rp => rp.ModuleName)
            .Select(rp => new RolePermissionDto
            {
                Id = rp.Id,
                ModuleName = rp.ModuleName,
                CanView = rp.CanView,
                CanCreate = rp.CanCreate,
                CanEdit = rp.CanEdit,
                CanDelete = rp.CanDelete
            })
            .ToListAsync();

        _cache.Set(cacheKey, permissions, TimeSpan.FromMinutes(10));
        return permissions;
    }

    public async Task ReplacePermissionsAsync(int roleId, IReadOnlyList<RolePermissionDto> permissions)
    {
        var existing = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        _context.RolePermissions.RemoveRange(existing);

        foreach (var permission in permissions)
        {
            await _context.RolePermissions.AddAsync(new RolePermission
            {
                RoleId = roleId,
                ModuleName = permission.ModuleName.Trim(),
                CanView = permission.CanView,
                CanCreate = permission.CanCreate,
                CanEdit = permission.CanEdit,
                CanDelete = permission.CanDelete
            });
        }

        _cache.Remove(GetPermissionCacheKey(roleId));
    }

    private static RoleDetailDto MapDetail(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        IsActive = role.IsActive,
        Permissions = role.RolePermissions
            .OrderBy(rp => rp.ModuleName)
            .Select(rp => new RolePermissionDto
            {
                Id = rp.Id,
                ModuleName = rp.ModuleName,
                CanView = rp.CanView,
                CanCreate = rp.CanCreate,
                CanEdit = rp.CanEdit,
                CanDelete = rp.CanDelete
            })
            .ToList()
    };

    private static string GetPermissionCacheKey(int roleId) => $"role-permissions:{roleId}";

    private void InvalidatePermissionCache()
    {
    }
}
