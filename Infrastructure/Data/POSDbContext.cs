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
    public DbSet<Branch> Branches { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<MenuCategory> MenuCategories { get; set; } = null!;
    public DbSet<SubCategory> SubCategories { get; set; } = null!;
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
    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new BusinessConfiguration());
        modelBuilder.ApplyConfiguration(new CountryConfiguration());
        modelBuilder.ApplyConfiguration(new CityConfiguration());
        modelBuilder.ApplyConfiguration(new BranchConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new MenuCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new SubCategoryConfiguration());
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

        // Configure BaseEntity default values
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) || entityType.ClrType == typeof(Business))
                continue;

            modelBuilder.Entity(entityType.ClrType)
                .Property<DateTime>("CreatedDate")
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity(entityType.ClrType)
                .Property<int>("BusinessId")
                .HasDefaultValue(1);

            modelBuilder.Entity(entityType.ClrType)
                .HasIndex("BusinessId")
                .HasDatabaseName($"idx_{entityType.GetTableName()?.ToLowerInvariant()}_businessid");

            modelBuilder.Entity(entityType.ClrType)
                .HasIndex("BranchId")
                .HasDatabaseName($"idx_{entityType.GetTableName()?.ToLowerInvariant()}_branchid");

            modelBuilder.Entity(entityType.ClrType)
                .HasIndex("BusinessId", "BranchId")
                .HasDatabaseName($"idx_{entityType.GetTableName()?.ToLowerInvariant()}_business_branch");

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(BuildTenantFilter(entityType.ClrType));
        }

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
            Currency = "USD",
            TimeZone = "UTC",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
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
            BranchId = 1,
            CreatedDate = DateTime.UtcNow
        };

        modelBuilder.Entity<Branch>().HasData(defaultBranch);

        // Seed Admin Role
        var adminRole = new Role
        {
            Id = 1,
            Name = "Admin",
            Permissions = "all_permissions",
            BusinessId = 1,
            BranchId = 1,
            CreatedDate = DateTime.UtcNow
        };

        modelBuilder.Entity<Role>().HasData(adminRole);

        // Seed Admin User
        // Password: Admin@123 (BCrypt hash: $2a$11$QvHz8.HeIU5ThFqjVPVVe.sTuKqDQI6R0nrPz/Z8KqK8qXyxi3H7O)
        var adminUser = new User
        {
            Id = 1,
            FullName = "System Administrator",
            Username = "admin",
            PasswordHash = "$2a$11$QvHz8.HeIU5ThFqjVPVVe.sTuKqDQI6R0nrPz/Z8KqK8qXyxi3H7O", // Admin@123
            Phone = "+1234567890",
            Email = "admin@restaurant.com",
            RoleId = 1,
            BusinessId = 1,
            BranchId = 1,
            Salary = 0,
            ShiftType = ShiftType.Flexible,
            Status = UserStatus.Active,
            CreatedDate = DateTime.UtcNow
        };

        modelBuilder.Entity<User>().HasData(adminUser);
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
            entry.Entity.UpdatedDate = DateTime.UtcNow;
        }
    }

    private void ApplyTenantAssignments()
    {
        var businessId = _tenantContext.BusinessId ?? 1;
        var branchId = _tenantContext.BranchId ?? 1;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State != EntityState.Added || entry.Entity is Business)
                continue;

            if (entry.Entity.BusinessId <= 0)
                entry.Entity.BusinessId = businessId;

            if (entry.Entity.BranchId <= 0)
                entry.Entity.BranchId = branchId;
        }
    }

    private LambdaExpression BuildTenantFilter(Type clrType)
    {
        var parameter = Expression.Parameter(clrType, "e");
        var businessProperty = Expression.Property(parameter, nameof(BaseEntity.BusinessId));
        var branchProperty = Expression.Property(parameter, nameof(BaseEntity.BranchId));
        var isDeletedProperty = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var notDeleted = Expression.Equal(isDeletedProperty, Expression.Constant(false));

        var isSuperAdmin = _tenantContext.IsSuperAdmin;
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

            if (branchId.HasValue)
            {
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
}