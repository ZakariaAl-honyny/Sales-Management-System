using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase23_CustomersModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerGroupId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "CustomerType",
                table: "Customers",
                type: "tinyint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AccountId",
                table: "Customers",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerGroupId",
                table: "Customers",
                column: "CustomerGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Accounts_AccountId",
                table: "Customers",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_CustomerGroups_CustomerGroupId",
                table: "Customers",
                column: "CustomerGroupId",
                principalTable: "CustomerGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Accounts_AccountId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_CustomerGroups_CustomerGroupId",
                table: "Customers");

            migrationBuilder.DropTable(
                name: "CustomerGroups");

            migrationBuilder.DropIndex(
                name: "IX_Customers_AccountId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerGroupId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerGroupId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerType",
                table: "Customers");
        }
    }
}
