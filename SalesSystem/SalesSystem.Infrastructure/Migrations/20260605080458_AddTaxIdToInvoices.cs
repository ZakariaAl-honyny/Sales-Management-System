using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxIdToInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SignaturePath",
                table: "StoreSettings",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxId",
                table: "SalesInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxId",
                table: "PurchaseInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Taxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Taxes", x => x.Id);
                    table.CheckConstraint("CHK_Taxes_Rate_Range", "[Rate] >= 0 AND [Rate] <= 100");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_TaxId",
                table: "SalesInvoices",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_TaxId",
                table: "PurchaseInvoices",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_Taxes_IsDefault",
                table: "Taxes",
                column: "IsDefault",
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Taxes_Name",
                table: "Taxes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_Taxes_TaxId",
                table: "PurchaseInvoices",
                column: "TaxId",
                principalTable: "Taxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_Taxes_TaxId",
                table: "SalesInvoices",
                column: "TaxId",
                principalTable: "Taxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoices_Taxes_TaxId",
                table: "PurchaseInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_Taxes_TaxId",
                table: "SalesInvoices");

            migrationBuilder.DropTable(
                name: "Taxes");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_TaxId",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseInvoices_TaxId",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "SignaturePath",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "PurchaseInvoices");
        }
    }
}
