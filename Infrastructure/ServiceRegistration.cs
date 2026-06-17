using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Application;
using POSSystem.Application.Orders.Interfaces;
using POSSystem.Application.Menu.Interfaces;
using POSSystem.Application.Inventory.Interfaces;
using POSSystem.Application.Recipe.Interfaces;
using POSSystem.Application.Brand.Interfaces;
using POSSystem.Application.Product.Interfaces;
using POSSystem.Application.Unit.Interfaces;
using POSSystem.Application.Business.Interfaces;
using POSSystem.Application.Branch.Interfaces;
using POSSystem.Application.Users.Interfaces;
using POSSystem.Application.Navigation.Interfaces;
using POSSystem.Application.Modules.Interfaces;
using POSSystem.Application.Warehouse.Interfaces;
using POSSystem.Application.Supplier.Interfaces;
using POSSystem.Application.Purchase.Interfaces;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Application.Sales.Interfaces;
using POSSystem.Application.Customer.Interfaces;
using POSSystem.Application.CashFlow.Interfaces;
using POSSystem.Application.CodeSequence.Interfaces;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Application.License.Interfaces;
using POSSystem.Application.License.Options;
using POSSystem.Infrastructure.Data;
using POSSystem.Infrastructure.License;
using POSSystem.Infrastructure.Repositories;
using POSSystem.Infrastructure.Security;
using POSSystem.Infrastructure.Services;

namespace POSSystem.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<POSDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));

        services.Configure<LicenseOptions>(configuration.GetSection(LicenseOptions.SectionName));
        services.AddSingleton<ILicenseService, LicenseService>();
        services.AddScoped<ILicenseUsageProvider, LicenseUsageProvider>();

        // Register repositories
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<INavigationMenuRepository, NavigationMenuRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

        // Register warehouse, supplier, purchase, stock repositories
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IStockLedgerRepository, StockLedgerRepository>();

        // Register sales repository
        services.AddScoped<ISalesRepository, SalesRepository>();

        // Register customer repository
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        // Register cash flow repository
        services.AddScoped<ICashFlowRepository, CashFlowRepository>();

        // Centralized code generation
        services.AddScoped<ICodeGeneratorService, CodeGeneratorService>();
        services.AddScoped<ICodeSequenceService, CodeSequenceService>();

        // Add other infrastructure services here

        return services;
    }
}