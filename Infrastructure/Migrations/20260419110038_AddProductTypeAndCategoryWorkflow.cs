using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductTypeAndCategoryWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModifiersJson",
                table: "OrderItems",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "OrderItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInventoryItem",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPurchasable",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecipeItem",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSaleable",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductType",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CategoryType",
                table: "MenuCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsInventoryItem",
                table: "InventoryItems",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPurchasable",
                table: "InventoryItems",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductType",
                table: "InventoryItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE MenuItems
                SET ProductType = 1,
                    IsSaleable = 1,
                    IsInventoryItem = 0,
                    IsRecipeItem = 0,
                    IsPurchasable = 0
            ");

            migrationBuilder.Sql(@"
                UPDATE MenuCategories
                SET CategoryType = 0
            ");

            migrationBuilder.Sql(@"
                UPDATE InventoryItems
                SET ProductType = 0,
                    IsInventoryItem = 1,
                    IsPurchasable = 1
            ");

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 11, 0, 35, 292, DateTimeKind.Utc).AddTicks(9337));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 11, 0, 35, 292, DateTimeKind.Utc).AddTicks(9498));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 11, 0, 35, 292, DateTimeKind.Utc).AddTicks(9533));

            migrationBuilder.CreateIndex(
                name: "idx_menuitem_branch_saleable_type",
                table: "MenuItems",
                columns: new[] { "BranchId", "IsSaleable", "ProductType" });

            migrationBuilder.CreateIndex(
                name: "idx_menucategory_branch_type",
                table: "MenuCategories",
                columns: new[] { "BranchId", "CategoryType" });

            migrationBuilder.CreateIndex(
                name: "idx_inventoryitem_branch_inventory_type",
                table: "InventoryItems",
                columns: new[] { "BranchId", "IsInventoryItem", "ProductType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_menuitem_branch_saleable_type",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "idx_menucategory_branch_type",
                table: "MenuCategories");

            migrationBuilder.DropIndex(
                name: "idx_inventoryitem_branch_inventory_type",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ModifiersJson",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "IsInventoryItem",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IsPurchasable",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IsRecipeItem",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IsSaleable",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "CategoryType",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "IsInventoryItem",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "IsPurchasable",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "InventoryItems");

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 18, 11, 23, 16, 244, DateTimeKind.Utc).AddTicks(9384));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 18, 11, 23, 16, 244, DateTimeKind.Utc).AddTicks(9726));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 18, 11, 23, 16, 244, DateTimeKind.Utc).AddTicks(9790));
        }
    }
}
