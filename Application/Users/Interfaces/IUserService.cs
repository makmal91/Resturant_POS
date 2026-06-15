using POSSystem.Application.Auth.DTOs;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Users.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Users.Interfaces;

public interface IUserRepository
{
    Task<PagedResultDto<UserListItemDto>> GetPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        string? sortDirection);

    Task<UserDetailDto?> GetByIdAsync(int id, int businessId);
    Task<User?> GetTrackedByIdAsync(int id, int businessId);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null);
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
    Task<bool> RoleExistsAsync(int roleId);
    Task<bool> BranchesExistAsync(int businessId, IReadOnlyList<int> branchIds);
    Task<int?> GetFirstActiveBranchIdAsync(int businessId);
    Task<IReadOnlyList<UserBranchAssignmentDto>> GetUserBranchesAsync(int userId);
    Task AddAsync(User user);
    Task SaveChangesAsync();
    Task ReplaceUserBranchesAsync(int userId, IReadOnlyList<int> branchIds);
    Task RemoveUserBranchAsync(int userId, int branchId);
}

public interface IUserService
{
    Task<PagedResultDto<UserListItemDto>> GetUsersPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        string? sortDirection,
        bool isGlobalAdmin);

    Task<UserDetailDto?> GetUserByIdAsync(int id, int businessId, int branchId, bool isGlobalAdmin);
    Task<UserDetailDto> CreateUserAsync(CreateUserDto dto, bool isGlobalAdmin, string? actorRoleName = null);
    Task<UserDetailDto?> UpdateUserAsync(int id, UpdateUserDto dto, int branchId, bool isGlobalAdmin, string? actorRoleName = null);
    Task DeleteUserAsync(int id, int businessId, int branchId, bool isGlobalAdmin, string? actorRoleName = null);
    Task<IReadOnlyList<UserBranchAssignmentDto>> GetUserBranchesAsync(int userId, int businessId);
    Task AssignUserBranchesAsync(int userId, AssignUserBranchesDto dto, int businessId, int branchId, bool isGlobalAdmin);
    Task RemoveUserBranchAsync(int userId, int branchId, int businessId, int requestBranchId, bool isGlobalAdmin);
}

public interface IRoleRepository
{
    Task<IReadOnlyList<RoleListItemDto>> GetAllAsync();
    Task<RoleDetailDto?> GetByIdAsync(int id);
    Task<Role?> GetTrackedByIdAsync(int id);
    Task<bool> NameExistsAsync(string name, int? excludeRoleId = null);
    Task AddAsync(Role role);
    Task SaveChangesAsync();
    Task<IReadOnlyList<RolePermissionDto>> GetPermissionsAsync(int roleId);
    Task ReplacePermissionsAsync(int roleId, IReadOnlyList<RolePermissionDto> permissions);
}

public interface IRoleService
{
    Task<IReadOnlyList<RoleListItemDto>> GetRolesAsync();
    Task<RoleDetailDto?> GetRoleByIdAsync(int id);
    Task<RoleDetailDto> CreateRoleAsync(CreateRoleDto dto);
    Task<RoleDetailDto?> UpdateRoleAsync(int id, UpdateRoleDto dto);
    Task DeleteRoleAsync(int id);
    Task<IReadOnlyList<RolePermissionDto>> GetRolePermissionsAsync(int roleId);
    Task UpdateRolePermissionsAsync(int roleId, UpdateRolePermissionsDto dto, string? actorRoleName = null);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}