using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase24_AccountingIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OpeningBalanceEquityAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_OpeningBalanceEquityAccountId",
                table: "SystemAccountMappings",
                column: "OpeningBalanceEquityAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_OpeningBalanceEquityAccountId",
                table: "SystemAccountMappings",
                column: "OpeningBalanceEquityAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_OpeningBalanceEquityAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_OpeningBalanceEquityAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "OpeningBalanceEquityAccountId",
                table: "SystemAccountMappings");
        }
    }
}
