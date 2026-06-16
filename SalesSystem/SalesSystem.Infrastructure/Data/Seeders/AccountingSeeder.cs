using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds the chart of accounts (74 accounts across 4 levels) and global SystemAccountMappings.
/// Idempotent — skips if the Accounts table already has records.
/// Uses a two-pass approach: seeds Level 1 → saves → queries Ids → seeds Level 2 → saves → repeats for Level 3 → Level 4.
/// </summary>
public static class AccountingSeeder
{
    public static async Task SeedAsync(SalesDbContext db, ILogger? logger = null)
    {
        if (await db.Set<Account>().AnyAsync())
        {
            logger?.LogInformation("Accounts already seeded. Skipping...");
            return;
        }

        // ─── Level 1: Groups (5 accounts) ─────────────────────────
        var level1 = new List<Account>
        {
            Account.Create("1000", "الأصول", "Assets",
                nature: (byte)AccountType.Asset, isLeaf: false, isSystem: true),
            Account.Create("1300", "الخصوم", "Liabilities",
                nature: (byte)AccountType.Liability, isLeaf: false, isSystem: true),
            Account.Create("1400", "حقوق الملكية", "Equity",
                nature: (byte)AccountType.Equity, isLeaf: false, isSystem: true),
            Account.Create("1500", "الإيرادات", "Revenue",
                nature: (byte)AccountType.Revenue, isLeaf: false, isSystem: true),
            Account.Create("1600", "المصروفات", "Expenses",
                nature: (byte)AccountType.Expense, isLeaf: false, isSystem: true),
        };

        db.Set<Account>().AddRange(level1);
        await db.SaveChangesAsync();
        var l1 = await db.Set<Account>().ToDictionaryAsync(a => a.AccountCode);

        // ─── Level 2: Main Categories (8 accounts) ────────────────
        var level2 = new List<Account>
        {
            // Under 1000 Assets
            Account.Create("1100", "أصول متداولة", "Current Assets",
                nature: (byte)AccountType.Asset, isLeaf: false,
                parentId: l1["1000"].Id, isSystem: true),
            Account.Create("1200", "أصول ثابتة", "Fixed Assets",
                nature: (byte)AccountType.Asset, isLeaf: false,
                parentId: l1["1000"].Id, isSystem: true),
            // Under 1300 Liabilities
            Account.Create("1310", "التزامات متداولة", "Current Liabilities",
                nature: (byte)AccountType.Liability, isLeaf: false,
                parentId: l1["1300"].Id, isSystem: true),
            // Under 1400 Equity
            Account.Create("1410", "رأس المال والاحتياطيات", "Capital & Reserves",
                nature: (byte)AccountType.Equity, isLeaf: false,
                parentId: l1["1400"].Id, isSystem: true),
            Account.Create("1420", "الأرباح والخسائر", "Profit & Loss",
                nature: (byte)AccountType.Equity, isLeaf: false,
                parentId: l1["1400"].Id, isSystem: true),
            // Under 1500 Revenue
            Account.Create("1510", "إيرادات النشاط", "Operating Revenue",
                nature: (byte)AccountType.Revenue, isLeaf: false,
                parentId: l1["1500"].Id, isSystem: true),
            // Under 1600 Expenses
            Account.Create("1610", "تكاليف النشاط", "Activity Costs",
                nature: (byte)AccountType.Expense, isLeaf: false,
                parentId: l1["1600"].Id, isSystem: true),
            Account.Create("1670", "مصاريف تشغيلية وإدارية", "Operating Expenses",
                nature: (byte)AccountType.Expense, isLeaf: false,
                parentId: l1["1600"].Id, isSystem: true),
        };

        db.Set<Account>().AddRange(level2);
        await db.SaveChangesAsync();
        var l2 = await db.Set<Account>().ToDictionaryAsync(a => a.AccountCode);

        // ─── Level 3: Sub Categories (24 accounts) ────────────────
        var level3 = new List<Account>();

        // Under 1100 Current Assets
        level3.Add(Account.Create("1110", "النقدية", "Cash & Cash Equivalents",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["1100"].Id));
        level3.Add(Account.Create("1120", "البنوك", "Bank Accounts",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["1100"].Id));
        level3.Add(Account.Create("1130", "العملاء", "Accounts Receivable",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["1100"].Id));
        level3.Add(Account.Create("1140", "المخزون", "Inventory",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["1100"].Id));
        level3.Add(Account.Create("1150", "أصول متداولة أخرى", "Other Current Assets",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["1100"].Id));
        level3.Add(Account.Create("1160", "تسوية المخزون", "Inventory Settlement",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l2["1100"].Id));
        level3.Add(Account.Create("1170", "عهد الموظفين", "Employee Custody",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["1100"].Id));

        // Under 1200 Fixed Assets
        level3.Add(Account.Create("1210", "أصول ثابتة ملموسة", "Tangible Fixed Assets",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["1200"].Id));
        level3.Add(Account.Create("1220", "أصول ثابتة غير ملموسة", "Intangible Fixed Assets",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["1200"].Id));
        level3.Add(Account.Create("1230", "مجمع الإهلاك", "Accumulated Depreciation",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["1200"].Id));

        // Under 1310 Current Liabilities
        level3.Add(Account.Create("1320", "الموردون", "Accounts Payable",
            nature: (byte)AccountType.Liability, isLeaf: false, parentId: l2["1310"].Id));
        level3.Add(Account.Create("1330", "الضرائب", "Taxes Payable",
            nature: (byte)AccountType.Liability, isLeaf: false, parentId: l2["1310"].Id));
        level3.Add(Account.Create("1340", "التزامات متداولة أخرى", "Other Current Liabilities",
            nature: (byte)AccountType.Liability, isLeaf: false, parentId: l2["1310"].Id));

        // Under 1410 Capital & Reserves
        level3.Add(Account.Create("1411", "رأس المال", "Capital",
            nature: (byte)AccountType.Equity, isLeaf: true, parentId: l2["1410"].Id));
        level3.Add(Account.Create("1412", "المسحوبات", "Drawings",
            nature: (byte)AccountType.Equity, isLeaf: true, parentId: l2["1410"].Id));

        // Under 1420 Profit & Loss
        level3.Add(Account.Create("1421", "أرباح مدورة", "Retained Earnings",
            nature: (byte)AccountType.Equity, isLeaf: true, parentId: l2["1420"].Id));
        level3.Add(Account.Create("1422", "أرصدة افتتاحية", "Opening Balance Equity",
            nature: (byte)AccountType.Equity, isLeaf: true, parentId: l2["1420"].Id, isSystem: true));
        level3.Add(Account.Create("1423", "أرباح غير موزعة", "Undistributed Profits",
            nature: (byte)AccountType.Equity, isLeaf: true, parentId: l2["1420"].Id, isSystem: true));

        // Under 1510 Operating Revenue
        level3.Add(Account.Create("1520", "إيرادات المبيعات", "Sales Revenue",
            nature: (byte)AccountType.Revenue, isLeaf: true, parentId: l2["1510"].Id));
        level3.Add(Account.Create("1530", "إيرادات أخرى", "Other Revenue",
            nature: (byte)AccountType.Revenue, isLeaf: true, parentId: l2["1510"].Id));

        // Under 1610 Activity Costs
        level3.Add(Account.Create("1620", "تكلفة المبيعات", "Cost of Sales",
            nature: (byte)AccountType.Expense, isLeaf: false, parentId: l2["1610"].Id));
        level3.Add(Account.Create("1630", "المردودات", "Returns",
            nature: (byte)AccountType.Expense, isLeaf: false, parentId: l2["1610"].Id));

        // Under 1670 Operating Expenses
        level3.Add(Account.Create("1680", "مصروفات عمومية وإدارية", "General & Admin Expenses",
            nature: (byte)AccountType.Expense, isLeaf: false, parentId: l2["1670"].Id));
        level3.Add(Account.Create("1690", "مصروفات أخرى", "Other Expenses",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l2["1670"].Id));

        db.Set<Account>().AddRange(level3);
        await db.SaveChangesAsync();
        var l3 = await db.Set<Account>().ToDictionaryAsync(a => a.AccountCode);

        // ─── Level 4: Detail Accounts (37 accounts) ───────────────
        var level4 = new List<Account>();

        // Under 1110 Cash
        level4.Add(Account.Create("1111", "الصندوق", "Cash on Hand",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1110"].Id));
        level4.Add(Account.Create("1112", "صندوق المصروفات النثرية", "Petty Cash",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1110"].Id));

        // Under 1120 Banks
        level4.Add(Account.Create("1121", "البنك الأهلي", "National Bank",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1120"].Id));
        level4.Add(Account.Create("1122", "بنك الرياض", "Riyad Bank",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1120"].Id));

        // Under 1130 Accounts Receivable
        level4.Add(Account.Create("1131", "العميل النقدي", "Cash Customer",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1130"].Id));
        level4.Add(Account.Create("1132", "عملاء آجلون", "Credit Customers",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1130"].Id));
        level4.Add(Account.Create("1133", "مخصص الديون المشكوك فيها", "Allowance for Doubtful Debts",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1130"].Id));

        // Under 1140 Inventory
        level4.Add(Account.Create("1141", "بضاعة أول المدة", "Opening Inventory",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1140"].Id));
        level4.Add(Account.Create("1142", "مخزون آخر المدة", "Closing Inventory",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1140"].Id));

        // Under 1150 Other Current Assets
        level4.Add(Account.Create("1151", "مصروفات مدفوعة مقدماً", "Prepaid Expenses",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1150"].Id));
        level4.Add(Account.Create("1152", "أوراق قبض", "Notes Receivable",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1150"].Id));

        // Under 1210 Tangible Fixed Assets
        level4.Add(Account.Create("1211", "أثاث ومعدات", "Furniture & Equipment",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1210"].Id));
        level4.Add(Account.Create("1212", "أجهزة حاسب آلي", "Computer Equipment",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1210"].Id));

        // Under 1220 Intangible Fixed Assets
        level4.Add(Account.Create("1221", "برمجيات", "Software",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1220"].Id));

        // Under 1230 Accumulated Depreciation
        level4.Add(Account.Create("1231", "مخصص إهلاك الأثاث", "Accum. Dep. Furniture",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1230"].Id));
        level4.Add(Account.Create("1232", "مخصص إهلاك الحاسب", "Accum. Dep. Computers",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1230"].Id));

        // Under 1320 Accounts Payable
        level4.Add(Account.Create("1321", "المورد النقدي", "Cash Supplier",
            nature: (byte)AccountType.Liability, isLeaf: true, parentId: l3["1320"].Id));
        level4.Add(Account.Create("1322", "موردون آجلون", "Credit Suppliers",
            nature: (byte)AccountType.Liability, isLeaf: true, parentId: l3["1320"].Id));

        // Under 1330 Taxes
        level4.Add(Account.Create("1331", "ضريبة المبيعات (خرج)", "VAT Output",
            nature: (byte)AccountType.Liability, isLeaf: true, parentId: l3["1330"].Id));
        level4.Add(Account.Create("1332", "ضريبة المشتريات (دخل)", "VAT Input",
            nature: (byte)AccountType.Liability, isLeaf: true, parentId: l3["1330"].Id));

        // Under 1340 Other Current Liabilities
        level4.Add(Account.Create("1341", "أوراق دفع", "Notes Payable",
            nature: (byte)AccountType.Liability, isLeaf: true, parentId: l3["1340"].Id));

        // Under 1620 Cost of Sales
        level4.Add(Account.Create("1621", "تكلفة البضاعة المباعة", "Cost of Goods Sold",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["1620"].Id));

        // Under 1630 Returns
        level4.Add(Account.Create("1631", "مردودات مبيعات", "Sales Returns",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["1630"].Id));
        level4.Add(Account.Create("1632", "مردودات مشتريات", "Purchase Returns",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["1630"].Id));

        // Under 1680 General & Admin Expenses
        level4.Add(Account.Create("1681", "مصروفات عمومية", "General Expenses",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["1680"].Id));
        level4.Add(Account.Create("1682", "الرواتب والأجور", "Salaries & Wages",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["1680"].Id));
        level4.Add(Account.Create("1683", "الكهرباء", "Electricity",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["1680"].Id));
        level4.Add(Account.Create("1684", "المياه", "Water",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["1680"].Id));
        level4.Add(Account.Create("1685", "الإيجارات", "Rent",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["1680"].Id));
        level4.Add(Account.Create("1686", "النقل", "Transport",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["1680"].Id));
        level4.Add(Account.Create("1687", "الخصم المسموح به", "Discount Allowed",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["1680"].Id));

        // Under 1690 Other Expenses
        level4.Add(Account.Create("1691", "هالك المخزون", "Spoilage Loss",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["1690"].Id));
        level4.Add(Account.Create("1692", "عجز مخزون", "Inventory Shortage",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["1690"].Id));
        level4.Add(Account.Create("1693", "زيادة مخزون", "Inventory Surplus",
            nature: (byte)AccountType.Revenue, isLeaf: true, parentId: l3["1690"].Id));

        // Under 1530 Other Revenue
        level4.Add(Account.Create("1531", "إيراد النقل", "Transport Revenue",
            nature: (byte)AccountType.Revenue, isLeaf: true, parentId: l3["1530"].Id));
        level4.Add(Account.Create("1532", "الخصم المكتسب", "Discount Received",
            nature: (byte)AccountType.Revenue, isLeaf: true, parentId: l3["1530"].Id));
        level4.Add(Account.Create("1533", "إيرادات التوصيل", "Delivery Charges Revenue",
            nature: (byte)AccountType.Revenue, isLeaf: true, parentId: l3["1530"].Id));

        db.Set<Account>().AddRange(level4);
        await db.SaveChangesAsync();

        var totalCount = 5 + 8 + 24 + 37; // 74
        logger?.LogInformation("Chart of accounts seeded: {Count} accounts created across 4 levels.", totalCount);

        // ─── Query IDs by AccountCode for System Account Mappings ──
        var allAccounts = await db.Set<Account>().ToDictionaryAsync(a => a.AccountCode);

        var cashAccount = allAccounts["1111"];
        var bankAccount = allAccounts["1121"];
        var arAccount = allAccounts["1131"];
        var inventoryAccount = allAccounts["1141"];
        var apAccount = allAccounts["1321"];
        var vatOutputAccount = allAccounts["1331"];
        var vatInputAccount = allAccounts["1332"];
        var capitalAccount = allAccounts["1411"];
        var salesRevenueAccount = allAccounts["1520"];
        var salesReturnAccount = allAccounts["1631"];
        var purchaseReturnAccount = allAccounts["1632"];
        var cogsAccount = allAccounts["1621"];
        var generalExpenseAccount = allAccounts["1681"];
        var spoilageAccount = allAccounts["1691"];
        var shortageAccount = allAccounts["1692"];
        var surplusAccount = allAccounts["1693"];
        var openingBalanceEquityAccount = allAccounts["1422"];
        var undistributedProfitsAccount = allAccounts["1423"];
        var deliveryChargesRevenueAccount = allAccounts["1533"];

        // ─── Seed system account mappings (key-value pattern) ────
        var mappingList = new List<SystemAccountMapping>
        {
            SystemAccountMapping.Create(SystemAccountKey.DefaultCash.ToString(), cashAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.DefaultBank.ToString(), bankAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.AccountsReceivable.ToString(), arAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.AccountsPayable.ToString(), apAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.Inventory.ToString(), inventoryAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.CostOfGoodsSold.ToString(), cogsAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.SalesRevenue.ToString(), salesRevenueAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.SalesReturns.ToString(), salesReturnAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.PurchaseReturns.ToString(), purchaseReturnAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.VatOutput.ToString(), vatOutputAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.VatInput.ToString(), vatInputAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.Capital.ToString(), capitalAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.OpeningBalanceEquity.ToString(), openingBalanceEquityAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.RetainedEarnings.ToString(), allAccounts["1421"].Id),
            SystemAccountMapping.Create(SystemAccountKey.InventoryShortage.ToString(), shortageAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.InventorySurplus.ToString(), surplusAccount.Id),

            // ─── Missing mappings (Phase 22-25 remediation) ─────
            SystemAccountMapping.Create(SystemAccountKey.UndistributedProfits.ToString(), undistributedProfitsAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.GeneralExpense.ToString(), generalExpenseAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.SpoilageLoss.ToString(), spoilageAccount.Id),
            SystemAccountMapping.Create(SystemAccountKey.EmployeeCustody.ToString(), allAccounts["1170"].Id),
            SystemAccountMapping.Create(SystemAccountKey.DeliveryChargesRevenue.ToString(), deliveryChargesRevenueAccount.Id),
        };

        db.Set<SystemAccountMapping>().AddRange(mappingList);

        await db.SaveChangesAsync();
        logger?.LogInformation("System account mappings seeded: {Count} keys.", mappingList.Count);
    }
}
