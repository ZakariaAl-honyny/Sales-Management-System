using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "WarehouseTransfers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedAt",
                table: "WarehouseTransfers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedChargeAmount",
                table: "SalesReturns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedDiscountAmount",
                table: "SalesReturns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedTaxAmount",
                table: "SalesReturns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<short>(
                name: "TaxId",
                table: "SalesReturns",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedChargeAmount",
                table: "PurchaseReturns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedDiscountAmount",
                table: "PurchaseReturns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedTaxAmount",
                table: "PurchaseReturns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<short>(
                name: "TaxId",
                table: "PurchaseReturns",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "InventoryCounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedAt",
                table: "InventoryCounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "InventoryBatches",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "هل الدفعة مغلقة (تم استهلاكها بالكامل)");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_InventoryBatches_IsClosed_Consistency",
                table: "InventoryBatches",
                sql: "([IsClosed] = 0 AND [QuantityRemaining] > 0) OR ([IsClosed] = 1 AND [QuantityRemaining] <= 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_InventoryBatches_IsClosed_Consistency",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "WarehouseTransfers");

            migrationBuilder.DropColumn(
                name: "PostedAt",
                table: "WarehouseTransfers");

            migrationBuilder.DropColumn(
                name: "ReturnedChargeAmount",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "ReturnedDiscountAmount",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "ReturnedTaxAmount",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "ReturnedChargeAmount",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "ReturnedDiscountAmount",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "ReturnedTaxAmount",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "InventoryCounts");

            migrationBuilder.DropColumn(
                name: "PostedAt",
                table: "InventoryCounts");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "InventoryBatches");
        }
    }
}
