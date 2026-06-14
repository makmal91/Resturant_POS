using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using POSSystem.Application.Orders.Interfaces;
using POSSystem.Application.Orders.Services;
using POSSystem.Application.Menu.Interfaces;
using POSSystem.Application.Menu.Services;
using POSSystem.Application.Inventory.Interfaces;
using POSSystem.Application.Inventory.Services;
using POSSystem.Application.Recipe.Interfaces;
using POSSystem.Application.Recipe.Services;
using POSSystem.Application.Business.Interfaces;
using POSSystem.Application.Business.Services;
using POSSystem.Application.Branch.Interfaces;
using POSSystem.Application.Branch.Services;
using POSSystem.Application.Users.Interfaces;
using POSSystem.Application.Users.Services;
using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Auth.Services;
using POSSystem.Application.Navigation.Interfaces;
using POSSystem.Application.Navigation.Services;

namespace POSSystem.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // Register order services
        services.AddScoped<IOrderService, OrderService>();

        // Register menu services
        services.AddScoped<IMenuService, MenuService>();

        // Register inventory services
        services.AddScoped<IInventoryService, InventoryService>();

        // Register recipe services
        services.AddScoped<IRecipeService, RecipeService>();

        // Register business services
        services.AddScoped<IBusinessService, BusinessService>();

        // Register branch services
        services.AddScoped<IBranchService, BranchService>();

        // Register user management services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<INavigationMenuService, NavigationMenuService>();

        // Add other application services here

        return services;
    }
}