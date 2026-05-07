using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialBackendPhase9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DocumentSequenceId",
                table: "DocumentSequences",
                newName: "Id");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DocumentSequences",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "DocumentSequences",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "DocumentSequences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "DocumentSequences",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "DocumentSequences",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DocumentSequences");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "DocumentSequences");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "DocumentSequences");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "DocumentSequences");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "DocumentSequences");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "DocumentSequences",
                newName: "DocumentSequenceId");
        }
    }
}
