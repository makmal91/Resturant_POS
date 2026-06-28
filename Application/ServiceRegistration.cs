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
using POSSystem.Application.Brand.Interfaces;
using POSSystem.Application.Brand.Services;
using POSSystem.Application.Product.Interfaces;
using POSSystem.Application.Product.Services;
using POSSystem.Application.Unit.Interfaces;
using POSSystem.Application.Unit.Services;
using POSSystem.Application.Business.Interfaces;
using POSSystem.Application.Business.Services;
using POSSystem.Application.Branch.Interfaces;
using POSSystem.Application.Branch.Services;
using POSSystem.Application.Users.Interfaces;
using POSSystem.Application.Users.Services;
using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application.Auth.Services;
using POSSystem.Application.Barcode.Interfaces;
using POSSystem.Application.Barcode.Services;
using POSSystem.Application.Navigation.Interfaces;
using POSSystem.Application.Navigation.Services;
using POSSystem.Application.Modules.Interfaces;
using POSSystem.Application.Modules.Services;
using POSSystem.Application.Warehouse.Interfaces;
using POSSystem.Application.Warehouse.Services;
using POSSystem.Application.Supplier.Interfaces;
using POSSystem.Application.Supplier.Services;
using POSSystem.Application.Purchase.Interfaces;
using POSSystem.Application.Purchase.Services;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Application.Stock.Services;
using POSSystem.Application.Sales.Interfaces;
using POSSystem.Application.Sales.Services;
using POSSystem.Application.Customer.Interfaces;
using POSSystem.Application.Customer.Services;
using POSSystem.Application.CashFlow.Interfaces;
using POSSystem.Application.CashFlow.Services;
using POSSystem.Application.Ledger.Interfaces;
using POSSystem.Application.Ledger.Services;
using POSSystem.Application.Payments.Interfaces;
using POSSystem.Application.Reports.Interfaces;
using POSSystem.Application.Reports.Services;
using POSSystem.Application.Payments.Services;
using POSSystem.Application.License.Interfaces;
using POSSystem.Application.License.Services;

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

        // Register brand services
        services.AddScoped<IBrandService, BrandService>();

        // Register product management services
        services.AddScoped<IUnitPricingService, UnitPricingService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IBarcodePrintService, BarcodePrintService>();

        // Register unit master services
        services.AddScoped<IUnitService, UnitService>();

        // Register business services
        services.AddScoped<IBusinessService, BusinessService>();

        // Register branch services
        services.AddScoped<IBranchService, BranchService>();

        // Register user management services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IPermissionAssignmentValidator, PermissionAssignmentValidator>();
        services.AddScoped<IFeaturePermissionService, FeaturePermissionService>();
        services.AddScoped<INavigationMenuService, NavigationMenuService>();
        services.AddScoped<IModuleService, ModuleService>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();

        // Register warehouse, supplier, purchase, stock services
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<ILowStockAlertService, LowStockAlertService>();
        services.AddScoped<IStockValidationService, StockValidationService>();

        // Register POS sales service
        services.AddScoped<ISalesService, SalesService>();

        // Register customer service
        services.AddScoped<ICustomerService, CustomerService>();

        // Register cash flow service
        services.AddScoped<ICashFlowService, CashFlowService>();

        // Party ledger service
        services.AddScoped<IPartyLedgerService, PartyLedgerService>();

        // Invoice payment service
        services.AddScoped<IInvoicePaymentService, InvoicePaymentService>();

        // Reports service
        services.AddScoped<IReportService, ReportService>();

        // License enforcement
        services.AddScoped<ILicenseEnforcementService, LicenseEnforcementService>();

        // Add other application services here

        return services;
    }
}