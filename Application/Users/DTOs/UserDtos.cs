namespace POSSystem.Application.Users.DTOs;

public class UserListItemDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IReadOnlyList<UserBranchAssignmentDto> Branches { get; set; } = Array.Empty<UserBranchAssignmentDto>();
    public string AssignedBranchesDisplay { get; set; } = string.Empty;
    public int PrimaryBranchId { get; set; }
    public string PrimaryBranchName { get; set; } = string.Empty;
}

public class UserDetailDto : UserListItemDto
{
    public int BusinessId { get; set; }
}

public class UserBranchAssignmentDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
}

public class CreateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public IReadOnlyList<int> BranchIds { get; set; } = Array.Empty<int>();
    public int BusinessId { get; set; }
}

public class UpdateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Password { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public IReadOnlyList<int> BranchIds { get; set; } = Array.Empty<int>();
    public int BusinessId { get; set; }
}

public class AssignUserBranchesDto
{
    public IReadOnlyList<int> BranchIds { get; set; } = Array.Empty<int>();
}
