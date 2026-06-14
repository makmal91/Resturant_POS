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

        var branchIds = user.UserBranches.Select(ub => ub.BranchId).Distinct().ToList();
        var primaryBranchId = branchIds.FirstOrDefault();

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
            UserId = user.Id,
            FullName = user.FullName,
            Username = user.Username,
            Email = user.Email,
            RoleId = user.RoleId,
            RoleName = user.Role.Name,
            BusinessId = user.BusinessId,
            PrimaryBranchId = primaryBranchId,
            BranchIds = branchIds
        };
    }
}
