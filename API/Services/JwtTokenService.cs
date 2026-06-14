using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using POSSystem.Application.Auth.Interfaces;

namespace POSSystem.API.Services;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(
        int userId,
        string username,
        string roleName,
        int roleId,
        int businessId,
        int primaryBranchId,
        IReadOnlyList<int> branchIds)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.")));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var branchIdsValue = string.Join(",", branchIds);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, roleName),
            new("roleId", roleId.ToString()),
            new("businessId", businessId.ToString()),
            new("branchId", primaryBranchId.ToString()),
            new("branchIds", branchIdsValue)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
