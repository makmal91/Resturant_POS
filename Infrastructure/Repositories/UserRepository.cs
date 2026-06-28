using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Users.DTOs;
using POSSystem.Application.Users.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private const int MaxPageSize = 100;
    private readonly POSDbContext _context;

    public UserRepository(POSDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<UserListItemDto>> GetPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        string? sortDirection)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.UserBranches)
                .ThenInclude(ub => ub.Branch)
            .Where(u => u.BusinessId == businessId);

        if (branchId > 0)
        {
            query = query.Where(u => u.UserBranches.Any(ub => ub.BranchId == branchId));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                u.Username.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                u.Phone.ToLower().Contains(term) ||
                u.Role.Name.ToLower().Contains(term));
        }

        query = ApplySorting(query, sortBy, sortDirection);

        var totalRecords = await query.CountAsync();
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<UserListItemDto>
        {
            Data = users.Select(MapListItem).ToList(),
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
            CurrentPage = page
        };
    }

    public async Task<UserDetailDto?> GetByIdAsync(int id, int businessId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.UserBranches)
                .ThenInclude(ub => ub.Branch)
            .FirstOrDefaultAsync(u => u.Id == id && u.BusinessId == businessId);

        return user == null ? null : MapDetail(user);
    }

    public async Task<User?> GetTrackedByIdAsync(int id, int businessId)
    {
        return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.UserBranches)
            .FirstOrDefaultAsync(u => u.Id == id && u.BusinessId == businessId);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var normalized = username.Trim().ToLower();
        return await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Role)
            .Include(u => u.UserBranches)
                .ThenInclude(ub => ub.Branch)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized && !u.IsDeleted);
    }

    public async Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null)
    {
        var normalized = username.Trim().ToLower();
        return await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u =>
                u.Username.ToLower() == normalized &&
                !u.IsDeleted &&
                (!excludeUserId.HasValue || u.Id != excludeUserId.Value));
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
    {
        var normalized = email.Trim().ToLower();
        return await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u =>
                u.Email.ToLower() == normalized &&
                !u.IsDeleted &&
                (!excludeUserId.HasValue || u.Id != excludeUserId.Value));
    }

    public Task<bool> RoleExistsAsync(int roleId) =>
        _context.Roles.AnyAsync(r => r.Id == roleId && r.IsActive);

    public async Task<bool> BranchesExistAsync(int businessId, IReadOnlyList<int> branchIds)
    {
        if (branchIds.Count == 0)
            return false;

        var distinctIds = branchIds.Distinct().ToList();
        var count = await _context.Branches.CountAsync(b => b.BusinessId == businessId && distinctIds.Contains(b.Id));
        return count == distinctIds.Count;
    }

    public async Task<int?> GetFirstActiveBranchIdAsync(int businessId) =>
        await _context.Branches
            .AsNoTracking()
            .Where(b => b.BusinessId == businessId && b.IsActive && !b.IsDeleted)
            .OrderBy(b => b.Id)
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyList<UserBranchAssignmentDto>> GetUserBranchesAsync(int userId)
    {
        return await _context.UserBranches
            .AsNoTracking()
            .Include(ub => ub.Branch)
            .Where(ub => ub.UserId == userId)
            .Select(ub => new UserBranchAssignmentDto
            {
                BranchId = ub.BranchId,
                BranchName = ub.Branch.Name
            })
            .ToListAsync();
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    public async Task ReplaceUserBranchesAsync(int userId, IReadOnlyList<int> branchIds)
    {
        var incomingIds = branchIds.Distinct().ToHashSet();
        var existing = await _context.UserBranches
            .Where(ub => ub.UserId == userId)
            .ToListAsync();

        var existingIds = existing.Select(ub => ub.BranchId).ToHashSet();

        foreach (var row in existing.Where(ub => !incomingIds.Contains(ub.BranchId)))
            _context.UserBranches.Remove(row);

        foreach (var branchId in incomingIds.Where(id => !existingIds.Contains(id)))
        {
            await _context.UserBranches.AddAsync(new UserBranch
            {
                UserId = userId,
                BranchId = branchId
            });
        }
    }

    public async Task RemoveUserBranchAsync(int userId, int branchId)
    {
        var mapping = await _context.UserBranches
            .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BranchId == branchId);

        if (mapping != null)
            _context.UserBranches.Remove(mapping);
    }

    private static IQueryable<User> ApplySorting(IQueryable<User> query, string? sortBy, string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var column = sortBy?.Trim().ToLowerInvariant() ?? "fullname";

        return column switch
        {
            "username" => descending ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
            "email" => descending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "role" or "rolename" => descending ? query.OrderByDescending(u => u.Role.Name) : query.OrderBy(u => u.Role.Name),
            "status" or "isactive" => descending ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive),
            _ => descending ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName)
        };
    }

    private static UserListItemDto MapListItem(User user)
    {
        var branches = user.UserBranches
            .Select(ub => new UserBranchAssignmentDto
            {
                BranchId = ub.BranchId,
                BranchName = ub.Branch?.Name ?? string.Empty
            })
            .OrderBy(b => b.BranchName)
            .ToList();

        return new UserListItemDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            RoleId = user.RoleId,
            RoleName = user.Role?.Name ?? string.Empty,
            IsActive = user.IsActive,
            Branches = branches,
            AssignedBranchesDisplay = string.Join(", ", branches.Select(b => b.BranchName)),
            PrimaryBranchId = branches.FirstOrDefault()?.BranchId ?? user.BranchId,
            PrimaryBranchName = branches.FirstOrDefault()?.BranchName
                ?? (user.BranchId > 0 ? user.BranchId.ToString() : string.Empty)
        };
    }

    private static UserDetailDto MapDetail(User user)
    {
        var listItem = MapListItem(user);
        return new UserDetailDto
        {
            Id = listItem.Id,
            FullName = listItem.FullName,
            Username = listItem.Username,
            Email = listItem.Email,
            Phone = listItem.Phone,
            RoleId = listItem.RoleId,
            RoleName = listItem.RoleName,
            IsActive = listItem.IsActive,
            Branches = listItem.Branches,
            AssignedBranchesDisplay = listItem.AssignedBranchesDisplay,
            PrimaryBranchId = listItem.PrimaryBranchId,
            PrimaryBranchName = listItem.PrimaryBranchName,
            BusinessId = user.BusinessId
        };
    }
}
