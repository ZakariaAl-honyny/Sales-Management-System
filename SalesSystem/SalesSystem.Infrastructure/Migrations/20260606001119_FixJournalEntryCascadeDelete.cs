using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixJournalEntryCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                table: "JournalEntryLines");

            migrationBuilder.CreateTable(
                name: "FiscalYearClosures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: false),
                    NetIncome = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingEntryId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYearClosures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalYearClosures_JournalEntries_ClosingEntryId",
                        column: x => x.ClosingEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FiscalYearClosures_Users_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYearClosures_ClosedByUserId",
                table: "FiscalYearClosures",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYearClosures_ClosingEntryId",
                table: "FiscalYearClosures",
                column: "ClosingEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYearClosures_FiscalYear",
                table: "FiscalYearClosures",
                column: "FiscalYear",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                table: "JournalEntryLines",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                table: "JournalEntryLines");

            migrationBuilder.DropTable(
                name: "FiscalYearClosures");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                table: "JournalEntryLines",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
