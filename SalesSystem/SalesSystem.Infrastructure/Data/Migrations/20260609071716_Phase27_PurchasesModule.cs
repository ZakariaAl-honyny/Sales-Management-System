using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase27_PurchasesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReturns_PurchaseInvoices_PurchaseInvoiceId",
                table: "PurchaseReturns");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseReturns_ReturnNo",
                table: "PurchaseReturns");

            migrationBuilder.RenameColumn(
                name: "Reason",
                table: "PurchaseReturns",
                newName: "Notes");

            migrationBuilder.AlterColumn<string>(
                name: "ReturnNo",
                table: "PurchaseReturns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "PurchaseReturns",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "PurchaseReturns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRate",
                table: "PurchaseReturns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "DiscountType",
                table: "PurchaseReturns",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LinkToInvoice",
                table: "PurchaseReturns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "CostInBaseCurrency",
                table: "PurchaseReturnItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PurchaseReturnItems",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductUnitId",
                table: "PurchaseReturnItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalFeesTotal",
                table: "PurchaseInvoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

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

            migrationBuilder.AddColumn<byte>(
                name: "DiscountType",
                table: "PurchaseInvoices",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalFeesAmount",
                table: "PurchaseInvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CostInBaseCurrency",
                table: "PurchaseInvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRate",
                table: "PurchaseInvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "DiscountType",
                table: "PurchaseInvoiceItems",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductUnitId",
                table: "PurchaseInvoiceItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AdditionalFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseInvoiceId = table.Column<int>(type: "int", nullable: false),
                    FeeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DistributionMethod = table.Column<byte>(type: "tinyint", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdditionalFees_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdditionalFees_PurchaseInvoices_PurchaseInvoiceId",
                        column: x => x.PurchaseInvoiceId,
                        principalTable: "PurchaseInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdditionalFeeAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdditionalFeeId = table.Column<int>(type: "int", nullable: false),
                    PurchaseInvoiceItemId = table.Column<int>(type: "int", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalFeeAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdditionalFeeAllocations_AdditionalFees_AdditionalFeeId",
                        column: x => x.AdditionalFeeId,
                        principalTable: "AdditionalFees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdditionalFeeAllocations_PurchaseInvoiceItems_PurchaseInvoiceItemId",
                        column: x => x.PurchaseInvoiceItemId,
                        principalTable: "PurchaseInvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductUnitId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CHK_PurchaseReturns_DiscountRate",
                table: "PurchaseReturns",
                sql: "[DiscountRate] IS NULL OR ([DiscountRate] >= 0 AND [DiscountRate] <= 100)");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnItems_ProductUnitId",
                table: "PurchaseReturnItems",
                column: "ProductUnitId");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_PurchaseInvoices_DiscountRate",
                table: "PurchaseInvoices",
                sql: "[DiscountRate] IS NULL OR ([DiscountRate] >= 0 AND [DiscountRate] <= 100)");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceItems_ProductUnitId",
                table: "PurchaseInvoiceItems",
                column: "ProductUnitId");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_PurchaseInvoiceItems_DiscountRate",
                table: "PurchaseInvoiceItems",
                sql: "[DiscountRate] IS NULL OR ([DiscountRate] >= 0 AND [DiscountRate] <= 100)");

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalFeeAllocations_AdditionalFeeId",
                table: "AdditionalFeeAllocations",
                column: "AdditionalFeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalFeeAllocations_PurchaseInvoiceItemId",
                table: "AdditionalFeeAllocations",
                column: "PurchaseInvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalFees_AccountId",
                table: "AdditionalFees",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalFees_PurchaseInvoiceId",
                table: "AdditionalFees",
                column: "PurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_ProductId",
                table: "PurchaseOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_ProductUnitId",
                table: "PurchaseOrderItems",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderId",
                table: "PurchaseOrderItems",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CurrencyId",
                table: "PurchaseOrders",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_WarehouseId",
                table: "PurchaseOrders",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoiceItems_ProductUnits_ProductUnitId",
                table: "PurchaseInvoiceItems",
                column: "ProductUnitId",
                principalTable: "ProductUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseReturnItems_ProductUnits_ProductUnitId",
                table: "PurchaseReturnItems",
                column: "ProductUnitId",
                principalTable: "ProductUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseReturns_PurchaseInvoices_PurchaseInvoiceId",
                table: "PurchaseReturns",
                column: "PurchaseInvoiceId",
                principalTable: "PurchaseInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoiceItems_ProductUnits_ProductUnitId",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReturnItems_ProductUnits_ProductUnitId",
                table: "PurchaseReturnItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReturns_PurchaseInvoices_PurchaseInvoiceId",
                table: "PurchaseReturns");

            migrationBuilder.DropTable(
                name: "AdditionalFeeAllocations");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "AdditionalFees");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_PurchaseReturns_DiscountRate",
                table: "PurchaseReturns");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseReturnItems_ProductUnitId",
                table: "PurchaseReturnItems");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_PurchaseInvoices_DiscountRate",
                table: "PurchaseInvoices");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseInvoiceItems_ProductUnitId",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_PurchaseInvoiceItems_DiscountRate",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "DiscountRate",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "LinkToInvoice",
                table: "PurchaseReturns");

            migrationBuilder.DropColumn(
                name: "CostInBaseCurrency",
                table: "PurchaseReturnItems");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PurchaseReturnItems");

            migrationBuilder.DropColumn(
                name: "ProductUnitId",
                table: "PurchaseReturnItems");

            migrationBuilder.DropColumn(
                name: "AdditionalFeesTotal",
                table: "PurchaseInvoices");

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
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropColumn(
                name: "CostInBaseCurrency",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropColumn(
                name: "DiscountRate",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropColumn(
                name: "ProductUnitId",
                table: "PurchaseInvoiceItems");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "PurchaseReturns",
                newName: "Reason");

            migrationBuilder.AlterColumn<string>(
                name: "ReturnNo",
                table: "PurchaseReturns",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "PurchaseReturns",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_ReturnNo",
                table: "PurchaseReturns",
                column: "ReturnNo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseReturns_PurchaseInvoices_PurchaseInvoiceId",
                table: "PurchaseReturns",
                column: "PurchaseInvoiceId",
                principalTable: "PurchaseInvoices",
                principalColumn: "Id");
        }
    }
}
