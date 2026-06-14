using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Users.DTOs;
using POSSystem.Application.Users.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Users.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository repository, IRoleRepository roleRepository, IPasswordHasher passwordHasher)
    {
        _repository = repository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
    }

    public Task<PagedResultDto<UserListItemDto>> GetUsersPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        string? sortDirection,
        bool isGlobalAdmin) =>
        _repository.GetPagedAsync(businessId, branchId, page, pageSize, search, sortBy, sortDirection);

    public async Task<UserDetailDto?> GetUserByIdAsync(int id, int businessId, int branchId, bool isGlobalAdmin)
    {
        var user = await _repository.GetByIdAsync(id, businessId);
        if (user == null)
            return null;

        if (branchId > 0 && !isGlobalAdmin && !user.Branches.Any(b => b.BranchId == branchId))
            throw new InvalidOperationException("You do not have access to this user in the selected branch.");

        return user;
    }

    public async Task<UserDetailDto> CreateUserAsync(CreateUserDto dto, bool isGlobalAdmin)
    {
        ValidateUserInput(dto.FullName, dto.Username, dto.Email, dto.Password, isEdit: false);

        if (!await _repository.RoleExistsAsync(dto.RoleId))
            throw new InvalidOperationException("Selected role is invalid.");

        var role = await GetRoleNameAsync(dto.RoleId);
        await ValidateBranchAssignmentAsync(dto.BusinessId, dto.BranchIds, role, isGlobalAdmin);

        if (await _repository.UsernameExistsAsync(dto.Username))
            throw new InvalidOperationException("Username is already taken.");

        if (await _repository.EmailExistsAsync(dto.Email))
            throw new InvalidOperationException("Email is already registered.");

        var primaryBranchId = dto.BranchIds.FirstOrDefault();
        if (primaryBranchId <= 0 && !RoleNames.BypassesBranchRequirement(role))
            throw new InvalidOperationException("At least one branch must be assigned.");

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Username = dto.Username.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone?.Trim() ?? string.Empty,
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            RoleId = dto.RoleId,
            IsActive = dto.IsActive,
            Status = dto.IsActive ? UserStatus.Active : UserStatus.Inactive,
            BusinessId = dto.BusinessId
        };

        await _repository.AddAsync(user);
        await _repository.SaveChangesAsync();

        if (dto.BranchIds.Count > 0)
        {
            await _repository.ReplaceUserBranchesAsync(user.Id, dto.BranchIds);
            await _repository.SaveChangesAsync();
        }

        return (await _repository.GetByIdAsync(user.Id, dto.BusinessId))!;
    }

    public async Task<UserDetailDto?> UpdateUserAsync(int id, UpdateUserDto dto, int branchId, bool isGlobalAdmin)
    {
        ValidateUserInput(dto.FullName, dto.Username, dto.Email, dto.Password, isEdit: true);

        var user = await _repository.GetTrackedByIdAsync(id, dto.BusinessId);
        if (user == null)
            return null;

        if (branchId > 0 && !isGlobalAdmin && !user.UserBranches.Any(ub => ub.BranchId == branchId))
            throw new InvalidOperationException("You do not have access to modify this user in the selected branch.");

        if (!await _repository.RoleExistsAsync(dto.RoleId))
            throw new InvalidOperationException("Selected role is invalid.");

        var role = await GetRoleNameAsync(dto.RoleId);
        await ValidateBranchAssignmentAsync(dto.BusinessId, dto.BranchIds, role, isGlobalAdmin);

        if (await _repository.UsernameExistsAsync(dto.Username, id))
            throw new InvalidOperationException("Username is already taken.");

        if (await _repository.EmailExistsAsync(dto.Email, id))
            throw new InvalidOperationException("Email is already registered.");

        user.FullName = dto.FullName.Trim();
        user.Username = dto.Username.Trim();
        user.Email = dto.Email.Trim();
        user.Phone = dto.Phone?.Trim() ?? string.Empty;
        user.RoleId = dto.RoleId;
        user.IsActive = dto.IsActive;
        user.Status = dto.IsActive ? UserStatus.Active : UserStatus.Inactive;

        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = _passwordHasher.HashPassword(dto.Password);

        if (dto.BranchIds.Count > 0)
            await _repository.ReplaceUserBranchesAsync(user.Id, dto.BranchIds);

        await _repository.SaveChangesAsync();
        return await _repository.GetByIdAsync(id, dto.BusinessId);
    }

    public async Task DeleteUserAsync(int id, int businessId, int branchId, bool isGlobalAdmin)
    {
        var user = await _repository.GetTrackedByIdAsync(id, businessId);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        if (branchId > 0 && !isGlobalAdmin && !user.UserBranches.Any(ub => ub.BranchId == branchId))
            throw new InvalidOperationException("You do not have access to delete this user in the selected branch.");

        user.IsDeleted = true;
        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;
        user.Status = UserStatus.Terminated;

        await _repository.SaveChangesAsync();
    }

    public Task<IReadOnlyList<UserBranchAssignmentDto>> GetUserBranchesAsync(int userId, int businessId) =>
        _repository.GetUserBranchesAsync(userId);

    public async Task AssignUserBranchesAsync(int userId, AssignUserBranchesDto dto, int businessId, int branchId, bool isGlobalAdmin)
    {
        var user = await _repository.GetTrackedByIdAsync(userId, businessId);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        var roleName = user.Role?.Name ?? await GetRoleNameAsync(user.RoleId);
        await ValidateBranchAssignmentAsync(businessId, dto.BranchIds, roleName, isGlobalAdmin);

        var merged = user.UserBranches.Select(ub => ub.BranchId).Union(dto.BranchIds).Distinct().ToList();
        if (merged.Count == 0 && !RoleNames.BypassesBranchRequirement(roleName))
            throw new InvalidOperationException("At least one branch must be assigned.");

        await _repository.ReplaceUserBranchesAsync(userId, merged);
        await _repository.SaveChangesAsync();
    }

    public async Task RemoveUserBranchAsync(int userId, int branchId, int businessId, int requestBranchId, bool isGlobalAdmin)
    {
        var user = await _repository.GetTrackedByIdAsync(userId, businessId);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        var roleName = user.Role?.Name ?? await GetRoleNameAsync(user.RoleId);
        var remaining = user.UserBranches.Where(ub => ub.BranchId != branchId).Select(ub => ub.BranchId).ToList();

        if (remaining.Count == 0 && !RoleNames.BypassesBranchRequirement(roleName))
            throw new InvalidOperationException("User must remain assigned to at least one branch.");

        await _repository.RemoveUserBranchAsync(userId, branchId);
        await _repository.SaveChangesAsync();
    }

    private async Task ValidateBranchAssignmentAsync(int businessId, IReadOnlyList<int> branchIds, string roleName, bool isGlobalAdmin)
    {
        if (RoleNames.BypassesBranchRequirement(roleName))
            return;

        if (branchIds.Count == 0)
            throw new InvalidOperationException("At least one branch must be assigned.");

        if (!await _repository.BranchesExistAsync(businessId, branchIds))
            throw new InvalidOperationException("One or more selected branches are invalid.");
    }

    private async Task<string> GetRoleNameAsync(int roleId)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        return role?.Name ?? string.Empty;
    }

    private static void ValidateUserInput(string fullName, string username, string email, string? password, bool isEdit)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidOperationException("Full name is required.");

        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("Username is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required.");

        if (!isEdit && string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Password is required.");
    }
}
