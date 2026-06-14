using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Navigation.DTOs;
using POSSystem.Application.Navigation.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Navigation.Services;

public class NavigationMenuService : INavigationMenuService
{
    private readonly INavigationMenuRepository _repository;
    private readonly IPermissionService _permissionService;

    public NavigationMenuService(
        INavigationMenuRepository repository,
        IPermissionService permissionService)
    {
        _repository = repository;
        _permissionService = permissionService;
    }

    public async Task<IReadOnlyList<NavigationMenuDto>> GetAllowedMenusAsync(int roleId, string roleName)
    {
        var allMenus = await _repository.GetAllActiveAsync();
        var allowedIds = RoleNames.IsMasterUser(roleName)
            ? allMenus.Select(m => m.Id).ToHashSet()
            : ResolveAllowedMenuIds(allMenus, await _permissionService.GetPermissionsAsync(roleId));

        return allMenus
            .Where(m => allowedIds.Contains(m.Id))
            .OrderBy(m => m.ParentId ?? m.Id)
            .ThenBy(m => m.DisplayOrder)
            .ThenBy(m => m.Name)
            .Select(Map)
            .ToList();
    }

    private static HashSet<int> ResolveAllowedMenuIds(
        IReadOnlyList<AppMenu> allMenus,
        IReadOnlyList<Users.DTOs.RolePermissionDto> permissions)
    {
        var viewableModules = permissions
            .Where(p => p.CanView)
            .Select(p => p.ModuleName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allowedIds = new HashSet<int>();

        foreach (var menu in allMenus.Where(m => !string.IsNullOrWhiteSpace(m.Route)))
        {
            if (string.IsNullOrWhiteSpace(menu.ModuleName) ||
                viewableModules.Contains(menu.ModuleName))
            {
                allowedIds.Add(menu.Id);
            }
        }

        foreach (var group in allMenus.Where(m => string.IsNullOrWhiteSpace(m.Route)))
        {
            if (allMenus.Any(child => child.ParentId == group.Id && allowedIds.Contains(child.Id)))
            {
                allowedIds.Add(group.Id);
            }
        }

        return allowedIds;
    }

    private static NavigationMenuDto Map(AppMenu menu) => new()
    {
        Id = menu.Id,
        Name = menu.Name,
        Route = menu.Route,
        Icon = menu.Icon,
        ModuleName = menu.ModuleName,
        ParentId = menu.ParentId,
        DisplayOrder = menu.DisplayOrder
    };
}
