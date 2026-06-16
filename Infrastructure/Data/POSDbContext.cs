using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using POSSystem.Application.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Configurations;

namespace POSSystem.Infrastructure.Data;

public class POSDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public POSDbContext(DbContextOptions<POSDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    #region DbSets
    public DbSet<Business> Businesses { get; set; } = null!;
    public DbSet<Country> Countries { get; set; } = null!;
    public DbSet<City> Cities { get; set; } = null!;
    public DbSet<Currency> Currencies { get; set; } = null!;
    public DbSet<Branch> Branches { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserBranch> UserBranches { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<PermissionModule> PermissionModules { get; set; } = null!;
    public DbSet<ModuleForm> ModuleForms { get; set; } = null!;
    public DbSet<RoleFormPermission> RoleFormPermissions { get; set; } = null!;
    public DbSet<AppMenu> Menus { get; set; } = null!;
    public DbSet<MenuCategory> MenuCategories { get; set; } = null!;
    public DbSet<SubCategory> SubCategories { get; set; } = null!;
    public DbSet<Brand> Brands { get; set; } = null!;
    public DbSet<MeasurementUnit> Units { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductUnit> ProductUnits { get; set; } = null!;
    public DbSet<ProductVariant> ProductVariants { get; set; } = null!;
    public DbSet<ProductBarcode> ProductBarcodes { get; set; } = null!;
    public DbSet<ProductImage> ProductImages { get; set; } = null!;
    public DbSet<MenuItem> MenuItems { get; set; } = null!;
    public DbSet<MenuItemVariant> MenuItemVariants { get; set; } = null!;
    public DbSet<MenuItemAddon> MenuItemAddons { get; set; } = null!;
    public DbSet<Table> Tables { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
    public DbSet<Recipe> Recipes { get; set; } = null!;
    public DbSet<StockMovement> StockMovements { get; set; } = null!;

    // Warehouse & Purchase module
    public DbSet<Warehouse> Warehouses { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<Purchase> Purchases { get; set; } = null!;
    public DbSet<PurchaseItem> PurchaseItems { get; set; } = null!;
    public DbSet<StockLedger> StockLedgerEntries { get; set; } = null!;

    // POS Sales module
    public DbSet<SaleInvoice> SaleInvoices { get; set; } = null!;
    public DbSet<SaleInvoiceItem> SaleInvoiceItems { get; set; } = null!;

    // Cash Flow module
    public DbSet<CashFlowTransaction> CashFlowTransactions { get; set; } = null!;
    public DbSet<CashRegister> CashRegisters { get; set; } = null!;

    // Expenses module
    public DbSet<Expense> Expenses { get; set; } = null!;
    public DbSet<ExpenseCategory> ExpenseCategories { get; set; } = null!;

    // Code generation
    public DbSet<CodeSequence> CodeSequences { get; set; } = null!;
    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new BusinessConfiguration());
        modelBuilder.ApplyConfiguration(new CountryConfiguration());
        modelBuilder.ApplyConfiguration(new CityConfiguration());
        modelBuilder.ApplyConfiguration(new CurrencyConfiguration());
        modelBuilder.ApplyConfiguration(new BranchConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserBranchConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionModuleConfiguration());
        modelBuilder.ApplyConfiguration(new ModuleFormConfiguration());
        modelBuilder.ApplyConfiguration(new RoleFormPermissionConfiguration());
        modelBuilder.ApplyConfiguration(new AppMenuConfiguration());
        modelBuilder.ApplyConfiguration(new MenuCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new SubCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new BrandConfiguration());
        modelBuilder.ApplyConfiguration(new MeasurementUnitConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new ProductUnitConfiguration());
        modelBuilder.ApplyConfiguration(new ProductVariantConfiguration());
        modelBuilder.ApplyConfiguration(new ProductBarcodeConfiguration());
        modelBuilder.ApplyConfiguration(new ProductImageConfiguration());
        modelBuilder.ApplyConfiguration(new MenuItemConfiguration());
        modelBuilder.ApplyConfiguration(new MenuItemVariantConfiguration());
        modelBuilder.ApplyConfiguration(new MenuItemAddonConfiguration());
        modelBuilder.ApplyConfiguration(new TableConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryItemConfiguration());
        modelBuilder.ApplyConfiguration(new RecipeConfiguration());
        modelBuilder.ApplyConfiguration(new StockMovementConfiguration());
        modelBuilder.ApplyConfiguration(new WarehouseConfiguration());
        modelBuilder.ApplyConfiguration(new SupplierConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseItemConfiguration());
        modelBuilder.ApplyConfiguration(new StockLedgerConfiguration());
        modelBuilder.ApplyConfiguration(new SaleInvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new SaleInvoiceItemConfiguration());
        modelBuilder.ApplyConfiguration(new CashFlowTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new CashRegisterConfiguration());
        modelBuilder.ApplyConfiguration(new ExpenseCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ExpenseConfiguration());
        modelBuilder.ApplyConfiguration(new CodeSequenceConfiguration());

        // Configure BaseEntity default values
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) ||
                entityType.ClrType == typeof(Business) ||
                entityType.ClrType == typeof(User))
                continue;

            modelBuilder.Entity(entityType.ClrType)
                .Property<DateTime>("CreatedAt")
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity(entityType.ClrType)
                .Property<int>("BusinessId")
                .HasDefaultValue(1);

            modelBuilder.Entity(entityType.ClrType)
                .HasIndex("BusinessId")
                .HasDatabaseName($"idx_{entityType.GetTableName()?.ToLowerInvariant()}_businessid");

            if (entityType.ClrType != typeof(Branch))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex("BranchId")
                    .HasDatabaseName($"idx_{entityType.GetTableName()?.ToLowerInvariant()}_branchid");

                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex("BusinessId", "BranchId")
                    .HasDatabaseName($"idx_{entityType.GetTableName()?.ToLowerInvariant()}_business_branch");
            }

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(BuildTenantFilter(entityType.ClrType));
        }

        modelBuilder.Entity<User>().HasQueryFilter(BuildBusinessScopedFilter<User>());

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var defaultBusiness = new Business
        {
            Id = 1,
            Name = "Main Business",
            LegalName = "Main Business Pvt Ltd",
            Phone = "+1234567890",
            Email = "owner@restaurant.com",
            Address = "123 Main Street",
            TaxNumber = "NTN-0001",
            CurrencyId = 1,
            Currency = "PKR",
            TimeZone = "UTC",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        modelBuilder.Entity<Business>().HasData(defaultBusiness);

        var countries = new[]
        {
            new Country { Id = 1, Name = "United States", Code = "US", IsActive = true },
            new Country { Id = 2, Name = "United Kingdom", Code = "GB", IsActive = true },
            new Country { Id = 3, Name = "Pakistan", Code = "PK", IsActive = true },
            new Country { Id = 4, Name = "United Arab Emirates", Code = "AE", IsActive = true }
        };

        modelBuilder.Entity<Country>().HasData(countries);

        var cities = new[]
        {
            new City { Id = 1, Name = "New York", CountryId = 1, IsActive = true },
            new City { Id = 2, Name = "Los Angeles", CountryId = 1, IsActive = true },
            new City { Id = 3, Name = "London", CountryId = 2, IsActive = true },
            new City { Id = 4, Name = "Manchester", CountryId = 2, IsActive = true },
            new City { Id = 5, Name = "Karachi", CountryId = 3, IsActive = true },
            new City { Id = 6, Name = "Lahore", CountryId = 3, IsActive = true },
            new City { Id = 7, Name = "Islamabad", CountryId = 3, IsActive = true },
            new City { Id = 8, Name = "Dubai", CountryId = 4, IsActive = true },
            new City { Id = 9, Name = "Abu Dhabi", CountryId = 4, IsActive = true }
        };

        modelBuilder.Entity<City>().HasData(cities);

        var currencies = new[]
        {
            new Currency { Id = 1, Code = "PKR", Name = "Pakistani Rupee", Symbol = "₨", ExchangeRateToPKR = 1m, IsBase = true, IsActive = true },
            new Currency { Id = 2, Code = "USD", Name = "US Dollar", Symbol = "$", ExchangeRateToPKR = 278m, IsBase = false, IsActive = true },
            new Currency { Id = 3, Code = "GBP", Name = "British Pound", Symbol = "£", ExchangeRateToPKR = 350m, IsBase = false, IsActive = true },
            new Currency { Id = 4, Code = "AED", Name = "UAE Dirham", Symbol = "د.إ", ExchangeRateToPKR = 75.7m, IsBase = false, IsActive = true },
            new Currency { Id = 5, Code = "EUR", Name = "Euro", Symbol = "€", ExchangeRateToPKR = 300m, IsBase = false, IsActive = true },
        };

        modelBuilder.Entity<Currency>().HasData(currencies);

        // Seed Default Branch
        var defaultBranch = new Branch
        {
            Id = 1,
            Name = "Main Branch",
            Code = "MAIN",
            Address = "123 Main Street",
            CountryId = 1,
            CityId = 1,
            Phone = "+1234567890",
            Email = "main@restaurant.com",
            OpeningTime = new TimeSpan(11, 0, 0),
            ClosingTime = new TimeSpan(22, 0, 0),
            IsActive = true,
            BusinessId = 1,
            CreatedAt = DateTime.UtcNow
        };

        modelBuilder.Entity<Branch>().HasData(defaultBranch);

        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        const string adminPasswordHash = "$2a$11$QvHz8.HeIU5ThFqjVPVVe.sTuKqDQI6R0nrPz/Z8KqK8qXyxi3H7O"; // Admin@123

        var roles = new[]
        {
            new Role { Id = 1, Name = RoleNames.SystemAdmin, Description = "Full system access", Permissions = "all", IsActive = true, CreatedDate = seedDate, UpdatedDate = null },
            new Role { Id = 2, Name = RoleNames.SuperAdmin, Description = "All branches access", Permissions = "all", IsActive = true, CreatedDate = seedDate, UpdatedDate = null },
            new Role { Id = 3, Name = RoleNames.Admin, Description = "Branch-level management", Permissions = "branch_admin", IsActive = true, CreatedDate = seedDate, UpdatedDate = null },
            new Role { Id = 4, Name = RoleNames.Manager, Description = "Operations control", Permissions = "operations", IsActive = true, CreatedDate = seedDate, UpdatedDate = null },
            new Role { Id = 5, Name = RoleNames.Cashier, Description = "POS billing access", Permissions = "pos", IsActive = true, CreatedDate = seedDate, UpdatedDate = null }
        };

        modelBuilder.Entity<Role>().HasData(roles);

        var adminUser = new User
        {
            Id = 1,
            FullName = "System Administrator",
            Username = "admin",
            PasswordHash = adminPasswordHash,
            Phone = "+1234567890",
            Email = "admin@restaurant.com",
            RoleId = 1,
            BusinessId = 1,
            IsActive = true,
            Salary = 0,
            ShiftType = ShiftType.Flexible,
            Status = UserStatus.Active,
            CreatedAt = seedDate
        };

        modelBuilder.Entity<User>().HasData(adminUser);

        modelBuilder.Entity<UserBranch>().HasData(new UserBranch
        {
            UserId = 1,
            BranchId = 1
        });
    }

    public override int SaveChanges()
    {
        ApplyTenantAssignments();
        ApplySoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantAssignments();
        ApplySoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplySoftDelete()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State != EntityState.Deleted)
                continue;

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.ModifiedAt = DateTime.UtcNow;

            if (entry.Entity is User user)
                user.DeletedAt = DateTime.UtcNow;
        }

        foreach (var entry in ChangeTracker.Entries<Role>())
        {
            if (entry.State != EntityState.Deleted)
                continue;

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.UpdatedDate = DateTime.UtcNow;
        }
    }

    private void ApplyTenantAssignments()
    {
        var businessId = _tenantContext.BusinessId ?? 1;
        var branchId = _tenantContext.BranchId ?? 1;
        var userId = _tenantContext.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.Entity is Business or User)
                continue;

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.BusinessId <= 0)
                    entry.Entity.BusinessId = businessId;

                if (entry.Entity is not Branch && entry.Entity.BranchId <= 0)
                    entry.Entity.BranchId = branchId;

                if (userId.HasValue && entry.Entity.CreatedBy == null)
                    entry.Entity.CreatedBy = userId.Value;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAt = DateTime.UtcNow;
                if (userId.HasValue)
                    entry.Entity.ModifiedBy = userId.Value;
            }
        }

        foreach (var entry in ChangeTracker.Entries<User>())
        {
            if (entry.State != EntityState.Added)
                continue;

            if (entry.Entity.BusinessId <= 0)
                entry.Entity.BusinessId = businessId;

            if (entry.Entity.BranchId <= 0)
            {
                var tenantBranchId = _tenantContext.BranchId ?? 0;
                if (tenantBranchId > 0)
                    entry.Entity.BranchId = tenantBranchId;
            }

            if (userId.HasValue && entry.Entity.CreatedBy == null)
                entry.Entity.CreatedBy = userId.Value;
        }
    }

    private LambdaExpression BuildTenantFilter(Type clrType)
    {
        var parameter = Expression.Parameter(clrType, "e");
        var businessProperty = Expression.Property(parameter, nameof(BaseEntity.BusinessId));
        var isDeletedProperty = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        // Using new property names (CreatedAt, ModifiedAt, CreatedBy, ModifiedBy)
        var notDeleted = Expression.Equal(isDeletedProperty, Expression.Constant(false));

        var isSuperAdmin = _tenantContext.IsMasterUser || _tenantContext.IsSuperAdmin;
        var businessId = _tenantContext.BusinessId;
        var branchId = _tenantContext.BranchId;

        Expression tenantPredicate;

        if (isSuperAdmin)
        {
            tenantPredicate = notDeleted;
        }
        else
        {
            Expression? scopedPredicate = null;

            if (businessId.HasValue)
            {
                scopedPredicate = Expression.Equal(businessProperty, Expression.Constant(businessId.Value));
            }

            if (branchId.HasValue && branchId.Value > 0 && clrType != typeof(Branch))
            {
                var branchProperty = Expression.Property(parameter, nameof(BaseEntity.BranchId));
                var branchPredicate = Expression.Equal(branchProperty, Expression.Constant(branchId.Value));
                scopedPredicate = scopedPredicate == null
                    ? branchPredicate
                    : Expression.AndAlso(scopedPredicate, branchPredicate);
            }

            tenantPredicate = scopedPredicate ?? Expression.Constant(true);
            tenantPredicate = Expression.AndAlso(tenantPredicate, notDeleted);
        }

        return Expression.Lambda(tenantPredicate, parameter);
    }

    private LambdaExpression BuildBusinessScopedFilter<TEntity>() where TEntity : BaseEntity
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var businessProperty = Expression.Property(parameter, nameof(BaseEntity.BusinessId));
        var isDeletedProperty = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var notDeleted = Expression.Equal(isDeletedProperty, Expression.Constant(false));

        var isSuperAdmin = _tenantContext.IsMasterUser || _tenantContext.IsSuperAdmin;
        var businessId = _tenantContext.BusinessId;

        Expression predicate;

        if (isSuperAdmin)
        {
            predicate = notDeleted;
        }
        else if (businessId.HasValue)
        {
            predicate = Expression.AndAlso(
                Expression.Equal(businessProperty, Expression.Constant(businessId.Value)),
                notDeleted);
        }
        else
        {
            predicate = notDeleted;
        }

        return Expression.Lambda(predicate, parameter);
    }
}