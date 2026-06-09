using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase29_ReceiptsPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPayments_Currencies_CurrencyId",
                table: "CustomerPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPayments_SalesInvoices_SalesInvoiceId",
                table: "CustomerPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_Currencies_CurrencyId",
                table: "SupplierPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_PurchaseInvoices_PurchaseInvoiceId",
                table: "SupplierPayments");

            migrationBuilder.RenameColumn(
                name: "ClosingBalance",
                table: "DailyClosures",
                newName: "ExpectedClosingBalance");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNo",
                table: "SupplierPayments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentNo",
                table: "SupplierPayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "PaymentMethod",
                table: "SupplierPayments",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "SupplierPayments",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<int>(
                name: "CashBoxId",
                table: "SupplierPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualCashCount",
                table: "DailyClosures",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Difference",
                table: "DailyClosures",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsReconciled",
                table: "DailyClosures",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "DailyClosures",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNo",
                table: "CustomerPayments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentNo",
                table: "CustomerPayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "PaymentMethod",
                table: "CustomerPayments",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "CustomerPayments",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<int>(
                name: "CashBoxId",
                table: "CustomerPayments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cheques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChequeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaturityDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CustomerPaymentId = table.Column<int>(type: "int", nullable: true),
                    SupplierPaymentId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cheques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cheques_CustomerPayments_CustomerPaymentId",
                        column: x => x.CustomerPaymentId,
                        principalTable: "CustomerPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cheques_SupplierPayments_SupplierPaymentId",
                        column: x => x.SupplierPaymentId,
                        principalTable: "SupplierPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerPaymentId = table.Column<int>(type: "int", nullable: true),
                    SupplierPaymentId = table.Column<int>(type: "int", nullable: true),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    InvoiceType = table.Column<byte>(type: "tinyint", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_CustomerPayments_CustomerPaymentId",
                        column: x => x.CustomerPaymentId,
                        principalTable: "CustomerPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_SupplierPayments_SupplierPaymentId",
                        column: x => x.SupplierPaymentId,
                        principalTable: "SupplierPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_CashBoxId",
                table: "SupplierPayments",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_CashBoxId",
                table: "CustomerPayments",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_Cheques_ChequeNumber",
                table: "Cheques",
                column: "ChequeNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Cheques_CustomerPaymentId",
                table: "Cheques",
                column: "CustomerPaymentId",
                unique: true,
                filter: "[CustomerPaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Cheques_SupplierPaymentId",
                table: "Cheques",
                column: "SupplierPaymentId",
                unique: true,
                filter: "[SupplierPaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_CustomerPaymentId_InvoiceId",
                table: "PaymentAllocations",
                columns: new[] { "CustomerPaymentId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_SupplierPaymentId_InvoiceId",
                table: "PaymentAllocations",
                columns: new[] { "SupplierPaymentId", "InvoiceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPayments_CashBoxes_CashBoxId",
                table: "CustomerPayments",
                column: "CashBoxId",
                principalTable: "CashBoxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPayments_Currencies_CurrencyId",
                table: "CustomerPayments",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPayments_SalesInvoices_SalesInvoiceId",
                table: "CustomerPayments",
                column: "SalesInvoiceId",
                principalTable: "SalesInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_CashBoxes_CashBoxId",
                table: "SupplierPayments",
                column: "CashBoxId",
                principalTable: "CashBoxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_Currencies_CurrencyId",
                table: "SupplierPayments",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_PurchaseInvoices_PurchaseInvoiceId",
                table: "SupplierPayments",
                column: "PurchaseInvoiceId",
                principalTable: "PurchaseInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPayments_CashBoxes_CashBoxId",
                table: "CustomerPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPayments_Currencies_CurrencyId",
                table: "CustomerPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPayments_SalesInvoices_SalesInvoiceId",
                table: "CustomerPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_CashBoxes_CashBoxId",
                table: "SupplierPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_Currencies_CurrencyId",
                table: "SupplierPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_PurchaseInvoices_PurchaseInvoiceId",
                table: "SupplierPayments");

            migrationBuilder.DropTable(
                name: "Cheques");

            migrationBuilder.DropTable(
                name: "PaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_CashBoxId",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPayments_CashBoxId",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "CashBoxId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "ActualCashCount",
                table: "DailyClosures");

            migrationBuilder.DropColumn(
                name: "Difference",
                table: "DailyClosures");

            migrationBuilder.DropColumn(
                name: "IsReconciled",
                table: "DailyClosures");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "DailyClosures");

            migrationBuilder.DropColumn(
                name: "CashBoxId",
                table: "CustomerPayments");

            migrationBuilder.RenameColumn(
                name: "ExpectedClosingBalance",
                table: "DailyClosures",
                newName: "ClosingBalance");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNo",
                table: "SupplierPayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentNo",
                table: "SupplierPayments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<byte>(
                name: "PaymentMethod",
                table: "SupplierPayments",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "SupplierPayments",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNo",
                table: "CustomerPayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentNo",
                table: "CustomerPayments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<byte>(
                name: "PaymentMethod",
                table: "CustomerPayments",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "CustomerPayments",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPayments_Currencies_CurrencyId",
                table: "CustomerPayments",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPayments_SalesInvoices_SalesInvoiceId",
                table: "CustomerPayments",
                column: "SalesInvoiceId",
                principalTable: "SalesInvoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_Currencies_CurrencyId",
                table: "SupplierPayments",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_PurchaseInvoices_PurchaseInvoiceId",
                table: "SupplierPayments",
                column: "PurchaseInvoiceId",
                principalTable: "PurchaseInvoices",
                principalColumn: "Id");
        }
    }
}
