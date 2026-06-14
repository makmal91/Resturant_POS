namespace POSSystem.Application.Auth.Interfaces;

public interface ITokenService
{
    string GenerateToken(
        int userId,
        string username,
        string roleName,
        int roleId,
        int businessId,
        int primaryBranchId,
        IReadOnlyList<int> branchIds);
}
