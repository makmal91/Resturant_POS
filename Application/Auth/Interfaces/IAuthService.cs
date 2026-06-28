using POSSystem.Application.Auth.DTOs;
using POSSystem.Application.Users.DTOs;

namespace POSSystem.Application.Auth.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthPermissionsResponseDto> GetCurrentUserPermissionsAsync(int roleId, string roleName);
}
