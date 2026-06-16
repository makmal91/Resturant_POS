using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Navigation.DTOs;
using POSSystem.Application.Navigation.Interfaces;
using POSSystem.Application.Modules.Interfaces;
using POSSystem.Application.Users.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Navigation.Services;

public class NavigationMenuService : INavigationMenuService
{
    private readonly IModuleRepository _moduleRepository;
    private readonly IPermissionService _permissionService;

    public NavigationMenuService(
        IModuleRepository moduleRepository,
        IPermissionService permissionService)
    {
        _moduleRepository = moduleRepository;
        _permissionService = permissionService;
    }

    public async Task<IReadOnlyList<NavigationMenuDto>> GetAllowedMenusAsync(int roleId, string roleName)
    {
        var tree = await GetSidebarTreeAsync(roleId, roleName);
        return FlattenTree(tree);
    }

    public async Task<IReadOnlyList<SidebarMenuItemDto>> GetSidebarTreeAsync(int roleId, string roleName)
    {
        var allModules = await _moduleRepository.GetSidebarModulesAsync();
        var permissions = RoleNames.CanBypassPermissions(roleName)
            ? null
            : await _permissionService.GetPermissionsAsync(roleId);

        return FilterSidebarTree(allModules, permissions, roleName);
    }

    private static IReadOnlyList<SidebarMenuItemDto> FilterSidebarTree(
        IReadOnlyList<Modules.DTOs.ModuleListItemDto> modules,
        IReadOnlyList<RolePermissionDto>? permissions,
        string roleName)
    {
        if (RoleNames.CanBypassPermissions(roleName))
            return modules.Select(MapSidebarItem).ToList();

        var (viewableModuleIds, viewableNames) = BuildViewableSets(permissions ?? []);

        return modules
            .Select(module => MapFilteredItem(module, viewableModuleIds, viewableNames))
            .Where(item => item != null)
            .Cast<SidebarMenuItemDto>()
            .ToList();
    }

    private static (HashSet<int> ModuleIds, HashSet<string> Names) BuildViewableSets(
        IReadOnlyList<RolePermissionDto> permissions)
    {
        var moduleIds = new HashSet<int>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in permissions.Where(p => p.CanView))
        {
            if (permission.ModuleId.HasValue)
                moduleIds.Add(permission.ModuleId.Value);

            if (!string.IsNullOrWhiteSpace(permission.ModuleName))
                names.Add(permission.ModuleName.Trim());
        }

        return (moduleIds, names);
    }

    private static bool HasModuleViewAccess(
        Modules.DTOs.ModuleListItemDto module,
        HashSet<int> viewableModuleIds,
        HashSet<string> viewableNames)
    {
        if (viewableModuleIds.Contains(module.Id))
            return true;

        if (!string.IsNullOrWhiteSpace(module.ModuleKey) &&
            viewableNames.Contains(module.ModuleKey))
            return true;

        if (viewableNames.Contains(module.ModuleName))
            return true;

        // Cash flow sub-pages share the main Cash Flow permission.
        if (!string.IsNullOrWhiteSpace(module.ModuleKey) &&
            module.ModuleKey.StartsWith("CashFlow.", StringComparison.OrdinalIgnoreCase) &&
            viewableNames.Contains(PermissionModules.CashFlow))
            return true;

        return false;
    }

    private static SidebarMenuItemDto? MapFilteredItem(
        Modules.DTOs.ModuleListItemDto module,
        HashSet<int> viewableModuleIds,
        HashSet<string> viewableNames)
    {
        var isGroup = string.IsNullOrWhiteSpace(module.ModuleKey);
        var children = module.Children
            .Select(child => MapFilteredItem(child, viewableModuleIds, viewableNames))
            .Where(child => child != null)
            .Cast<SidebarMenuItemDto>()
            .ToList();

        if (isGroup)
        {
            if (children.Count == 0)
                return null;

            return new SidebarMenuItemDto
            {
                Id = module.Id,
                Name = module.ModuleName,
                Route = null,
                Icon = module.Icon,
                ModuleName = null,
                DisplayOrder = module.DisplayOrder,
                Children = children
            };
        }

        if (!HasModuleViewAccess(module, viewableModuleIds, viewableNames))
            return null;

        return new SidebarMenuItemDto
        {
            Id = module.Id,
            Name = module.ModuleName,
            Route = module.Route,
            Icon = module.Icon,
            ModuleName = module.ModuleKey,
            DisplayOrder = module.DisplayOrder,
            Children = children
        };
    }

    private static SidebarMenuItemDto MapSidebarItem(Modules.DTOs.ModuleListItemDto module) =>
        new()
        {
            Id = module.Id,
            Name = module.ModuleName,
            Route = module.Route,
            Icon = module.Icon,
            ModuleName = string.IsNullOrWhiteSpace(module.ModuleKey) ? null : module.ModuleKey,
            DisplayOrder = module.DisplayOrder,
            Children = module.Children.Select(MapSidebarItem).ToList()
        };

    private static IReadOnlyList<NavigationMenuDto> FlattenTree(IReadOnlyList<SidebarMenuItemDto> tree)
    {
        var result = new List<NavigationMenuDto>();

        void Walk(IReadOnlyList<SidebarMenuItemDto> items, int? parentId)
        {
            foreach (var item in items)
            {
                result.Add(new NavigationMenuDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Route = item.Route,
                    Icon = item.Icon,
                    ModuleName = item.ModuleName,
                    ParentId = parentId,
                    DisplayOrder = item.DisplayOrder
                });

                if (item.Children.Count > 0)
                    Walk(item.Children, item.Id);
            }
        }

        Walk(tree, null);
        return result;
    }
}
