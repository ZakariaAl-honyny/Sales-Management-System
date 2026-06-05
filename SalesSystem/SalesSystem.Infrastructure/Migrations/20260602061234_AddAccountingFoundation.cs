using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountType = table.Column<byte>(type: "tinyint", nullable: false),
                    ParentAccountId = table.Column<int>(type: "int", nullable: true),
                    IsSystemAccount = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Accounts_ParentAccountId",
                        column: x => x.ParentAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntryNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EntryType = table.Column<byte>(type: "tinyint", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsPosted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsReversed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PostedBy = table.Column<int>(type: "int", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemAccountMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    DefaultCashAccountId = table.Column<int>(type: "int", nullable: false),
                    DefaultBankAccountId = table.Column<int>(type: "int", nullable: false),
                    InventoryAssetAccountId = table.Column<int>(type: "int", nullable: false),
                    AccountsReceivableAccountId = table.Column<int>(type: "int", nullable: false),
                    AccountsPayableAccountId = table.Column<int>(type: "int", nullable: false),
                    VatOutputAccountId = table.Column<int>(type: "int", nullable: false),
                    VatInputAccountId = table.Column<int>(type: "int", nullable: false),
                    CapitalAccountId = table.Column<int>(type: "int", nullable: false),
                    SalesRevenueAccountId = table.Column<int>(type: "int", nullable: false),
                    SalesReturnAccountId = table.Column<int>(type: "int", nullable: false),
                    CogsAccountId = table.Column<int>(type: "int", nullable: false),
                    GeneralExpenseAccountId = table.Column<int>(type: "int", nullable: false),
                    SpoilageLossAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAccountMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemAccountMappings_Accounts_AccountsPayableAccountId",
                        column: x => x.AccountsPayableAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemAccountMappings_Accounts_AccountsReceivableAccountId",
                        column: x => x.AccountsReceivableAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemAccountMappings_Accounts_CapitalAccountId",
                        column: x => x.CapitalAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemAccountMappings_Accounts_CogsAccountId",
                        column: x => x.CogsAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemAccountMappings_Accounts_DefaultBankAccountId",
                        column: x => x.DefaultBankAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemAccountMappings_Accounts_DefaultCashAccountId",
                        column: x => x.DefaultCashAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemAccountMappings_Accounts_GeneralExpenseAccountId",
                        column: x => x.GeneralExpenseAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemAccountMappings_Accounts_InventoryAssetAccountId",
                        column: x => x.InventoryAssetAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemAccountMappings_Accounts_SalesReturnAccountId",
                        column: x => x.SalesReturnAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemAccountMappings_Accounts_SalesRevenueAccountId",
                        column: x => x.SalesRevenueAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemAccountMappings_Accounts_SpoilageLossAccountId",
                        column: x => x.SpoilageLossAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemAccountMappings_Accounts_VatInputAccountId",
                        column: x => x.VatInputAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemAccountMappings_Accounts_VatOutputAccountId",
                        column: x => x.VatOutputAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntryLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JournalEntryId = table.Column<int>(type: "int", nullable: false),
                    JournalEntryId1 = table.Column<int>(type: "int", nullable: true),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Debit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_JournalEntries_JournalEntryId1",
                        column: x => x.JournalEntryId1,
                        principalTable: "JournalEntries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountCode",
                table: "Accounts",
                column: "AccountCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ParentAccountId",
                table: "Accounts",
                column: "ParentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_EntryNumber",
                table: "JournalEntries",
                column: "EntryNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TransactionDate",
                table: "JournalEntries",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_AccountId",
                table: "JournalEntryLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_JournalEntryId",
                table: "JournalEntryLines",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_JournalEntryId1",
                table: "JournalEntryLines",
                column: "JournalEntryId1");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_AccountsPayableAccountId",
                table: "SystemAccountMappings",
                column: "AccountsPayableAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_AccountsReceivableAccountId",
                table: "SystemAccountMappings",
                column: "AccountsReceivableAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_CapitalAccountId",
                table: "SystemAccountMappings",
                column: "CapitalAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_CogsAccountId",
                table: "SystemAccountMappings",
                column: "CogsAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_DefaultBankAccountId",
                table: "SystemAccountMappings",
                column: "DefaultBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_DefaultCashAccountId",
                table: "SystemAccountMappings",
                column: "DefaultCashAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_GeneralExpenseAccountId",
                table: "SystemAccountMappings",
                column: "GeneralExpenseAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_InventoryAssetAccountId",
                table: "SystemAccountMappings",
                column: "InventoryAssetAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_SalesReturnAccountId",
                table: "SystemAccountMappings",
                column: "SalesReturnAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_SalesRevenueAccountId",
                table: "SystemAccountMappings",
                column: "SalesRevenueAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_SpoilageLossAccountId",
                table: "SystemAccountMappings",
                column: "SpoilageLossAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_VatInputAccountId",
                table: "SystemAccountMappings",
                column: "VatInputAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_VatOutputAccountId",
                table: "SystemAccountMappings",
                column: "VatOutputAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JournalEntryLines");

            migrationBuilder.DropTable(
                name: "SystemAccountMappings");

            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
