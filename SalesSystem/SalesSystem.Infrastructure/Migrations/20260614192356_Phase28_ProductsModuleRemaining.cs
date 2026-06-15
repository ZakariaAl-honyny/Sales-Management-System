using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase28_ProductsModuleRemaining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashBoxes_Categories_CategoryId",
                table: "CashBoxes");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanySettings_Currencies_DefaultCurrencyId",
                table: "CompanySettings");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Accounts_AccountId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Parties_PartyId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Units_DefaultPurchaseUnitId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Units_DefaultSalesUnitId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoices_CashBoxes_CashBoxId",
                table: "PurchaseInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReturnItems_PurchaseInvoiceItems_PurchaseInvoiceLineId",
                table: "PurchaseReturnItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Accounts_AccountId",
                table: "Suppliers");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Parties_PartyId",
                table: "Suppliers");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_AccountsPayableAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_AccountsReceivableAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_CapitalAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_CogsAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_DefaultBankAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_DefaultCashAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_GeneralExpenseAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_InventoryAssetAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_OpeningBalanceEquityAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_PurchaseReturnAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_SalesReturnAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_SalesRevenueAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_SpoilageLossAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_VatInputAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_VatOutputAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Accounts_AccountId",
                table: "Warehouses");

            migrationBuilder.DropTable(
                name: "AdditionalFeeAllocations");

            migrationBuilder.DropTable(
                name: "BillOfMaterials");

            migrationBuilder.DropTable(
                name: "CashTransactions");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Cheques");

            migrationBuilder.DropTable(
                name: "DailyClosures");

            migrationBuilder.DropTable(
                name: "ExchangeRateHistories");

            migrationBuilder.DropTable(
                name: "FiscalYearClosures");

            migrationBuilder.DropTable(
                name: "InventoryMovements");

            migrationBuilder.DropTable(
                name: "InventoryOperationItems");

            migrationBuilder.DropTable(
                name: "PaymentAllocations");

            migrationBuilder.DropTable(
                name: "ProductBarcodes");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "ProductPriceHistories");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "SalesQuotationItems");

            migrationBuilder.DropTable(
                name: "StockTransferItems");

            migrationBuilder.DropTable(
                name: "StockWriteOffs");

            migrationBuilder.DropTable(
                name: "StoreSettings");

            migrationBuilder.DropTable(
                name: "AdditionalFees");

            migrationBuilder.DropTable(
                name: "InventoryOperations");

            migrationBuilder.DropTable(
                name: "CustomerPayments");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "SalesQuotations");

            migrationBuilder.DropTable(
                name: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_AccountId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_IsDefault",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_Name",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_AccountsPayableAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_AccountsReceivableAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_CapitalAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_CogsAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_DefaultBankAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_DefaultCashAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_GeneralExpenseAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_InventoryAssetAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_OpeningBalanceEquityAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_PurchaseReturnAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_SalesReturnAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_SalesRevenueAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_SpoilageLossAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_VatInputAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_AccountId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_CategoryId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_PartyId",
                table: "Suppliers");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_PurchaseReturns_DiscountRate",
                table: "PurchaseReturns");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseReturnItems_PurchaseInvoiceLineId",
                table: "PurchaseReturnItems");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseInvoices_CashBoxId",
                table: "PurchaseInvoices");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_PurchaseInvoices_DiscountRate",
                table: "PurchaseInvoices");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_PurchaseInvoices_PaidAmount",
                table: "PurchaseInvoices");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_PurchaseInvoiceItems_DiscountRate",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_ProductUnits_BaseUnitFactor",
                table: "ProductUnits");

            migrationBuilder.DropIndex(
                name: "IX_Products_DefaultPurchaseUnitId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_DefaultSalesUnitId",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_InventoryBatches_QuantityReceived_NonNegative",
                table: "InventoryBatches");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_InventoryBatches_QuantityRemaining_NonNegative",
                table: "InventoryBatches");

            migrationBuilder.DropIndex(
                name: "IX_Customers_AccountId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CategoryId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_PartyId",
                table: "Customers");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Currencies_TempId",
                table: "Currencies");

            migrationBuilder.DropIndex(
                name: "IX_CashBoxes_CategoryId",
                table: "CashBoxes");

            migrationBuilder.DropColumn(
                name: "ReorderLevel",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "AccountsPayableAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "AccountsReceivableAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "CapitalAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "CogsAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "DefaultBankAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "DefaultCashAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "GeneralExpenseAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "InventoryAssetAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "OpeningBalanceEquityAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "PurchaseReturnAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "SalesReturnAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "SalesRevenueAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "SpoilageLossAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "VatInputAccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CurrentBalance",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OpeningBalance",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "PartyId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "QuotationId",
                table: "SalesInvoices");

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
                name: "DiscountAmount",
                table: "PurchaseReturnItems");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "PurchaseReturnItems");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PurchaseReturnItems");

            migrationBuilder.DropColumn(
                name: "PurchaseInvoiceLineId",
                table: "PurchaseReturnItems");

            migrationBuilder.DropColumn(
                name: "AdditionalFeesTotal",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "CashBoxId",
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
                name: "SupplierInvoiceNo",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "AdditionalFeesAmount",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropColumn(
                name: "CostInBaseCurrency",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropColumn(
                name: "DiscountRate",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DefaultPurchaseUnitId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DefaultSalesUnitId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaxStockLevel",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SupplierPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "QuantityReceived",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "QuantityRemaining",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "SupplierBatchNo",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CurrentBalance",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "OpeningBalance",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PartyId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TempId",
                table: "Currencies");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "CashBoxes");

            migrationBuilder.RenameIndex(
                name: "IX_WarehouseStocks_WarehouseId_ProductId",
                table: "WarehouseStocks",
                newName: "IX_WarehouseStocks_Warehouse_Product");

            migrationBuilder.RenameColumn(
                name: "VatOutputAccountId",
                table: "SystemAccountMappings",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_SystemAccountMappings_VatOutputAccountId",
                table: "SystemAccountMappings",
                newName: "IX_SystemAccountMappings_AccountId");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "PurchaseInvoices",
                newName: "RemainingAmount");

            migrationBuilder.RenameColumn(
                name: "DueAmount",
                table: "PurchaseInvoices",
                newName: "NetTotal");

            migrationBuilder.RenameColumn(
                name: "BaseConversionFactor",
                table: "ProductUnits",
                newName: "Factor");

            migrationBuilder.RenameColumn(
                name: "HasExpiry",
                table: "Products",
                newName: "TrackExpiry");

            migrationBuilder.AlterColumn<short>(
                name: "WarehouseId",
                table: "WarehouseStocks",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "AvgCost",
                table: "WarehouseStocks",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "متوسط التكلفة المرجح");

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

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "WarehouseStocks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "WarehouseStocks",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Warehouses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Warehouses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                comment: "اسم المستودع",
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "ManagerName",
                table: "Warehouses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Warehouses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "BranchId",
                table: "Warehouses",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Warehouses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "Id",
                table: "Warehouses",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Warehouses",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                comment: "كود المستودع — فريد");

            migrationBuilder.AlterColumn<short>(
                name: "BranchId",
                table: "UserBranches",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "IsSystem",
                table: "Units",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<short>(
                name: "Id",
                table: "Units",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<decimal>(
                name: "Rate",
                table: "Taxes",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<short>(
                name: "Id",
                table: "Taxes",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                table: "SystemAccountMappings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "SystemAccountMappings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "MappingKey",
                table: "SystemAccountMappings",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Suppliers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Suppliers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "Suppliers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "CurrencyId",
                table: "SupplierPayments",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "WarehouseId",
                table: "SalesReturns",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<short>(
                name: "CurrencyId",
                table: "SalesReturns",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "WarehouseId",
                table: "SalesInvoices",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<short>(
                name: "TaxId",
                table: "SalesInvoices",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "CurrencyId",
                table: "SalesInvoices",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "WarehouseId",
                table: "PurchaseReturns",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ReturnNo",
                table: "PurchaseReturns",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<short>(
                name: "CurrencyId",
                table: "PurchaseReturns",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "WarehouseId",
                table: "PurchaseInvoices",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<short>(
                name: "TaxId",
                table: "PurchaseInvoices",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "CurrencyId",
                table: "PurchaseInvoices",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "UnitId",
                table: "ProductUnits",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<short>(
                name: "TaxId",
                table: "Products",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "Products",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "Primary barcode for quick lookup — ASCII-only, not a unique identifier",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "CurrencyId",
                table: "ProductPrices",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductPrices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Mobile",
                table: "Parties",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "Parties",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "CurrencyId",
                table: "JournalEntries",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "BranchId",
                table: "JournalEntries",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentVoucherId",
                table: "JournalEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptVoucherId",
                table: "JournalEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "WarehouseId",
                table: "InventoryCounts",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<short>(
                name: "WarehouseId",
                table: "InventoryBatches",
                type: "smallint",
                nullable: false,
                comment: "معرف المستودع (smallint FK)",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "BatchNo",
                table: "InventoryBatches",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "رقم الدفعة / رقم التشغيلة",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldComment: "رقم الدفعة / رقم التشغيلة");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "InventoryBatches",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManufactureDate",
                table: "InventoryBatches",
                type: "datetime2",
                nullable: true,
                comment: "تاريخ التصنيع");

            migrationBuilder.AddColumn<int>(
                name: "PurchaseInvoiceItemId",
                table: "InventoryBatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "InventoryBatches",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m,
                comment: "الكمية الحالية في الدفعة");

            migrationBuilder.AlterColumn<short>(
                name: "WarehouseId",
                table: "InventoryAdjustments",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<short>(
                name: "CurrencyId",
                table: "Expenses",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<short>(
                name: "BranchId",
                table: "Departments",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Customers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerSince",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Customers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PriceLevel",
                table: "Customers",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "CurrencyId",
                table: "CustomerReceipts",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "Currencies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Currencies",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<short>(
                name: "Id",
                table: "Currencies",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<short>(
                name: "CurrencyId",
                table: "CashBoxes",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<short>(
                name: "BranchId",
                table: "CashBoxes",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AccountId",
                table: "CashBoxes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<short>(
                name: "Id",
                table: "Branches",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<short>(
                name: "CurrencyId",
                table: "Banks",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<short>(
                name: "Id",
                table: "AccountCategories",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.CreateTable(
                name: "CurrencyRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyId = table.Column<short>(type: "smallint", nullable: false),
                    RateToBase = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyRates", x => x.Id);
                    table.CheckConstraint("CHK_CurrencyRates_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]");
                    table.CheckConstraint("CHK_CurrencyRates_RateToBase", "[RateToBase] > 0");
                    table.ForeignKey(
                        name: "FK_CurrencyRates_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    TransactionNo = table.Column<int>(type: "int", nullable: false, comment: "رقم المعاملة — فريد"),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "تاريخ المعاملة"),
                    TransactionType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع المعاملة (مشتريات، مبيعات، تحويل، تسوية، إلخ)"),
                    ReferenceType = table.Column<byte>(type: "tinyint", nullable: true, comment: "نوع المستند المرجعي"),
                    ReferenceId = table.Column<int>(type: "int", nullable: true, comment: "معرف المستند المرجعي"),
                    WarehouseId = table.Column<short>(type: "smallint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentVouchers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherNo = table.Column<int>(type: "int", nullable: false),
                    VoucherDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrencyId = table.Column<short>(type: "smallint", nullable: false),
                    CashBoxId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceDocumentId = table.Column<int>(type: "int", nullable: true),
                    SourceDocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentVouchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentVouchers_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentVouchers_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentVouchers_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptVouchers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherNo = table.Column<int>(type: "int", nullable: false),
                    VoucherDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrencyId = table.Column<short>(type: "smallint", nullable: false),
                    CashBoxId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptVouchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptVouchers_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptVouchers_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptVouchers_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    TransferNo = table.Column<int>(type: "int", nullable: false, comment: "رقم التحويل — فريد"),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "تاريخ التحويل"),
                    SourceWarehouseId = table.Column<short>(type: "smallint", nullable: false),
                    DestinationWarehouseId = table.Column<short>(type: "smallint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Warehouses_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Warehouses_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactionLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryTransactionId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductUnitId = table.Column<int>(type: "int", nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "الكمية بوحدات التخزين الأساسية"),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "تكلفة الوحدة"),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "التكلفة الإجمالية للسطر")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactionLines", x => x.Id);
                    table.CheckConstraint("CHK_InvTxLines_Quantity_Positive", "[Quantity] > 0");
                    table.CheckConstraint("CHK_InvTxLines_UnitCost_NonNegative", "[UnitCost] >= 0");
                    table.ForeignKey(
                        name: "FK_InventoryTransactionLines_InventoryBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "InventoryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactionLines_InventoryTransactions_InventoryTransactionId",
                        column: x => x.InventoryTransactionId,
                        principalTable: "InventoryTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactionLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactionLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseTransferLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseTransferId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductUnitId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "الكمية بوحدات التخزين الأساسية"),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "تكلفة الوحدة"),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "التكلفة الإجمالية للسطر"),
                    BatchId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTransferLines", x => x.Id);
                    table.CheckConstraint("CHK_WHTxLines_Quantity_Positive", "[Quantity] > 0");
                    table.CheckConstraint("CHK_WHTxLines_UnitCost_NonNegative", "[UnitCost] >= 0");
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_InventoryBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "InventoryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_WarehouseTransfers_WarehouseTransferId",
                        column: x => x.WarehouseTransferId,
                        principalTable: "WarehouseTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Code",
                table: "Warehouses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_MappingKey_BranchId",
                table: "SystemAccountMappings",
                columns: new[] { "MappingKey", "BranchId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_InvoiceNo",
                table: "SalesInvoices",
                column: "InvoiceNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_InvoiceNo",
                table: "PurchaseInvoices",
                column: "InvoiceNo",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CHK_PurchaseInvoices_PaidAmount",
                table: "PurchaseInvoices",
                sql: "[PaidAmount] >= 0 AND [PaidAmount] <= [NetTotal]");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_ProductUnits_BaseUnitFactor",
                table: "ProductUnits",
                sql: "IsBaseUnit = 0 OR Factor = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_Mobile",
                table: "Parties",
                column: "Mobile");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_PaymentVoucherId",
                table: "JournalEntries",
                column: "PaymentVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_ReceiptVoucherId",
                table: "JournalEntries",
                column: "ReceiptVoucherId");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_InventoryBatches_Quantity_NonNegative",
                table: "InventoryBatches",
                sql: "[Quantity] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyRates_CurrencyId_EffectiveFrom",
                table: "CurrencyRates",
                columns: new[] { "CurrencyId", "EffectiveFrom" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyRates_EffectiveFrom",
                table: "CurrencyRates",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactionLines_BatchId",
                table: "InventoryTransactionLines",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactionLines_ProductUnitId",
                table: "InventoryTransactionLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_InvTxLines_ProductId",
                table: "InventoryTransactionLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InvTxLines_TransactionId",
                table: "InventoryTransactionLines",
                column: "InventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_Reference",
                table: "InventoryTransactions",
                columns: new[] { "ReferenceType", "ReferenceId" },
                filter: "[ReferenceType] IS NOT NULL AND [ReferenceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_TransactionNo",
                table: "InventoryTransactions",
                column: "TransactionNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_WarehouseId",
                table: "InventoryTransactions",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVouchers_AccountId",
                table: "PaymentVouchers",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVouchers_CashBoxId",
                table: "PaymentVouchers",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVouchers_CurrencyId",
                table: "PaymentVouchers",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVouchers_VoucherNo",
                table: "PaymentVouchers",
                column: "VoucherNo",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptVouchers_AccountId",
                table: "ReceiptVouchers",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptVouchers_CashBoxId",
                table: "ReceiptVouchers",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptVouchers_CurrencyId",
                table: "ReceiptVouchers",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptVouchers_VoucherNo",
                table: "ReceiptVouchers",
                column: "VoucherNo",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferLines_BatchId",
                table: "WarehouseTransferLines",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferLines_ProductUnitId",
                table: "WarehouseTransferLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_WHTxLines_ProductId",
                table: "WarehouseTransferLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WHTxLines_TransferId",
                table: "WarehouseTransferLines",
                column: "WarehouseTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_DestWarehouseId",
                table: "WarehouseTransfers",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_SourceWarehouseId",
                table: "WarehouseTransfers",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_TransferNo",
                table: "WarehouseTransfers",
                column: "TransferNo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanySettings_Currencies_DefaultCurrencyId",
                table: "CompanySettings",
                column: "DefaultCurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Parties_Id",
                table: "Customers",
                column: "Id",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_PaymentVouchers_PaymentVoucherId",
                table: "JournalEntries",
                column: "PaymentVoucherId",
                principalTable: "PaymentVouchers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_ReceiptVouchers_ReceiptVoucherId",
                table: "JournalEntries",
                column: "ReceiptVoucherId",
                principalTable: "ReceiptVouchers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Parties_Id",
                table: "Suppliers",
                column: "Id",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_AccountId",
                table: "SystemAccountMappings",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanySettings_Currencies_DefaultCurrencyId",
                table: "CompanySettings");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Parties_Id",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_PaymentVouchers_PaymentVoucherId",
                table: "JournalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_ReceiptVouchers_ReceiptVoucherId",
                table: "JournalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Parties_Id",
                table: "Suppliers");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAccountMappings_Accounts_AccountId",
                table: "SystemAccountMappings");

            migrationBuilder.DropTable(
                name: "CurrencyRates");

            migrationBuilder.DropTable(
                name: "InventoryTransactionLines");

            migrationBuilder.DropTable(
                name: "PaymentVouchers");

            migrationBuilder.DropTable(
                name: "ReceiptVouchers");

            migrationBuilder.DropTable(
                name: "WarehouseTransferLines");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "WarehouseTransfers");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_Code",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_SystemAccountMappings_MappingKey_BranchId",
                table: "SystemAccountMappings");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_InvoiceNo",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseInvoices_InvoiceNo",
                table: "PurchaseInvoices");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_PurchaseInvoices_PaidAmount",
                table: "PurchaseInvoices");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_ProductUnits_BaseUnitFactor",
                table: "ProductUnits");

            migrationBuilder.DropIndex(
                name: "IX_Parties_Mobile",
                table: "Parties");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_PaymentVoucherId",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_ReceiptVoucherId",
                table: "JournalEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_InventoryBatches_Quantity_NonNegative",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "AvgCost",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "WarehouseStocks");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "MappingKey",
                table: "SystemAccountMappings");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductPrices");

            migrationBuilder.DropColumn(
                name: "Mobile",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "PaymentVoucherId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "ReceiptVoucherId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "ManufactureDate",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "PurchaseInvoiceItemId",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "CustomerSince",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PriceLevel",
                table: "Customers");

            migrationBuilder.RenameIndex(
                name: "IX_WarehouseStocks_Warehouse_Product",
                table: "WarehouseStocks",
                newName: "IX_WarehouseStocks_WarehouseId_ProductId");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "SystemAccountMappings",
                newName: "VatOutputAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_SystemAccountMappings_AccountId",
                table: "SystemAccountMappings",
                newName: "IX_SystemAccountMappings_VatOutputAccountId");

            migrationBuilder.RenameColumn(
                name: "RemainingAmount",
                table: "PurchaseInvoices",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "NetTotal",
                table: "PurchaseInvoices",
                newName: "DueAmount");

            migrationBuilder.RenameColumn(
                name: "Factor",
                table: "ProductUnits",
                newName: "BaseConversionFactor");

            migrationBuilder.RenameColumn(
                name: "TrackExpiry",
                table: "Products",
                newName: "HasExpiry");

            migrationBuilder.AlterColumn<int>(
                name: "WarehouseId",
                table: "WarehouseStocks",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AddColumn<decimal>(
                name: "ReorderLevel",
                table: "WarehouseStocks",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Warehouses",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Warehouses",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldComment: "اسم المستودع");

            migrationBuilder.AlterColumn<string>(
                name: "ManagerName",
                table: "Warehouses",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Warehouses",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "Warehouses",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Warehouses",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Warehouses",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "Warehouses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Warehouses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "UserBranches",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<bool>(
                name: "IsSystem",
                table: "Units",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Units",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<decimal>(
                name: "Rate",
                table: "Taxes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Taxes",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccountsPayableAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccountsReceivableAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CapitalAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CogsAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SystemAccountMappings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultBankAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultCashAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GeneralExpenseAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InventoryAssetAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpeningBalanceEquityAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseReturnAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SalesReturnAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SalesRevenueAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SpoilageLossAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SystemAccountMappings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VatInputAccountId",
                table: "SystemAccountMappings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Suppliers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "Suppliers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Suppliers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                table: "Suppliers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentBalance",
                table: "Suppliers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningBalance",
                table: "Suppliers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PartyId",
                table: "Suppliers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "SupplierPayments",
                type: "int",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WarehouseId",
                table: "SalesReturns",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "SalesReturns",
                type: "int",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WarehouseId",
                table: "SalesInvoices",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "TaxId",
                table: "SalesInvoices",
                type: "int",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "SalesInvoices",
                type: "int",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuotationId",
                table: "SalesInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WarehouseId",
                table: "PurchaseReturns",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<string>(
                name: "ReturnNo",
                table: "PurchaseReturns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "PurchaseReturns",
                type: "int",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
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

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "PurchaseReturnItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<byte>(
                name: "Mode",
                table: "PurchaseReturnItems",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PurchaseReturnItems",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseInvoiceLineId",
                table: "PurchaseReturnItems",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WarehouseId",
                table: "PurchaseInvoices",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "TaxId",
                table: "PurchaseInvoices",
                type: "int",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "PurchaseInvoices",
                type: "int",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalFeesTotal",
                table: "PurchaseInvoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CashBoxId",
                table: "PurchaseInvoices",
                type: "int",
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

            migrationBuilder.AddColumn<string>(
                name: "SupplierInvoiceNo",
                table: "PurchaseInvoices",
                type: "nvarchar(50)",
                maxLength: 50,
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
                name: "DiscountAmount",
                table: "PurchaseInvoiceItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

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

            migrationBuilder.AddColumn<byte>(
                name: "Mode",
                table: "PurchaseInvoiceItems",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PurchaseInvoiceItems",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UnitId",
                table: "ProductUnits",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "TaxId",
                table: "Products",
                type: "int",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "Primary barcode for quick lookup — ASCII-only, not a unique identifier");

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DefaultPurchaseUnitId",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultSalesUnitId",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxStockLevel",
                table: "Products",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "Products",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SupplierPrice",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "ProductPrices",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Parties",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "JournalEntries",
                type: "int",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "JournalEntries",
                type: "int",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WarehouseId",
                table: "InventoryCounts",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "WarehouseId",
                table: "InventoryBatches",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldComment: "معرف المستودع (smallint FK)");

            migrationBuilder.AlterColumn<string>(
                name: "BatchNo",
                table: "InventoryBatches",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                comment: "رقم الدفعة / رقم التشغيلة",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "رقم الدفعة / رقم التشغيلة");

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityReceived",
                table: "InventoryBatches",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m,
                comment: "الكمية المستلمة في الدفعة");

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityRemaining",
                table: "InventoryBatches",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m,
                comment: "الكمية المتبقية في الدفعة");

            migrationBuilder.AddColumn<string>(
                name: "SupplierBatchNo",
                table: "InventoryBatches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "رقم الدفعة عند المورد");

            migrationBuilder.AlterColumn<int>(
                name: "WarehouseId",
                table: "InventoryAdjustments",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "Expenses",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "Departments",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Customers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentBalance",
                table: "Customers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningBalance",
                table: "Customers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PartyId",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "CustomerReceipts",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "Currencies",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Currencies",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Currencies",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<short>(
                name: "TempId",
                table: "Currencies",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "CashBoxes",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "CashBoxes",
                type: "int",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AccountId",
                table: "CashBoxes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "CashBoxes",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Branches",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "Banks",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "AccountCategories",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Currencies_TempId",
                table: "Currencies",
                column: "TempId");

            migrationBuilder.CreateTable(
                name: "AdditionalFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    PurchaseInvoiceId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    DistributionMethod = table.Column<byte>(type: "tinyint", nullable: false),
                    FeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FeeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                name: "BillOfMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssemblyProductId = table.Column<int>(type: "int", nullable: false),
                    ComponentProductId = table.Column<int>(type: "int", nullable: false),
                    ComponentUnitId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    QuantityRequired = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "الكمية المطلوبة من المكوّن لإنتاج وحدة واحدة من المنتج المُجمَّع"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    WastePercentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m, comment: "نسبة الهالك (مثال: 5 تعني 5% إضافية مطلوبة)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillOfMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_ProductUnits_ComponentUnitId",
                        column: x => x.ComponentUnitId,
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_Products_AssemblyProductId",
                        column: x => x.AssemblyProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_Products_ComponentProductId",
                        column: x => x.ComponentProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashBoxId = table.Column<int>(type: "int", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RunningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionType = table.Column<byte>(type: "tinyint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashTransactions_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashTransactions_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashBoxId = table.Column<int>(type: "int", nullable: true),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    SalesInvoiceId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    PaymentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPayments_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerPayments_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerPayments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerPayments_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalTable: "SalesInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyClosures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashBoxId = table.Column<int>(type: "int", nullable: false),
                    ActualCashCount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: false),
                    ClosureDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    ExpectedClosingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsReconciled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalExpense = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalIncome = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "ExchangeRateHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NewRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OldRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RateToBase = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RateType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRateHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeRateHistories_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalYearClosures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: false),
                    ClosingEntryId = table.Column<int>(type: "int", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    NetIncome = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MovementType = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuantityAfter = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityBefore = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityChange = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    AdjustmentType = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OperationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OperationNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                name: "ProductBarcodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    BarcodeValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UnitType = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBarcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBarcodes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "مسار ملف الصورة"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "صورة رئيسية للمنتج"),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "ترتيب العرض"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductPriceHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductUnitId = table.Column<int>(type: "int", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: false),
                    CostingMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    InvoiceId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    NewAvgCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewRetailPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewWholesalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OldAvgCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OldRetailPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OldValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OldWholesalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPriceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPriceHistories_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductPriceHistories_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductPriceHistories_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                name: "SalesQuotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuotationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuotationNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesQuotations_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromWarehouseId = table.Column<int>(type: "int", nullable: false),
                    ToWarehouseId = table.Column<int>(type: "int", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransferNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Warehouses_FromWarehouseId",
                        column: x => x.FromWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Warehouses_ToWarehouseId",
                        column: x => x.ToWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockWriteOffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    WriteOffDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockWriteOffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockWriteOffs_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockWriteOffs_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockWriteOffs_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AllowNegativeStock = table.Column<bool>(type: "bit", nullable: false),
                    AutoUpdatePrices = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DefaultTaxRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EnableStockAlerts = table.Column<bool>(type: "bit", nullable: false),
                    InvoicePrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "INV"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsTaxEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LogoPath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SignaturePath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StoreName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSettings", x => x.Id);
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
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                name: "Cheques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerPaymentId = table.Column<int>(type: "int", nullable: true),
                    SupplierPaymentId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChequeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaturityDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cheques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cheques_CustomerPayments_CustomerPaymentId",
                        column: x => x.CustomerPaymentId,
                        principalTable: "CustomerPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cheques_SupplierPayments_SupplierPaymentId",
                        column: x => x.SupplierPaymentId,
                        principalTable: "SupplierPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerPaymentId = table.Column<int>(type: "int", nullable: true),
                    SupplierPaymentId = table.Column<int>(type: "int", nullable: true),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    InvoiceType = table.Column<byte>(type: "tinyint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_CustomerPayments_CustomerPaymentId",
                        column: x => x.CustomerPaymentId,
                        principalTable: "CustomerPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_SupplierPayments_SupplierPaymentId",
                        column: x => x.SupplierPaymentId,
                        principalTable: "SupplierPayments",
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    StockIssueReason = table.Column<int>(type: "int", nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductUnitId = table.Column<int>(type: "int", nullable: false),
                    PurchaseOrderId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "SalesQuotationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SalesQuotationId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Mode = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesQuotationItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotationItems_SalesQuotations_SalesQuotationId",
                        column: x => x.SalesQuotationId,
                        principalTable: "SalesQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    StockTransferId = table.Column<int>(type: "int", nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: true),
                    Mode = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferItems_InventoryBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "InventoryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferItems_StockTransfers_StockTransferId",
                        column: x => x.StockTransferId,
                        principalTable: "StockTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_AccountId",
                table: "Warehouses",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_IsDefault",
                table: "Warehouses",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Name",
                table: "Warehouses",
                column: "Name");

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
                name: "IX_SystemAccountMappings_OpeningBalanceEquityAccountId",
                table: "SystemAccountMappings",
                column: "OpeningBalanceEquityAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAccountMappings_PurchaseReturnAccountId",
                table: "SystemAccountMappings",
                column: "PurchaseReturnAccountId");

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
                name: "IX_Suppliers_AccountId",
                table: "Suppliers",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CategoryId",
                table: "Suppliers",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_PartyId",
                table: "Suppliers",
                column: "PartyId");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_PurchaseReturns_DiscountRate",
                table: "PurchaseReturns",
                sql: "[DiscountRate] IS NULL OR ([DiscountRate] >= 0 AND [DiscountRate] <= 100)");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnItems_PurchaseInvoiceLineId",
                table: "PurchaseReturnItems",
                column: "PurchaseInvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_CashBoxId",
                table: "PurchaseInvoices",
                column: "CashBoxId");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_PurchaseInvoices_DiscountRate",
                table: "PurchaseInvoices",
                sql: "[DiscountRate] IS NULL OR ([DiscountRate] >= 0 AND [DiscountRate] <= 100)");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_PurchaseInvoices_PaidAmount",
                table: "PurchaseInvoices",
                sql: "[PaidAmount] >= 0 AND [PaidAmount] <= [TotalAmount]");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_PurchaseInvoiceItems_DiscountRate",
                table: "PurchaseInvoiceItems",
                sql: "[DiscountRate] IS NULL OR ([DiscountRate] >= 0 AND [DiscountRate] <= 100)");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_ProductUnits_BaseUnitFactor",
                table: "ProductUnits",
                sql: "IsBaseUnit = 0 OR BaseConversionFactor = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Products_DefaultPurchaseUnitId",
                table: "Products",
                column: "DefaultPurchaseUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_DefaultSalesUnitId",
                table: "Products",
                column: "DefaultSalesUnitId");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_InventoryBatches_QuantityReceived_NonNegative",
                table: "InventoryBatches",
                sql: "[QuantityReceived] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_InventoryBatches_QuantityRemaining_NonNegative",
                table: "InventoryBatches",
                sql: "[QuantityRemaining] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AccountId",
                table: "Customers",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CategoryId",
                table: "Customers",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PartyId",
                table: "Customers",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxes_CategoryId",
                table: "CashBoxes",
                column: "CategoryId");

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
                name: "IX_BillOfMaterials_AssemblyProduct_ComponentProduct",
                table: "BillOfMaterials",
                columns: new[] { "AssemblyProductId", "ComponentProductId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterials_ComponentProductId",
                table: "BillOfMaterials",
                column: "ComponentProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterials_ComponentUnitId",
                table: "BillOfMaterials",
                column: "ComponentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_CashBoxId",
                table: "CashTransactions",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_CurrencyId",
                table: "CashTransactions",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_ReferenceType_ReferenceId",
                table: "CashTransactions",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cheques_ChequeNumber",
                table: "Cheques",
                column: "ChequeNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Cheques_CustomerPaymentId",
                table: "Cheques",
                column: "CustomerPaymentId",
                unique: true,
                filter: "[CustomerPaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Cheques_SupplierPaymentId",
                table: "Cheques",
                column: "SupplierPaymentId",
                unique: true,
                filter: "[SupplierPaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_CashBoxId",
                table: "CustomerPayments",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_CurrencyId",
                table: "CustomerPayments",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_CustomerId",
                table: "CustomerPayments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_PaymentNo",
                table: "CustomerPayments",
                column: "PaymentNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_SalesInvoiceId",
                table: "CustomerPayments",
                column: "SalesInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyClosures_CashBoxId_ClosureDate",
                table: "DailyClosures",
                columns: new[] { "CashBoxId", "ClosureDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRateHistories_CurrencyId_EffectiveDate",
                table: "ExchangeRateHistories",
                columns: new[] { "CurrencyId", "EffectiveDate" });

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

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_CreatedByUserId",
                table: "InventoryMovements",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ProductId_MovementDate",
                table: "InventoryMovements",
                columns: new[] { "ProductId", "MovementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ReferenceType_ReferenceId",
                table: "InventoryMovements",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_WarehouseId",
                table: "InventoryMovements",
                column: "WarehouseId");

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

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_CustomerPaymentId_InvoiceId",
                table: "PaymentAllocations",
                columns: new[] { "CustomerPaymentId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_SupplierPaymentId_InvoiceId",
                table: "PaymentAllocations",
                columns: new[] { "SupplierPaymentId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBarcodes_BarcodeValue",
                table: "ProductBarcodes",
                column: "BarcodeValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductBarcodes_ProductId",
                table: "ProductBarcodes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

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

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationItems_ProductId",
                table: "SalesQuotationItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationItems_SalesQuotationId",
                table: "SalesQuotationItems",
                column: "SalesQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_CurrencyId",
                table: "SalesQuotations",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_CustomerId",
                table: "SalesQuotations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_QuotationNo",
                table: "SalesQuotations",
                column: "QuotationNo",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_WarehouseId",
                table: "SalesQuotations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferItems_BatchId",
                table: "StockTransferItems",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferItems_ProductId",
                table: "StockTransferItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferItems_StockTransferId",
                table: "StockTransferItems",
                column: "StockTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_FromWarehouseId",
                table: "StockTransfers",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ToWarehouseId",
                table: "StockTransfers",
                column: "ToWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_TransferNo",
                table: "StockTransfers",
                column: "TransferNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockWriteOffs_CreatedByUserId",
                table: "StockWriteOffs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockWriteOffs_ProductId",
                table: "StockWriteOffs",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockWriteOffs_WarehouseId",
                table: "StockWriteOffs",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashBoxes_Categories_CategoryId",
                table: "CashBoxes",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanySettings_Currencies_DefaultCurrencyId",
                table: "CompanySettings",
                column: "DefaultCurrencyId",
                principalTable: "Currencies",
                principalColumn: "TempId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Accounts_AccountId",
                table: "Customers",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Parties_PartyId",
                table: "Customers",
                column: "PartyId",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Units_DefaultPurchaseUnitId",
                table: "Products",
                column: "DefaultPurchaseUnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Units_DefaultSalesUnitId",
                table: "Products",
                column: "DefaultSalesUnitId",
                principalTable: "Units",
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
                name: "FK_PurchaseReturnItems_PurchaseInvoiceItems_PurchaseInvoiceLineId",
                table: "PurchaseReturnItems",
                column: "PurchaseInvoiceLineId",
                principalTable: "PurchaseInvoiceItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Accounts_AccountId",
                table: "Suppliers",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Parties_PartyId",
                table: "Suppliers",
                column: "PartyId",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_AccountsPayableAccountId",
                table: "SystemAccountMappings",
                column: "AccountsPayableAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_AccountsReceivableAccountId",
                table: "SystemAccountMappings",
                column: "AccountsReceivableAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_CapitalAccountId",
                table: "SystemAccountMappings",
                column: "CapitalAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_CogsAccountId",
                table: "SystemAccountMappings",
                column: "CogsAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_DefaultBankAccountId",
                table: "SystemAccountMappings",
                column: "DefaultBankAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_DefaultCashAccountId",
                table: "SystemAccountMappings",
                column: "DefaultCashAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_GeneralExpenseAccountId",
                table: "SystemAccountMappings",
                column: "GeneralExpenseAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_InventoryAssetAccountId",
                table: "SystemAccountMappings",
                column: "InventoryAssetAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_OpeningBalanceEquityAccountId",
                table: "SystemAccountMappings",
                column: "OpeningBalanceEquityAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_PurchaseReturnAccountId",
                table: "SystemAccountMappings",
                column: "PurchaseReturnAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_SalesReturnAccountId",
                table: "SystemAccountMappings",
                column: "SalesReturnAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_SalesRevenueAccountId",
                table: "SystemAccountMappings",
                column: "SalesRevenueAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_SpoilageLossAccountId",
                table: "SystemAccountMappings",
                column: "SpoilageLossAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_VatInputAccountId",
                table: "SystemAccountMappings",
                column: "VatInputAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAccountMappings_Accounts_VatOutputAccountId",
                table: "SystemAccountMappings",
                column: "VatOutputAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Accounts_AccountId",
                table: "Warehouses",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
