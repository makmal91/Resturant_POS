using POSSystem.Application.Auth.DTOs;
using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Users.Interfaces;

namespace POSSystem.Application.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
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

        var branches = user.UserBranches
            .Where(ub => ub.Branch != null && ub.Branch.IsActive)
            .Select(ub => new AuthBranchDto
            {
                Id = ub.BranchId,
                Name = ub.Branch!.Name
            })
            .DistinctBy(b => b.Id)
            .OrderBy(b => b.Name)
            .ToList();

        if (branches.Count == 0)
            throw new InvalidOperationException("No branch assigned.");

        var branchIds = branches.Select(b => b.Id).ToList();
        var primaryBranchId = branchIds[0];

        var token = _tokenService.GenerateToken(
            user.Id,
            user.Username,
            user.Role.Name,
            user.RoleId,
            user.BusinessId,
            primaryBranchId,
            branchIds);

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
                RoleName = user.Role.Name
            },
            Branches = branches
        };
    }
}
