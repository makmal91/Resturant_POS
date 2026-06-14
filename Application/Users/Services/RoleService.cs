using POSSystem.Application.Common.Constants;
using POSSystem.Application.Users.DTOs;
using POSSystem.Application.Users.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Users.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _repository;

    public RoleService(IRoleRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<RoleListItemDto>> GetRolesAsync() =>
        _repository.GetAllAsync();

    public Task<RoleDetailDto?> GetRoleByIdAsync(int id) =>
        _repository.GetByIdAsync(id);

    public async Task<RoleDetailDto> CreateRoleAsync(CreateRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Role name is required.");

        if (await _repository.NameExistsAsync(dto.Name))
            throw new InvalidOperationException("Role name already exists.");

        var role = new Role
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            IsActive = dto.IsActive,
            Permissions = string.Empty
        };

        await _repository.AddAsync(role);
        await _repository.SaveChangesAsync();

        await SeedDefaultPermissionsAsync(role.Id);
        await _repository.SaveChangesAsync();

        return (await _repository.GetByIdAsync(role.Id))!;
    }

    public async Task<RoleDetailDto?> UpdateRoleAsync(int id, UpdateRoleDto dto)
    {
        var role = await _repository.GetTrackedByIdAsync(id);
        if (role == null)
            return null;

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Role name is required.");

        if (await _repository.NameExistsAsync(dto.Name, id))
            throw new InvalidOperationException("Role name already exists.");

        role.Name = dto.Name.Trim();
        role.Description = dto.Description?.Trim() ?? string.Empty;
        role.IsActive = dto.IsActive;

        await _repository.SaveChangesAsync();
        return await _repository.GetByIdAsync(id);
    }

    public async Task DeleteRoleAsync(int id)
    {
        var role = await _repository.GetTrackedByIdAsync(id);
        if (role == null)
            throw new InvalidOperationException("Role not found.");

        if (role.Users.Any())
            throw new InvalidOperationException("Cannot delete a role that is assigned to users.");

        role.IsDeleted = true;
        role.IsActive = false;
        await _repository.SaveChangesAsync();
    }

    public Task<IReadOnlyList<RolePermissionDto>> GetRolePermissionsAsync(int roleId) =>
        _repository.GetPermissionsAsync(roleId);

    public async Task UpdateRolePermissionsAsync(int roleId, UpdateRolePermissionsDto dto)
    {
        var role = await _repository.GetTrackedByIdAsync(roleId);
        if (role == null)
            throw new InvalidOperationException("Role not found.");

        var permissions = dto.Permissions
            .Where(p => !string.IsNullOrWhiteSpace(p.ModuleName))
            .Select(p => new RolePermissionDto
            {
                ModuleName = p.ModuleName.Trim(),
                CanView = p.CanView,
                CanCreate = p.CanCreate,
                CanEdit = p.CanEdit,
                CanDelete = p.CanDelete
            })
            .ToList();

        await _repository.ReplacePermissionsAsync(roleId, permissions);
        await _repository.SaveChangesAsync();
    }

    private async Task SeedDefaultPermissionsAsync(int roleId)
    {
        var permissions = PermissionModules.All
            .Select(module => new RolePermissionDto
            {
                ModuleName = module,
                CanView = false,
                CanCreate = false,
                CanEdit = false,
                CanDelete = false
            })
            .ToList();

        await _repository.ReplacePermissionsAsync(roleId, permissions);
    }
}
