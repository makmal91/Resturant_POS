using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Application;
using POSSystem.Application.Orders.Interfaces;
using POSSystem.Application.Menu.Interfaces;
using POSSystem.Application.Inventory.Interfaces;
using POSSystem.Application.Recipe.Interfaces;
using POSSystem.Infrastructure.Data;
using POSSystem.Infrastructure.Repositories;

namespace POSSystem.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<POSDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Register repositories
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();

        // Add other infrastructure services here

        return services;
    }
}