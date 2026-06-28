using POSSystem.Application.Auth.DTOs;
using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Branch.Interfaces;
using POSSystem.Application.Users.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IPermissionService _permissionService;
    private readonly IFeaturePermissionService _featurePermissionService;

    public AuthService(
        IUserRepository userRepository,
        IBranchRepository branchRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IPermissionService permissionService,
        IFeaturePermissionService featurePermissionService)
    {
        _userRepository = userRepository;
        _branchRepository = branchRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _permissionService = permissionService;
        _featurePermissionService = featurePermissionService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Username and password are required.");

        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null || !user.IsActive)
            throw new InvalidOperationException("Invalid username or password.");

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid username or password.");

        var isMasterUser = RoleNames.IsMasterUser(user.Role.Name);
        var hasGlobalBranchAccess = RoleNames.HasGlobalBranchAccess(user.Role.Name);
        var branches = hasGlobalBranchAccess
            ? (await _branchRepository.GetAllActiveSummariesAsync())
                .Select(b => new AuthBranchDto
                {
                    Id = b.Id,
                    Name = b.Name
                })
                .ToList()
            : user.UserBranches
                .Where(ub => ub.Branch != null && ub.Branch.IsActive)
                .Select(ub => new AuthBranchDto
                {
                    Id = ub.BranchId,
                    Name = ub.Branch!.Name
                })
                .DistinctBy(b => b.Id)
                .OrderBy(b => b.Name)
                .ToList();

        if (branches.Count == 0 && !RoleNames.BypassesBranchRequirement(user.Role.Name))
            throw new InvalidOperationException("No branch assigned.");

        var branchIds = branches.Select(b => b.Id).ToList();
        var primaryBranchId = branchIds.Count > 0 ? branchIds[0] : 0;

        var token = _tokenService.GenerateToken(
            user.Id,
            user.Username,
            user.Role.Name,
            user.RoleId,
            user.BusinessId,
            primaryBranchId,
            branchIds);

        var permissions = await _permissionService.GetPermissionsAsync(user.RoleId);
        var features = await _featurePermissionService.GetEnabledFeaturesForRoleAsync(user.RoleId, user.Role.Name);

        return new LoginResponseDto
        {
            Token = token,
            User = new AuthUserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                BusinessId = user.BusinessId,
                RoleId = user.RoleId,
                RoleName = user.Role.Name,
                IsMasterUser = isMasterUser,
                IsGlobalAdmin = hasGlobalBranchAccess
            },
            Branches = branches,
            Permissions = permissions,
            Features = features
        };
    }

    public async Task<AuthPermissionsResponseDto> GetCurrentUserPermissionsAsync(int roleId, string roleName)
    {
        var permissions = await _permissionService.GetPermissionsAsync(roleId);
        var features = await _featurePermissionService.GetEnabledFeaturesForRoleAsync(roleId, roleName);

        return new AuthPermissionsResponseDto
        {
            Permissions = permissions,
            Features = features
        };
    }
}
