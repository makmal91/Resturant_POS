using POSSystem.Application.Navigation.DTOs;

namespace POSSystem.Application.Navigation.Interfaces;

public interface INavigationMenuService
{
    Task<IReadOnlyList<NavigationMenuDto>> GetAllowedMenusAsync(int roleId, string roleName);
    Task<IReadOnlyList<SidebarMenuItemDto>> GetSidebarTreeAsync(int roleId, string roleName);
}
