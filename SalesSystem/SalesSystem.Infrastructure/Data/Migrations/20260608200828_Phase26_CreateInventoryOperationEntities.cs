using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase26_CreateInventoryOperationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    OperationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdjustmentType = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryOperations", x => x.Id);
                    table.CheckConstraint("CHK_InventoryOperation_Status_Range", "[Status] >= 1 AND [Status] <= 3");
                    table.CheckConstraint("CHK_InventoryOperation_Type_Range", "[OperationType] >= 1 AND [OperationType] <= 3");
                    table.ForeignKey(
                        name: "FK_InventoryOperations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryOperationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryOperationId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    StockIssueReason = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryOperationItems", x => x.Id);
                    table.CheckConstraint("CHK_InventoryOperationItem_Quantity_Positive", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_InventoryOperationItems_InventoryOperations_InventoryOperationId",
                        column: x => x.InventoryOperationId,
                        principalTable: "InventoryOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryOperationItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryOperationItems_InventoryOperationId_ProductId",
                table: "InventoryOperationItems",
                columns: new[] { "InventoryOperationId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryOperationItems_ProductId",
                table: "InventoryOperationItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryOperations_OperationNo",
                table: "InventoryOperations",
                column: "OperationNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryOperations_WarehouseId",
                table: "InventoryOperations",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryOperationItems");

            migrationBuilder.DropTable(
                name: "InventoryOperations");
        }
    }
}
