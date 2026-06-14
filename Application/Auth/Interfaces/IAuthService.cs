using POSSystem.Application.Auth.DTOs;

namespace POSSystem.Application.Auth.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
}
