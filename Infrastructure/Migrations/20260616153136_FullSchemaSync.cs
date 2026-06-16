using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace POSSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FullSchemaSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Branches_BranchId",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Branches_BranchId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "idx_user_email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "idx_role_branchid_name",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "idx_customer_branch_phone",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "idx_customer_email",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "idx_customer_phone",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "ModifiedById",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "MenuItemVariants");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "MenuItemVariants");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "MenuItemAddons");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "MenuItemAddons");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "ModifiedByName",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "Branches");

            migrationBuilder.RenameIndex(
                name: "idx_table_branchid",
                table: "Tables",
                newName: "idx_tables_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_stockmovement_branchid",
                table: "StockMovements",
                newName: "idx_stockmovements_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_recipe_branchid",
                table: "Recipes",
                newName: "idx_recipes_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_payment_branchid",
                table: "Payments",
                newName: "idx_payments_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_order_branchid",
                table: "Orders",
                newName: "idx_orders_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_orderitem_branchid",
                table: "OrderItems",
                newName: "idx_orderitems_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_variant_branchid",
                table: "MenuItemVariants",
                newName: "idx_menuitemvariants_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_menuitem_branchid",
                table: "MenuItems",
                newName: "idx_menuitems_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_addon_branchid",
                table: "MenuItemAddons",
                newName: "idx_menuitemaddons_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_menucategory_branchid",
                table: "MenuCategories",
                newName: "idx_menucategories_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_inventoryitem_branchid",
                table: "InventoryItems",
                newName: "idx_inventoryitems_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_customer_branchid",
                table: "Customers",
                newName: "idx_customers_branchid");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                table: "Branches",
                newName: "CountryId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "Users",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "Tables",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "StockMovements",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "Permissions",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Roles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "Recipes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "MenuItemVariants",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "ProductType",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "SubCategoryId",
                table: "MenuItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "MenuItemAddons",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "MenuCategories",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "MenuCategories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "MenuCategories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "MenuCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "MenuCategories",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "Image",
                table: "MenuCategories",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "MenuCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "MenuCategories",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "MenuCategories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Status",
                table: "MenuCategories",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "InventoryItems",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Customers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Customers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "CNIC",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                table: "Customers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CustomerCode",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CustomerType",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsWalkIn",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningBalance",
                table: "Customers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "Status",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "Branches",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Branches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImageData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ImageContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImageFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Brands_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Businesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Logo = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    LogoFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LogoContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Businesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashFlowTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    ReferenceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashFlowTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashFlowTransactions_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashRegisters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegisterDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpeningCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosingCash = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExpectedCash = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ActualCash = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClosedBy = table.Column<int>(type: "int", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashRegisters_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodeSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    Prefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastNumber = table.Column<long>(type: "bigint", nullable: false),
                    ResetType = table.Column<int>(type: "int", nullable: false),
                    LastResetDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExchangeRateToPKR = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    IsBase = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseCategories_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Menus_Menus_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModuleKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentModuleId = table.Column<int>(type: "int", nullable: true),
                    Route = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Modules_Modules_ParentModuleId",
                        column: x => x.ParentModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Icon = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ImageData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ImageContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubCategories_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubCategories_MenuCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "MenuCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suppliers_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 1m),
                    Status = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Units_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserBranches",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBranches", x => new { x.UserId, x.BranchId });
                    table.ForeignKey(
                        name: "FK_UserBranches_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBranches_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warehouses_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseCategoryId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expenses_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expenses_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModuleForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    FormName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FormCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleForms_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ModuleId = table.Column<int>(type: "int", nullable: true),
                    ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CanView = table.Column<bool>(type: "bit", nullable: false),
                    CanCreate = table.Column<bool>(type: "bit", nullable: false),
                    CanEdit = table.Column<bool>(type: "bit", nullable: false),
                    CanDelete = table.Column<bool>(type: "bit", nullable: false),
                    CanExport = table.Column<bool>(type: "bit", nullable: false),
                    CanUpload = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    SubCategoryId = table.Column<int>(type: "int", nullable: true),
                    BrandId = table.Column<int>(type: "int", nullable: true),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WholesalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsVariantEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsDiscountAllowed = table.Column<bool>(type: "bit", nullable: false),
                    DiscountType = table.Column<int>(type: "int", nullable: true),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Products_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_MenuCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "MenuCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_SubCategories_SubCategoryId",
                        column: x => x.SubCategoryId,
                        principalTable: "SubCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Purchases_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Purchases_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Purchases_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    SaleDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReturnAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    CardAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PricingType = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HeldNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CashierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleInvoices_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleInvoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleInvoices_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleFormPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    FormId = table.Column<int>(type: "int", nullable: false),
                    CanView = table.Column<bool>(type: "bit", nullable: false),
                    CanCreate = table.Column<bool>(type: "bit", nullable: false),
                    CanEdit = table.Column<bool>(type: "bit", nullable: false),
                    CanDelete = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleFormPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleFormPermissions_ModuleForms_FormId",
                        column: x => x.FormId,
                        principalTable: "ModuleForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleFormPermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ImageData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsBaseUnit = table.Column<bool>(type: "bit", nullable: false),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    WholesalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductUnits_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Size = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AdditionalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostPriceOverride = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SellingPriceOverride = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductBarcodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductUnitId = table.Column<int>(type: "int", nullable: true),
                    ProductVariantId = table.Column<int>(type: "int", nullable: true),
                    BarcodeValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBarcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBarcodes_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalTable: "ProductUnits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductBarcodes_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductBarcodes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    VariantId = table.Column<int>(type: "int", nullable: true),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 1m),
                    BaseQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseItems_ProductUnits_UnitId",
                        column: x => x.UnitId,
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaleInvoiceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleInvoiceId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    VariantId = table.Column<int>(type: "int", nullable: true),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxPercent = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ItemNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleInvoiceItems_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleInvoiceItems_ProductUnits_UnitId",
                        column: x => x.UnitId,
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleInvoiceItems_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleInvoiceItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleInvoiceItems_SaleInvoices_SaleInvoiceId",
                        column: x => x.SaleInvoiceId,
                        principalTable: "SaleInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockLedger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    VariantId = table.Column<int>(type: "int", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    QuantityInBaseUnit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BusinessId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLedger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockLedger_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockLedger_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockLedger_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockLedger_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BusinessId", "CityId", "CountryId", "CreatedDate", "CreatedById", "Email", "Phone" },
                values: new object[] { 1, 5, 3, new DateTime(2026, 6, 16, 15, 31, 34, 487, DateTimeKind.Utc).AddTicks(1018), 1, "info@akhsoft.com", "+923432998052" });

            migrationBuilder.InsertData(
                table: "Businesses",
                columns: new[] { "Id", "Address", "CreatedDate", "CreatedById", "Currency", "Email", "IsActive", "IsDeleted", "LegalName", "Logo", "LogoContentType", "LogoFileName", "UpdatedDate", "ModifiedById", "Name", "Phone", "TaxNumber", "TimeZone" },
                values: new object[] { 1, "123 Main Street", new DateTime(2026, 6, 16, 15, 31, 34, 487, DateTimeKind.Utc).AddTicks(264), 1, "PKR", "info@akhsoft.com", true, false, "AKHSOFT", null, null, null, null, null, "AKHSOFT", "+923432998052", "NTN-0001", "Asia/Karachi" });

            migrationBuilder.InsertData(
                table: "Cities",
                columns: new[] { "Id", "CountryId", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, 1, true, "New York" },
                    { 2, 1, true, "Los Angeles" },
                    { 3, 2, true, "London" },
                    { 4, 2, true, "Manchester" },
                    { 5, 3, true, "Karachi" },
                    { 6, 3, true, "Lahore" },
                    { 7, 3, true, "Islamabad" },
                    { 8, 4, true, "Dubai" },
                    { 9, 4, true, "Abu Dhabi" }
                });

            migrationBuilder.InsertData(
                table: "Countries",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "US", true, "United States" },
                    { 2, "GB", true, "United Kingdom" },
                    { 3, "PK", true, "Pakistan" },
                    { 4, "AE", true, "United Arab Emirates" }
                });

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Id", "Code", "ExchangeRateToPKR", "IsActive", "IsBase", "Name", "Symbol" },
                values: new object[,]
                {
                    { 1, "PKR", 1m, true, true, "Pakistani Rupee", "₨" },
                    { 2, "USD", 278m, true, false, "US Dollar", "$" },
                    { 3, "GBP", 350m, true, false, "British Pound", "£" },
                    { 4, "AED", 75.7m, true, false, "UAE Dirham", "د.إ" },
                    { 5, "EUR", 300m, true, false, "Euro", "€" }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedDate", "Description", "IsActive", "Name", "Permissions" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Full system access", true, "System Admin", "all" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedDate", "Description", "IsActive", "IsDeleted", "Name", "Permissions", "UpdatedDate" },
                values: new object[,]
                {
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "All branches access", true, false, "Super Admin", "all", null },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Branch-level management", true, false, "Admin", "branch_admin", null },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Operations control", true, false, "Manager", "operations", null },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "POS billing access", true, false, "Cashier", "pos", null }
                });

            migrationBuilder.InsertData(
                table: "UserBranches",
                columns: new[] { "BranchId", "UserId" },
                values: new object[] { 1, 1 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BusinessId", "CreatedDate", "CreatedById", "DeletedAt", "Email", "FullName", "IsActive", "PasswordHash", "Phone", "Username" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, "info@infoakhsoft.com", "Muhammad Akmal", true, "$2a$11$W7Mi6nl3DiHePG3yDxDRv.VuEY5uE2Jfa2VizDS.h78g1bjFEYuuu", "+923432998052", "makmal" });

            migrationBuilder.CreateIndex(
                name: "idx_user_businessid",
                table: "Users",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_user_email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_tables_business_branch",
                table: "Tables",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_tables_businessid",
                table: "Tables",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_stockmovements_business_branch",
                table: "StockMovements",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_stockmovements_businessid",
                table: "StockMovements",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_role_name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_recipes_business_branch",
                table: "Recipes",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_recipes_businessid",
                table: "Recipes",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_payments_business_branch",
                table: "Payments",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_payments_businessid",
                table: "Payments",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_orders_business_branch",
                table: "Orders",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_orders_businessid",
                table: "Orders",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_orderitems_business_branch",
                table: "OrderItems",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_orderitems_businessid",
                table: "OrderItems",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_menuitemvariants_business_branch",
                table: "MenuItemVariants",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_menuitemvariants_businessid",
                table: "MenuItemVariants",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_menuitem_subcategoryid",
                table: "MenuItems",
                column: "SubCategoryId");

            migrationBuilder.CreateIndex(
                name: "idx_menuitems_business_branch",
                table: "MenuItems",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_menuitems_businessid",
                table: "MenuItems",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_menuitemaddons_business_branch",
                table: "MenuItemAddons",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_menuitemaddons_businessid",
                table: "MenuItemAddons",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_menucategories_business_branch",
                table: "MenuCategories",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_menucategories_businessid",
                table: "MenuCategories",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_menucategory_branch_code",
                table: "MenuCategories",
                columns: new[] { "BranchId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL AND [Code] <> ''");

            migrationBuilder.CreateIndex(
                name: "idx_inventoryitems_business_branch",
                table: "InventoryItems",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_inventoryitems_businessid",
                table: "InventoryItems",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_customer_branch_code",
                table: "Customers",
                columns: new[] { "BusinessId", "BranchId", "CustomerCode" },
                unique: true,
                filter: "[CustomerCode] <> '' AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "idx_customer_branch_phone_unique",
                table: "Customers",
                columns: new[] { "BusinessId", "BranchId", "Phone" },
                unique: true,
                filter: "[Phone] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "idx_customer_cityid",
                table: "Customers",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "idx_customer_countryid",
                table: "Customers",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "idx_customer_walkin",
                table: "Customers",
                columns: new[] { "BusinessId", "BranchId", "IsWalkIn" });

            migrationBuilder.CreateIndex(
                name: "idx_customers_business_branch",
                table: "Customers",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_customers_businessid",
                table: "Customers",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_branch_cityid",
                table: "Branches",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "idx_branch_countryid",
                table: "Branches",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "idx_branches_businessid",
                table: "Branches",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_brand_branch_name",
                table: "Brands",
                columns: new[] { "BranchId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_brands_branchid",
                table: "Brands",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_brands_business_branch",
                table: "Brands",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_brands_businessid",
                table: "Brands",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_business_email",
                table: "Businesses",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "idx_business_name",
                table: "Businesses",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "idx_cashflowtransactions_branchid",
                table: "CashFlowTransactions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_cashflowtransactions_business_branch",
                table: "CashFlowTransactions",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_cashflowtransactions_businessid",
                table: "CashFlowTransactions",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowTransactions_BusinessId_BranchId_TransactionDate",
                table: "CashFlowTransactions",
                columns: new[] { "BusinessId", "BranchId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowTransactions_BusinessId_BranchId_TransactionType",
                table: "CashFlowTransactions",
                columns: new[] { "BusinessId", "BranchId", "TransactionType" });

            migrationBuilder.CreateIndex(
                name: "idx_cashregisters_branchid",
                table: "CashRegisters",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_cashregisters_business_branch",
                table: "CashRegisters",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_cashregisters_businessid",
                table: "CashRegisters",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_BusinessId_BranchId_RegisterDate",
                table: "CashRegisters",
                columns: new[] { "BusinessId", "BranchId", "RegisterDate" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "idx_city_country_name",
                table: "Cities",
                columns: new[] { "CountryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_city_countryid",
                table: "Cities",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "idx_codesequence_module_branch",
                table: "CodeSequences",
                columns: new[] { "ModuleName", "BranchId" },
                unique: true,
                filter: "[BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_country_code",
                table: "Countries",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_currency_code",
                table: "Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_expensecategories_branchid",
                table: "ExpenseCategories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_expensecategories_business_branch",
                table: "ExpenseCategories",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_expensecategories_businessid",
                table: "ExpenseCategories",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_expensecategory_branch_name",
                table: "ExpenseCategories",
                columns: new[] { "BusinessId", "BranchId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "idx_expenses_branchid",
                table: "Expenses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_expenses_business_branch",
                table: "Expenses",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_expenses_businessid",
                table: "Expenses",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BusinessId_BranchId_ExpenseCategoryId",
                table: "Expenses",
                columns: new[] { "BusinessId", "BranchId", "ExpenseCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BusinessId_BranchId_ExpenseDate",
                table: "Expenses",
                columns: new[] { "BusinessId", "BranchId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseCategoryId",
                table: "Expenses",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "idx_menus_displayorder",
                table: "Menus",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "idx_menus_parentid",
                table: "Menus",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "idx_module_form_code",
                table: "ModuleForms",
                column: "FormCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleForms_ModuleId",
                table: "ModuleForms",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "idx_module_key",
                table: "Modules",
                column: "ModuleKey",
                unique: true,
                filter: "[ModuleKey] <> '' AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_ParentModuleId",
                table: "Modules",
                column: "ParentModuleId");

            migrationBuilder.CreateIndex(
                name: "idx_productbarcode_value",
                table: "ProductBarcodes",
                column: "BarcodeValue",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "idx_productbarcodes_branchid",
                table: "ProductBarcodes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_productbarcodes_business_branch",
                table: "ProductBarcodes",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_productbarcodes_businessid",
                table: "ProductBarcodes",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBarcodes_ProductId",
                table: "ProductBarcodes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBarcodes_ProductUnitId",
                table: "ProductBarcodes",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBarcodes_ProductVariantId",
                table: "ProductBarcodes",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "idx_productimage_product_primary",
                table: "ProductImages",
                columns: new[] { "ProductId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "idx_productimages_branchid",
                table: "ProductImages",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_productimages_business_branch",
                table: "ProductImages",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_productimages_businessid",
                table: "ProductImages",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_product_business_branch_category",
                table: "Products",
                columns: new[] { "BusinessId", "BranchId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "idx_product_business_branch_code",
                table: "Products",
                columns: new[] { "BusinessId", "BranchId", "ProductCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_product_business_branch_sku",
                table: "Products",
                columns: new[] { "BusinessId", "BranchId", "SKU" });

            migrationBuilder.CreateIndex(
                name: "idx_products_branchid",
                table: "Products",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_products_business_branch",
                table: "Products",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_products_businessid",
                table: "Products",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandId",
                table: "Products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SubCategoryId",
                table: "Products",
                column: "SubCategoryId");

            migrationBuilder.CreateIndex(
                name: "idx_productunit_product_name",
                table: "ProductUnits",
                columns: new[] { "ProductId", "UnitName" });

            migrationBuilder.CreateIndex(
                name: "idx_productunits_branchid",
                table: "ProductUnits",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_productunits_business_branch",
                table: "ProductUnits",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_productunits_businessid",
                table: "ProductUnits",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_productvariant_product_sku",
                table: "ProductVariants",
                columns: new[] { "ProductId", "SKU" });

            migrationBuilder.CreateIndex(
                name: "idx_productvariants_branchid",
                table: "ProductVariants",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_productvariants_business_branch",
                table: "ProductVariants",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_productvariants_businessid",
                table: "ProductVariants",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_purchaseitems_branchid",
                table: "PurchaseItems",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_purchaseitems_business_branch",
                table: "PurchaseItems",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_purchaseitems_businessid",
                table: "PurchaseItems",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_ProductId",
                table: "PurchaseItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_PurchaseId",
                table: "PurchaseItems",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_UnitId",
                table: "PurchaseItems",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_VariantId",
                table: "PurchaseItems",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_business_branch_invoice",
                table: "Purchases",
                columns: new[] { "BusinessId", "BranchId", "InvoiceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_purchases_branchid",
                table: "Purchases",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_purchases_business_branch",
                table: "Purchases",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_purchases_businessid",
                table: "Purchases",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_SupplierId",
                table: "Purchases",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_WarehouseId",
                table: "Purchases",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "idx_role_form_permission",
                table: "RoleFormPermissions",
                columns: new[] { "RoleId", "FormId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleFormPermissions_FormId",
                table: "RoleFormPermissions",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "idx_rolepermission_role_module",
                table: "RolePermissions",
                columns: new[] { "RoleId", "ModuleName" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_ModuleId",
                table: "RolePermissions",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "idx_saleinvoiceitems_branchid",
                table: "SaleInvoiceItems",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_saleinvoiceitems_business_branch",
                table: "SaleInvoiceItems",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_saleinvoiceitems_businessid",
                table: "SaleInvoiceItems",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoiceItems_ProductId",
                table: "SaleInvoiceItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoiceItems_SaleInvoiceId",
                table: "SaleInvoiceItems",
                column: "SaleInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoiceItems_UnitId",
                table: "SaleInvoiceItems",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoiceItems_VariantId",
                table: "SaleInvoiceItems",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "idx_saleinvoices_branchid",
                table: "SaleInvoices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_saleinvoices_business_branch",
                table: "SaleInvoices",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_saleinvoices_businessid",
                table: "SaleInvoices",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_BusinessId_BranchId_InvoiceNo",
                table: "SaleInvoices",
                columns: new[] { "BusinessId", "BranchId", "InvoiceNo" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_BusinessId_BranchId_SaleDate",
                table: "SaleInvoices",
                columns: new[] { "BusinessId", "BranchId", "SaleDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_BusinessId_BranchId_Status",
                table: "SaleInvoices",
                columns: new[] { "BusinessId", "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_CustomerId",
                table: "SaleInvoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoices_WarehouseId",
                table: "SaleInvoices",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "idx_ledger_business_branch_date",
                table: "StockLedger",
                columns: new[] { "BusinessId", "BranchId", "Date" });

            migrationBuilder.CreateIndex(
                name: "idx_ledger_business_branch_product_variant_warehouse",
                table: "StockLedger",
                columns: new[] { "BusinessId", "BranchId", "ProductId", "VariantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "idx_ledger_business_branch_product_warehouse",
                table: "StockLedger",
                columns: new[] { "BusinessId", "BranchId", "ProductId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "idx_stockledger_branchid",
                table: "StockLedger",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_stockledger_business_branch",
                table: "StockLedger",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_stockledger_businessid",
                table: "StockLedger",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedger_ProductId",
                table: "StockLedger",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedger_VariantId",
                table: "StockLedger",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedger_WarehouseId",
                table: "StockLedger",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "idx_subcategories_branchid",
                table: "SubCategories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_subcategories_business_branch",
                table: "SubCategories",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_subcategories_businessid",
                table: "SubCategories",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_subcategory_branch_category",
                table: "SubCategories",
                columns: new[] { "BranchId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "idx_subcategory_branch_code",
                table: "SubCategories",
                columns: new[] { "BranchId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL AND [Code] <> '' AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "idx_subcategory_category_name",
                table: "SubCategories",
                columns: new[] { "CategoryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_supplier_branch_code",
                table: "Suppliers",
                columns: new[] { "BusinessId", "BranchId", "SupplierCode" },
                unique: true,
                filter: "[SupplierCode] <> '' AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "idx_supplier_business_branch_name",
                table: "Suppliers",
                columns: new[] { "BusinessId", "BranchId", "Name" });

            migrationBuilder.CreateIndex(
                name: "idx_suppliers_branchid",
                table: "Suppliers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_suppliers_business_branch",
                table: "Suppliers",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_suppliers_businessid",
                table: "Suppliers",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_unit_business_branch_code",
                table: "Units",
                columns: new[] { "BusinessId", "BranchId", "Code" });

            migrationBuilder.CreateIndex(
                name: "idx_unit_business_branch_name",
                table: "Units",
                columns: new[] { "BusinessId", "BranchId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_units_branchid",
                table: "Units",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_units_business_branch",
                table: "Units",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_units_businessid",
                table: "Units",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "idx_userbranch_branchid",
                table: "UserBranches",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_userbranch_userid",
                table: "UserBranches",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_warehouse_business_branch_name",
                table: "Warehouses",
                columns: new[] { "BusinessId", "BranchId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_warehouses_branchid",
                table: "Warehouses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_warehouses_business_branch",
                table: "Warehouses",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_warehouses_businessid",
                table: "Warehouses",
                column: "BusinessId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_SubCategories_SubCategoryId",
                table: "MenuItems",
                column: "SubCategoryId",
                principalTable: "SubCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Businesses_BusinessId",
                table: "Users",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_SubCategories_SubCategoryId",
                table: "MenuItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Businesses_BusinessId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Businesses");

            migrationBuilder.DropTable(
                name: "CashFlowTransactions");

            migrationBuilder.DropTable(
                name: "CashRegisters");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "CodeSequences");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DropTable(
                name: "ProductBarcodes");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "PurchaseItems");

            migrationBuilder.DropTable(
                name: "RoleFormPermissions");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SaleInvoiceItems");

            migrationBuilder.DropTable(
                name: "StockLedger");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "UserBranches");

            migrationBuilder.DropTable(
                name: "ExpenseCategories");

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "ModuleForms");

            migrationBuilder.DropTable(
                name: "ProductUnits");

            migrationBuilder.DropTable(
                name: "SaleInvoices");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "SubCategories");

            migrationBuilder.DropIndex(
                name: "idx_user_businessid",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "idx_user_email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "idx_tables_business_branch",
                table: "Tables");

            migrationBuilder.DropIndex(
                name: "idx_tables_businessid",
                table: "Tables");

            migrationBuilder.DropIndex(
                name: "idx_stockmovements_business_branch",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "idx_stockmovements_businessid",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "idx_role_name",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "idx_recipes_business_branch",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "idx_recipes_businessid",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "idx_payments_business_branch",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "idx_payments_businessid",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "idx_orders_business_branch",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "idx_orders_businessid",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "idx_orderitems_business_branch",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "idx_orderitems_businessid",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "idx_menuitemvariants_business_branch",
                table: "MenuItemVariants");

            migrationBuilder.DropIndex(
                name: "idx_menuitemvariants_businessid",
                table: "MenuItemVariants");

            migrationBuilder.DropIndex(
                name: "idx_menuitem_subcategoryid",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "idx_menuitems_business_branch",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "idx_menuitems_businessid",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "idx_menuitemaddons_business_branch",
                table: "MenuItemAddons");

            migrationBuilder.DropIndex(
                name: "idx_menuitemaddons_businessid",
                table: "MenuItemAddons");

            migrationBuilder.DropIndex(
                name: "idx_menucategories_business_branch",
                table: "MenuCategories");

            migrationBuilder.DropIndex(
                name: "idx_menucategories_businessid",
                table: "MenuCategories");

            migrationBuilder.DropIndex(
                name: "idx_menucategory_branch_code",
                table: "MenuCategories");

            migrationBuilder.DropIndex(
                name: "idx_inventoryitems_business_branch",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "idx_inventoryitems_businessid",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "idx_customer_branch_code",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "idx_customer_branch_phone_unique",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "idx_customer_cityid",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "idx_customer_countryid",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "idx_customer_walkin",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "idx_customers_business_branch",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "idx_customers_businessid",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "idx_branch_cityid",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "idx_branch_countryid",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "idx_branches_businessid",
                table: "Branches");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "MenuItemVariants");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "SubCategoryId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "MenuItemAddons");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CNIC",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerType",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsWalkIn",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "OpeningBalance",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Branches");

            migrationBuilder.RenameIndex(
                name: "idx_tables_branchid",
                table: "Tables",
                newName: "idx_table_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_stockmovements_branchid",
                table: "StockMovements",
                newName: "idx_stockmovement_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_recipes_branchid",
                table: "Recipes",
                newName: "idx_recipe_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_payments_branchid",
                table: "Payments",
                newName: "idx_payment_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_orders_branchid",
                table: "Orders",
                newName: "idx_order_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_orderitems_branchid",
                table: "OrderItems",
                newName: "idx_orderitem_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_menuitemvariants_branchid",
                table: "MenuItemVariants",
                newName: "idx_variant_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_menuitems_branchid",
                table: "MenuItems",
                newName: "idx_menuitem_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_menuitemaddons_branchid",
                table: "MenuItemAddons",
                newName: "idx_addon_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_menucategories_branchid",
                table: "MenuCategories",
                newName: "idx_menucategory_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_inventoryitems_branchid",
                table: "InventoryItems",
                newName: "idx_inventoryitem_branchid");

            migrationBuilder.RenameIndex(
                name: "idx_customers_branchid",
                table: "Customers",
                newName: "idx_customer_branchid");

            migrationBuilder.RenameColumn(
                name: "CountryId",
                table: "Branches",
                newName: "BranchId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Tables",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "Tables",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Permissions",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Roles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Roles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedById",
                table: "Roles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Recipes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "Recipes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "MenuItemVariants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "MenuItemVariants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductType",
                table: "MenuItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "MenuItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "MenuItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "MenuItemAddons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "MenuItemAddons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "MenuCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "MenuCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Customers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Branches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Branches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Branches",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByName",
                table: "Branches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "Branches",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BranchId", "City", "CreatedById", "CreatedByName", "CreatedDate", "Currency", "Email", "ModifiedByName", "Phone", "TaxRate" },
                values: new object[] { 1, "Default City", null, null, new DateTime(2026, 4, 19, 11, 0, 35, 292, DateTimeKind.Utc).AddTicks(9337), "USD", "main@restaurant.com", null, "+1234567890", 10.00m });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BranchId", "CreatedById", "CreatedByName", "CreatedDate", "ModifiedById", "ModifiedByName", "Name", "Permissions" },
                values: new object[] { 1, null, null, new DateTime(2026, 4, 19, 11, 0, 35, 292, DateTimeKind.Utc).AddTicks(9498), null, null, "Admin", "all_permissions" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedById", "CreatedByName", "CreatedDate", "Email", "FullName", "ModifiedByName", "PasswordHash", "Phone", "Username" },
                values: new object[] { null, null, new DateTime(2026, 4, 19, 11, 0, 35, 292, DateTimeKind.Utc).AddTicks(9533), "admin@restaurant.com", "System Administrator", null, "$2a$11$QvHz8.HeIU5ThFqjVPVVe.sTuKqDQI6R0nrPz/Z8KqK8qXyxi3H7O", "+1234567890", "admin" });

            migrationBuilder.CreateIndex(
                name: "idx_user_email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "idx_role_branchid_name",
                table: "Roles",
                columns: new[] { "BranchId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_customer_branch_phone",
                table: "Customers",
                columns: new[] { "BranchId", "Phone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_customer_email",
                table: "Customers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "idx_customer_phone",
                table: "Customers",
                column: "Phone");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Branches_BranchId",
                table: "Roles",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Branches_BranchId",
                table: "Users",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
