using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase25_ProductEntityCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Units_RetailUnitId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Units_UnitId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Units_WholesaleUnitId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_RetailUnitId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_UnitId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_WholesaleUnitId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LastPurchasePrice",
                table: "ProductUnits");

            migrationBuilder.DropColumn(
                name: "PurchaseCost",
                table: "ProductUnits");

            migrationBuilder.DropColumn(
                name: "SalesPrice",
                table: "ProductUnits");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ProductUnits");

            migrationBuilder.DropColumn(
                name: "SupplierPrice",
                table: "ProductUnits");

            migrationBuilder.DropColumn(
                name: "UnitName",
                table: "ProductUnits");

            migrationBuilder.DropColumn(
                name: "WholesalePrice",
                table: "ProductUnits");

            migrationBuilder.DropColumn(
                name: "ConversionFactor",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RetailUnitId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WholesaleUnitId",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "AvgCost",
                table: "Products",
                newName: "Cost");

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "ProductUnits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnits_UnitId",
                table: "ProductUnits",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductUnits_Units_UnitId",
                table: "ProductUnits",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductUnits_Units_UnitId",
                table: "ProductUnits");

            migrationBuilder.DropIndex(
                name: "IX_ProductUnits_UnitId",
                table: "ProductUnits");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "ProductUnits");

            migrationBuilder.RenameColumn(
                name: "Cost",
                table: "Products",
                newName: "AvgCost");

            migrationBuilder.AddColumn<decimal>(
                name: "LastPurchasePrice",
                table: "ProductUnits",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseCost",
                table: "ProductUnits",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SalesPrice",
                table: "ProductUnits",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ProductUnits",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "SupplierPrice",
                table: "ProductUnits",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UnitName",
                table: "ProductUnits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "WholesalePrice",
                table: "ProductUnits",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ConversionFactor",
                table: "Products",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetailUnitId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WholesaleUnitId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_RetailUnitId",
                table: "Products",
                column: "RetailUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_UnitId",
                table: "Products",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_WholesaleUnitId",
                table: "Products",
                column: "WholesaleUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Units_RetailUnitId",
                table: "Products",
                column: "RetailUnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Units_UnitId",
                table: "Products",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Units_WholesaleUnitId",
                table: "Products",
                column: "WholesaleUnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
