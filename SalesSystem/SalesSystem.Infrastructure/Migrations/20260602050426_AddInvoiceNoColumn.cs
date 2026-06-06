using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceNoColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPriceHistories_ProductUnits_ProductUnitId1",
                table: "ProductPriceHistories");

            migrationBuilder.DropIndex(
                name: "IX_ProductPriceHistories_ProductUnitId1",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "ProductUnitId1",
                table: "ProductPriceHistories");

            // Drop existing nvarchar InvoiceNo columns before adding int InvoiceNo (InvoiceNo was nvarchar in InitialCreate)
            // Must drop indexes first because they depend on the column
            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_InvoiceNo",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseInvoices_InvoiceNo",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "InvoiceNo",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "InvoiceNo",
                table: "PurchaseInvoices");

            migrationBuilder.AddColumn<int>(
                name: "InvoiceNo",
                table: "SalesInvoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceNo",
                table: "PurchaseInvoices",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the new int InvoiceNo columns first
            migrationBuilder.DropColumn(
                name: "InvoiceNo",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "InvoiceNo",
                table: "PurchaseInvoices");

            // Restore the original nvarchar InvoiceNo columns and indexes
            migrationBuilder.AddColumn<string>(
                name: "InvoiceNo",
                table: "SalesInvoices",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNo",
                table: "PurchaseInvoices",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_InvoiceNo",
                table: "SalesInvoices",
                column: "InvoiceNo");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_InvoiceNo",
                table: "PurchaseInvoices",
                column: "InvoiceNo");

            migrationBuilder.AddColumn<int>(
                name: "ProductUnitId1",
                table: "ProductPriceHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceHistories_ProductUnitId1",
                table: "ProductPriceHistories",
                column: "ProductUnitId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPriceHistories_ProductUnits_ProductUnitId1",
                table: "ProductPriceHistories",
                column: "ProductUnitId1",
                principalTable: "ProductUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
