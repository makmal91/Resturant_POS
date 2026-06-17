using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductStockFieldsAndLowStockAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowNegativeStock",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableLowStockAlert",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LowStockAlertLevel",
                table: "Products",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningStock",
                table: "Products",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "LowStockAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    VariantId = table.Column<int>(type: "int", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    CurrentStock = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AlertLevel = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastTriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_LowStockAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LowStockAlerts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LowStockAlerts_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LowStockAlerts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LowStockAlerts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 6, 17, 11, 39, 16, 891, DateTimeKind.Utc).AddTicks(4398));

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 6, 17, 11, 39, 16, 891, DateTimeKind.Utc).AddTicks(3759));

            migrationBuilder.CreateIndex(
                name: "idx_lowstockalert_product_variant_warehouse",
                table: "LowStockAlerts",
                columns: new[] { "BusinessId", "BranchId", "ProductId", "VariantId", "WarehouseId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "idx_lowstockalerts_branchid",
                table: "LowStockAlerts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "idx_lowstockalerts_business_branch",
                table: "LowStockAlerts",
                columns: new[] { "BusinessId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "idx_lowstockalerts_businessid",
                table: "LowStockAlerts",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_LowStockAlerts_ProductId",
                table: "LowStockAlerts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_LowStockAlerts_VariantId",
                table: "LowStockAlerts",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_LowStockAlerts_WarehouseId",
                table: "LowStockAlerts",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LowStockAlerts");

            migrationBuilder.DropColumn(
                name: "AllowNegativeStock",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "EnableLowStockAlert",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LowStockAlertLevel",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OpeningStock",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 6, 17, 4, 26, 32, 837, DateTimeKind.Utc).AddTicks(9008));

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 6, 17, 4, 26, 32, 837, DateTimeKind.Utc).AddTicks(8483));
        }
    }
}
