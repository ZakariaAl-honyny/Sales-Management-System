using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncInventoryAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InventoryMovements",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "InventoryMovementId",
                table: "InventoryMovements");

            migrationBuilder.RenameColumn(
                name: "WarehouseStockId",
                table: "WarehouseStocks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "StoreSettingsId",
                table: "StoreSettings",
                newName: "Id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "WarehouseStocks",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "WarehouseStocks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "WarehouseStocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "WarehouseStocks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "WarehouseStocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "StoreSettings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "StoreSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "StoreSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "StoreSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "InventoryMovements",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "InventoryMovements",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "InventoryMovements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "InventoryMovements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "InventoryMovements",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InventoryMovements",
                table: "InventoryMovements",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InventoryMovements",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "StoreSettings");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "InventoryMovements");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "WarehouseStocks",
                newName: "WarehouseStockId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "StoreSettings",
                newName: "StoreSettingsId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "WarehouseStocks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InventoryMovementId",
                table: "InventoryMovements",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InventoryMovements",
                table: "InventoryMovements",
                column: "InventoryMovementId");
        }
    }
}
