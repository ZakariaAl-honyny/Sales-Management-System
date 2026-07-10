using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase27PurchaseModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRate",
                table: "PurchaseReturns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "PurchaseReturns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "PurchaseReturns",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "CostInBaseCurrency",
                table: "PurchaseReturnLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "PurchaseInvoices",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostInBaseCurrency",
                table: "PurchaseInvoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRate",
                table: "PurchaseInvoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "PurchaseInvoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalFeesAmount",
                table: "PurchaseInvoiceLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CostInBaseCurrency",
                table: "PurchaseInvoiceLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "PurchaseInvoiceLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRate",
                table: "PurchaseInvoiceLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "PurchaseInvoiceLines",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountRate",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "CostInBaseCurrency",
                table: "PurchaseReturnLines");

            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "CostInBaseCurrency",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "DiscountRate",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "AdditionalFeesAmount",
                table: "PurchaseInvoiceLines");

            migrationBuilder.DropColumn(
                name: "CostInBaseCurrency",
                table: "PurchaseInvoiceLines");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "PurchaseInvoiceLines");

            migrationBuilder.DropColumn(
                name: "DiscountRate",
                table: "PurchaseInvoiceLines");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "PurchaseInvoiceLines");
        }
    }
}
