using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixModuleKeyUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_module_key",
                table: "Modules");

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 6, 16, 15, 43, 46, 146, DateTimeKind.Utc).AddTicks(4665));

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 6, 16, 15, 43, 46, 146, DateTimeKind.Utc).AddTicks(3814));

            migrationBuilder.CreateIndex(
                name: "idx_module_key",
                table: "Modules",
                column: "ModuleKey",
                unique: true,
                filter: "[ModuleKey] <> '' AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_module_key",
                table: "Modules");

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 6, 16, 15, 31, 34, 487, DateTimeKind.Utc).AddTicks(1018));

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 6, 16, 15, 31, 34, 487, DateTimeKind.Utc).AddTicks(264));

            migrationBuilder.CreateIndex(
                name: "idx_module_key",
                table: "Modules",
                column: "ModuleKey",
                unique: true);
        }
    }
}
