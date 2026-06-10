using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase25_RemoveUnitBarcode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnitBarcodes");

            migrationBuilder.DropIndex(
                name: "IX_ProductPrices_ProductUnit_Currency_Level_Date",
                table: "ProductPrices");

            migrationBuilder.DropColumn(
                name: "PriceLevel",
                table: "ProductPrices");

            migrationBuilder.RenameColumn(
                name: "MinStock",
                table: "Products",
                newName: "MinStockLevel");

            migrationBuilder.AddColumn<decimal>(
                name: "AvgCost",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasExpiry",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TrackBatches",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DecimalPlaces",
                table: "Currencies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_ProductUnit_Currency_Date",
                table: "ProductPrices",
                columns: new[] { "ProductUnitId", "CurrencyId", "EffectiveFrom" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductPrices_ProductUnit_Currency_Date",
                table: "ProductPrices");

            migrationBuilder.DropColumn(
                name: "AvgCost",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HasExpiry",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TrackBatches",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DecimalPlaces",
                table: "Currencies");

            migrationBuilder.RenameColumn(
                name: "MinStockLevel",
                table: "Products",
                newName: "MinStock");

            migrationBuilder.AddColumn<int>(
                name: "PriceLevel",
                table: "ProductPrices",
                type: "int",
                nullable: false,
                defaultValue: 1,
                comment: "مستوى السعر: 1=تجزئة, 2=جملة, 3=VIP, 4=موزع");

            migrationBuilder.CreateTable(
                name: "UnitBarcodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductUnitId = table.Column<int>(type: "int", nullable: false),
                    BarcodeValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SupplierCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitBarcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitBarcodes_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_ProductUnit_Currency_Level_Date",
                table: "ProductPrices",
                columns: new[] { "ProductUnitId", "CurrencyId", "PriceLevel", "EffectiveFrom" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_UnitBarcodes_BarcodeValue",
                table: "UnitBarcodes",
                column: "BarcodeValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitBarcodes_ProductUnitId",
                table: "UnitBarcodes",
                column: "ProductUnitId");
        }
    }
}
