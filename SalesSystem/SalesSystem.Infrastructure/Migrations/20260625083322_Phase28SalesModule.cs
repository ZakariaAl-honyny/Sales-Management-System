using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase28SalesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "SalesReturns",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "SalesReturns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReturnReason",
                table: "SalesReturns",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostInBaseCurrency",
                table: "SalesReturnLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostInBaseCurrency",
                table: "SalesInvoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRate",
                table: "SalesInvoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "SalesInvoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CostInBaseCurrency",
                table: "SalesInvoiceLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "SalesInvoiceLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRate",
                table: "SalesInvoiceLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "SalesInvoiceLines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ProfitAmount",
                table: "SalesInvoiceLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "SalesInvoiceLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "ReturnReason",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "CostInBaseCurrency",
                table: "SalesReturnLines");

            migrationBuilder.DropColumn(
                name: "CostInBaseCurrency",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "DiscountRate",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "CostInBaseCurrency",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "DiscountRate",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "ProfitAmount",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "SalesInvoiceLines");
        }
    }
}
