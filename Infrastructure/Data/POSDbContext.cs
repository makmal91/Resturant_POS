using Microsoft.EntityFrameworkCore;
using POSSystem.Domain;
using POSSystem.Infrastructure.Configurations;

namespace POSSystem.Infrastructure.Data;

public class POSDbContext : DbContext
{
    public POSDbContext(DbContextOptions<POSDbContext> options) : base(options)
    {
    }

    #region DbSets
    public DbSet<Branch> Branches { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<MenuCategory> MenuCategories { get; set; } = null!;
    public DbSet<MenuItem> MenuItems { get; set; } = null!;
    public DbSet<MenuItemVariant> MenuItemVariants { get; set; } = null!;
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
        modelBuilder.ApplyConfiguration(new BranchConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new MenuCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new MenuItemConfiguration());
        modelBuilder.ApplyConfiguration(new MenuItemVariantConfiguration());
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
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<DateTime>("CreatedDate")
                    .HasDefaultValueSql("GETUTCDATE()");
            }
        }

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Default Branch
        var defaultBranch = new Branch
        {
            Id = 1,
            Name = "Main Branch",
            Code = "MAIN",
            Address = "123 Main Street",
            City = "Default City",
            Phone = "+1234567890",
            Email = "main@restaurant.com",
            OpeningTime = new TimeSpan(11, 0, 0),
            ClosingTime = new TimeSpan(22, 0, 0),
            TaxRate = 10.00m,
            Currency = "USD",
            IsActive = true,
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
            BranchId = 1,
            Salary = 0,
            ShiftType = ShiftType.Flexible,
            Status = UserStatus.Active,
            CreatedDate = DateTime.UtcNow
        };

        modelBuilder.Entity<User>().HasData(adminUser);
    }
}