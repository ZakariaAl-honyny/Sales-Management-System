using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "CashBoxes");

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "SupplierPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "SupplierPayments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "SalesReturns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "SalesReturns",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "SalesInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "SalesInvoices",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "PurchaseReturns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "PurchaseReturns",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "PurchaseInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "PurchaseInvoices",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "CustomerPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "CustomerPayments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "CashTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "CashBoxes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExchangeRateToBase = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 1.0m),
                    IsBaseCurrency = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FractionName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                    table.CheckConstraint("CHK_Currencies_ExchangeRate", "[ExchangeRateToBase] > 0");
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRateHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    OldRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NewRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RateType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRateHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeRateHistories_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_CurrencyId",
                table: "SupplierPayments",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_CurrencyId",
                table: "SalesReturns",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_CurrencyId",
                table: "SalesInvoices",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_CurrencyId",
                table: "PurchaseReturns",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_CurrencyId",
                table: "PurchaseInvoices",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_CurrencyId",
                table: "CustomerPayments",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_CurrencyId",
                table: "CashTransactions",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxes_CurrencyId",
                table: "CashBoxes",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                table: "Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_IsBaseCurrency",
                table: "Currencies",
                column: "IsBaseCurrency",
                unique: true,
                filter: "[IsBaseCurrency] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Name",
                table: "Currencies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRateHistories_CurrencyId",
                table: "ExchangeRateHistories",
                column: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxes_Currencies_CurrencyId",
                table: "CashBoxes",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransactions_Currencies_CurrencyId",
                table: "CashTransactions",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPayments_Currencies_CurrencyId",
                table: "CustomerPayments",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_Currencies_CurrencyId",
                table: "PurchaseInvoices",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseReturns_Currencies_CurrencyId",
                table: "PurchaseReturns",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_Currencies_CurrencyId",
                table: "SalesInvoices",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesReturns_Currencies_CurrencyId",
                table: "SalesReturns",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_Currencies_CurrencyId",
                table: "SupplierPayments",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashBoxes_Currencies_CurrencyId",
                table: "CashBoxes");

            migrationBuilder.DropForeignKey(
                name: "FK_CashTransactions_Currencies_CurrencyId",
                table: "CashTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPayments_Currencies_CurrencyId",
                table: "CustomerPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoices_Currencies_CurrencyId",
                table: "PurchaseInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReturns_Currencies_CurrencyId",
                table: "PurchaseReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_Currencies_CurrencyId",
                table: "SalesInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesReturns_Currencies_CurrencyId",
                table: "SalesReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_Currencies_CurrencyId",
                table: "SupplierPayments");

            migrationBuilder.DropTable(
                name: "ExchangeRateHistories");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_CurrencyId",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SalesReturns_CurrencyId",
                table: "SalesReturns");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_CurrencyId",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseReturns_CurrencyId",
                table: "PurchaseReturns");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseInvoices_CurrencyId",
                table: "PurchaseInvoices");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPayments_CurrencyId",
                table: "CustomerPayments");

            migrationBuilder.DropIndex(
                name: "IX_CashTransactions_CurrencyId",
                table: "CashTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CashBoxes_CurrencyId",
                table: "CashBoxes");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "CashBoxes");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "CashBoxes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "SAR");
        }
    }
}
