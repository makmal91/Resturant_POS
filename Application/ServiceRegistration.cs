using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using POSSystem.Application.Orders.Interfaces;
using POSSystem.Application.Orders.Services;
using POSSystem.Application.Menu.Interfaces;
using POSSystem.Application.Menu.Services;

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

        // Add other application services here

        return services;
    }
}