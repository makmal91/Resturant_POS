using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOpeningStockVariantWise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.Products', N'OpeningStockVariantWise') IS NULL
                    ALTER TABLE [Products] ADD [OpeningStockVariantWise] bit NOT NULL CONSTRAINT [DF_Products_OpeningStockVariantWise] DEFAULT CAST(0 AS bit);
                """);

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 6, 17, 11, 51, 17, 596, DateTimeKind.Utc).AddTicks(4870));

            migrationBuilder.UpdateData(
                table: "Businesses",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 6, 17, 11, 51, 17, 596, DateTimeKind.Utc).AddTicks(3985));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.Products', N'OpeningStockVariantWise') IS NOT NULL
                BEGIN
                    DECLARE @dfName NVARCHAR(128);
                    SELECT @dfName = dc.name
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
                    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Products')
                      AND c.name = N'OpeningStockVariantWise';
                    IF @dfName IS NOT NULL
                        EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @dfName + N']');
                    ALTER TABLE [Products] DROP COLUMN [OpeningStockVariantWise];
                END
                """);

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
        }
    }
}
