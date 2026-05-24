using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCashBoxSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CashBoxId",
                table: "SalesInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CashBoxId",
                table: "PurchaseInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WholesalePrice",
                table: "ProductUnits",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ChangeReason",
                table: "ProductPriceHistories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ChangedByUserId",
                table: "ProductPriceHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "NewAvgCost",
                table: "ProductPriceHistories",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NewRetailPrice",
                table: "ProductPriceHistories",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NewWholesalePrice",
                table: "ProductPriceHistories",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OldAvgCost",
                table: "ProductPriceHistories",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OldRetailPrice",
                table: "ProductPriceHistories",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OldWholesalePrice",
                table: "ProductPriceHistories",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ProductUnitId1",
                table: "ProductPriceHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DailyClosures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashBoxId = table.Column<int>(type: "int", nullable: false),
                    ClosureDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalIncome = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalExpense = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyClosures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyClosures_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_CashBoxId",
                table: "SalesInvoices",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_CashBoxId",
                table: "PurchaseInvoices",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceHistories_ChangedBy",
                table: "ProductPriceHistories",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceHistories_ChangedByUserId",
                table: "ProductPriceHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceHistories_ProductUnitId",
                table: "ProductPriceHistories",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceHistories_ProductUnitId1",
                table: "ProductPriceHistories",
                column: "ProductUnitId1");

            migrationBuilder.CreateIndex(
                name: "IX_DailyClosures_CashBoxId_ClosureDate",
                table: "DailyClosures",
                columns: new[] { "CashBoxId", "ClosureDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPriceHistories_ProductUnits_ProductUnitId",
                table: "ProductPriceHistories",
                column: "ProductUnitId",
                principalTable: "ProductUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPriceHistories_ProductUnits_ProductUnitId1",
                table: "ProductPriceHistories",
                column: "ProductUnitId1",
                principalTable: "ProductUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPriceHistories_Users_ChangedBy",
                table: "ProductPriceHistories",
                column: "ChangedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPriceHistories_Users_ChangedByUserId",
                table: "ProductPriceHistories",
                column: "ChangedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_CashBoxes_CashBoxId",
                table: "PurchaseInvoices",
                column: "CashBoxId",
                principalTable: "CashBoxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_CashBoxes_CashBoxId",
                table: "SalesInvoices",
                column: "CashBoxId",
                principalTable: "CashBoxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductPriceHistories_ProductUnits_ProductUnitId",
                table: "ProductPriceHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductPriceHistories_ProductUnits_ProductUnitId1",
                table: "ProductPriceHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductPriceHistories_Users_ChangedBy",
                table: "ProductPriceHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductPriceHistories_Users_ChangedByUserId",
                table: "ProductPriceHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoices_CashBoxes_CashBoxId",
                table: "PurchaseInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_CashBoxes_CashBoxId",
                table: "SalesInvoices");

            migrationBuilder.DropTable(
                name: "DailyClosures");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_CashBoxId",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseInvoices_CashBoxId",
                table: "PurchaseInvoices");

            migrationBuilder.DropIndex(
                name: "IX_ProductPriceHistories_ChangedBy",
                table: "ProductPriceHistories");

            migrationBuilder.DropIndex(
                name: "IX_ProductPriceHistories_ChangedByUserId",
                table: "ProductPriceHistories");

            migrationBuilder.DropIndex(
                name: "IX_ProductPriceHistories_ProductUnitId",
                table: "ProductPriceHistories");

            migrationBuilder.DropIndex(
                name: "IX_ProductPriceHistories_ProductUnitId1",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "CashBoxId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "CashBoxId",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "WholesalePrice",
                table: "ProductUnits");

            migrationBuilder.DropColumn(
                name: "ChangeReason",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "ChangedByUserId",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "NewAvgCost",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "NewRetailPrice",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "NewWholesalePrice",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "OldAvgCost",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "OldRetailPrice",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "OldWholesalePrice",
                table: "ProductPriceHistories");

            migrationBuilder.DropColumn(
                name: "ProductUnitId1",
                table: "ProductPriceHistories");
        }
    }
}
