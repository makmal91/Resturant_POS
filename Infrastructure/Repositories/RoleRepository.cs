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
                IsActive = r.IsActive,
                UserCount = r.Users.Count(u => !u.IsDeleted)
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
            .Where(rp => rp.RoleId == roleId && !rp.IsDeleted)
            .OrderBy(rp => rp.ModuleName)
            .Select(rp => new RolePermissionDto
            {
                Id = rp.Id,
                ModuleId = rp.ModuleId,
                ModuleName = rp.ModuleName,
                CanView = rp.CanView,
                CanCreate = rp.CanCreate,
                CanEdit = rp.CanEdit,
                CanDelete = rp.CanDelete,
                CanExport = rp.CanExport,
                CanUpload = rp.CanUpload
            })
            .ToListAsync();

        _cache.Set(cacheKey, permissions, TimeSpan.FromMinutes(10));
        return permissions;
    }

    public async Task ReplacePermissionsAsync(int roleId, IReadOnlyList<RolePermissionDto> permissions)
    {
        var existing = await _context.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        ConsolidateDuplicateModuleRows(existing);

        var incomingKeys = permissions
            .Select(p => p.ModuleName.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var incomingModuleIds = permissions
            .Where(p => p.ModuleId.HasValue)
            .Select(p => p.ModuleId!.Value)
            .ToHashSet();

        foreach (var row in existing.Where(r =>
                     !r.IsDeleted &&
                     r.ModuleId.HasValue &&
                     !incomingModuleIds.Contains(r.ModuleId.Value) &&
                     !incomingKeys.Contains(r.ModuleName)))
        {
            row.IsDeleted = true;
            row.UpdatedDate = DateTime.UtcNow;
        }

        foreach (var row in existing.Where(r =>
                     !r.IsDeleted &&
                     !r.ModuleId.HasValue &&
                     !incomingKeys.Contains(r.ModuleName)))
        {
            row.IsDeleted = true;
            row.UpdatedDate = DateTime.UtcNow;
        }

        foreach (var permission in permissions)
        {
            var moduleName = permission.ModuleName.Trim();
            var row = FindMatchingPermissionRow(existing, permission.ModuleId, moduleName);

            if (row == null)
            {
                await _context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = roleId,
                    ModuleId = permission.ModuleId,
                    ModuleName = moduleName,
                    CanView = permission.CanView,
                    CanCreate = permission.CanCreate,
                    CanEdit = permission.CanEdit,
                    CanDelete = permission.CanDelete,
                    CanExport = permission.CanExport,
                    CanUpload = permission.CanUpload,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow
                });
                continue;
            }

            row.ModuleId = permission.ModuleId;
            row.ModuleName = moduleName;
            row.CanView = permission.CanView;
            row.CanCreate = permission.CanCreate;
            row.CanEdit = permission.CanEdit;
            row.CanDelete = permission.CanDelete;
            row.CanExport = permission.CanExport;
            row.CanUpload = permission.CanUpload;
            row.IsDeleted = false;
            row.UpdatedDate = DateTime.UtcNow;
        }

        _cache.Remove(GetPermissionCacheKey(roleId));
    }

    private static void ConsolidateDuplicateModuleRows(List<RolePermission> existing)
    {
        var activeRows = existing.Where(r => !r.IsDeleted).ToList();

        foreach (var group in activeRows.GroupBy(r => ResolveModuleGroupKey(r)))
        {
            var keeper = group
                .OrderByDescending(r => r.ModuleId.HasValue)
                .ThenByDescending(r => r.UpdatedDate ?? r.CreatedDate)
                .First();

            foreach (var duplicate in group.Where(r => r.Id != keeper.Id))
            {
                duplicate.IsDeleted = true;
                duplicate.UpdatedDate = DateTime.UtcNow;
            }
        }
    }

    private static string ResolveModuleGroupKey(RolePermission row)
    {
        if (row.ModuleId.HasValue)
            return $"id:{row.ModuleId.Value}";

        return $"name:{PermissionModuleResolver.Normalize(row.ModuleName)}";
    }

    private static RolePermission? FindMatchingPermissionRow(
        IReadOnlyList<RolePermission> existing,
        int? moduleId,
        string moduleName)
    {
        var matches = existing
            .Where(r => MatchesModulePermission(r, moduleId, moduleName))
            .OrderByDescending(r => !r.IsDeleted)
            .ThenByDescending(r => r.ModuleId.HasValue)
            .ThenByDescending(r => r.UpdatedDate ?? r.CreatedDate)
            .ToList();

        if (matches.Count == 0)
            return null;

        var keeper = matches[0];
        foreach (var duplicate in matches.Skip(1))
        {
            duplicate.IsDeleted = true;
            duplicate.UpdatedDate = DateTime.UtcNow;
        }

        return keeper;
    }

    private static bool MatchesModulePermission(RolePermission row, int? moduleId, string moduleName)
    {
        if (moduleId.HasValue && row.ModuleId == moduleId)
            return true;

        return PermissionModuleResolver.Matches(row.ModuleName, moduleName);
    }

    private const string CacheGenerationKey = "perm-cache-gen";

    private long GetCacheGeneration() =>
        _cache.GetOrCreate(CacheGenerationKey, _ => 0L);

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
                ModuleId = rp.ModuleId,
                ModuleName = rp.ModuleName,
                CanView = rp.CanView,
                CanCreate = rp.CanCreate,
                CanEdit = rp.CanEdit,
                CanDelete = rp.CanDelete,
                CanExport = rp.CanExport,
                CanUpload = rp.CanUpload
            })
            .ToList()
    };

    private string GetPermissionCacheKey(int roleId) => $"role-permissions:{roleId}:{GetCacheGeneration()}";

    private void InvalidatePermissionCache()
    {
        var current = _cache.Get<long>(CacheGenerationKey);
        _cache.Set(CacheGenerationKey, current + 1);
    }
}
