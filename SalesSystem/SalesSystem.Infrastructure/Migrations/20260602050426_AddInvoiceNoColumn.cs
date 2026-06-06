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
            migrationBuilder.DropColumn(
                name: "InvoiceNo",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "InvoiceNo",
                table: "PurchaseInvoices");

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
