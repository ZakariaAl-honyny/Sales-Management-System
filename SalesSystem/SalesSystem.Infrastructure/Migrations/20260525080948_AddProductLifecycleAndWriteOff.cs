using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductLifecycleAndWriteOff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CashTransactions");

            migrationBuilder.AlterColumn<decimal>(
                name: "DefaultTaxRate",
                table: "StoreSettings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseConversionFactor",
                table: "ProductUnits",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationDate",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "CashTransactions",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningBalance",
                table: "CashBoxes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "StockWriteOffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    WriteOffDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockWriteOffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockWriteOffs_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockWriteOffs_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockWriteOffs_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CHK_SalesInvoices_PaidAmount",
                table: "SalesInvoices",
                sql: "[PaidAmount] >= 0 AND [PaidAmount] <= [TotalAmount]");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_PurchaseInvoices_PaidAmount",
                table: "PurchaseInvoices",
                sql: "[PaidAmount] >= 0 AND [PaidAmount] <= [TotalAmount]");

            migrationBuilder.CreateIndex(
                name: "IX_StockWriteOffs_CreatedByUserId",
                table: "StockWriteOffs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockWriteOffs_ProductId",
                table: "StockWriteOffs",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockWriteOffs_WarehouseId",
                table: "StockWriteOffs",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockWriteOffs");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_SalesInvoices_PaidAmount",
                table: "SalesInvoices");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_PurchaseInvoices_PaidAmount",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "ExpirationDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OpeningBalance",
                table: "CashBoxes");

            migrationBuilder.AlterColumn<decimal>(
                name: "DefaultTaxRate",
                table: "StoreSettings",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseConversionFactor",
                table: "ProductUnits",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "CashTransactions",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "CashTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
