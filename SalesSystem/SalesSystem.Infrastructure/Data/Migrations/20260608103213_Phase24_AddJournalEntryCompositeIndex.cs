using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase24_AddJournalEntryCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_ReferenceType_ReferenceId",
                table: "JournalEntries",
                columns: new[] { "ReferenceType", "ReferenceId" },
                filter: "[ReferenceType] IS NOT NULL AND [ReferenceId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_ReferenceType_ReferenceId",
                table: "JournalEntries");
        }
    }
}
