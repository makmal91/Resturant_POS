namespace POSSystem.Application.Auth.DTOs;

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int BusinessId { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsMasterUser { get; set; }
    public bool IsGlobalAdmin { get; set; }
}

public class AuthBranchDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public AuthUserDto User { get; set; } = new();
    public IReadOnlyList<AuthBranchDto> Branches { get; set; } = Array.Empty<AuthBranchDto>();
    public IReadOnlyList<Users.DTOs.RolePermissionDto> Permissions { get; set; } = Array.Empty<Users.DTOs.RolePermissionDto>();
}
