using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds the chart of accounts (60 accounts across 4 levels) and global SystemAccountMappings.
/// Idempotent — skips if the Accounts table already has records.
/// Uses a two-pass approach: seeds Level 1 → saves → queries Ids → seeds Level 2 → saves → repeats for Level 3 → Level 4.
/// </summary>
public static class AccountingSeeder
{
    private const string ColorAsset = "#2196F3";
    private const string ColorLiability = "#F44336";
    private const string ColorEquity = "#4CAF50";
    private const string ColorRevenue = "#4CAF50";
    private const string ColorExpense = "#FF9800";

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
            Account.Create("1000", "الأصول", "Assets", AccountType.Asset, 1,
                isSystemAccount: true, colorCode: ColorAsset,
                description: "الأصول المتداولة والثابتة",
                explanation: "يمثل جميع أصول المنشأة من متداولة وثابتة"),
            Account.Create("1300", "الخصوم", "Liabilities", AccountType.Liability, 1,
                isSystemAccount: true, colorCode: ColorLiability,
                description: "الالتزامات المالية على المنشأة",
                explanation: "يمثل التزامات المنشأة تجاه الغير"),
            Account.Create("1400", "حقوق الملكية", "Equity", AccountType.Equity, 1,
                isSystemAccount: true, colorCode: ColorEquity,
                description: "رأس المال وحقوق الملكية",
                explanation: "يمثل حقوق الملكية ورأس المال"),
            Account.Create("1500", "الإيرادات", "Revenue", AccountType.Revenue, 1,
                isSystemAccount: true, colorCode: ColorRevenue,
                description: "إيرادات النشاط التشغيلي",
                explanation: "يمثل إيرادات المنشأة من مختلف الأنشطة"),
            Account.Create("1600", "المصروفات", "Expenses", AccountType.Expense, 1,
                isSystemAccount: true, colorCode: ColorExpense,
                description: "المصروفات التشغيلية",
                explanation: "يمثل مصروفات المنشأة التشغيلية والإدارية"),
        };

        db.Set<Account>().AddRange(level1);
        await db.SaveChangesAsync();
        var l1 = await db.Set<Account>().ToDictionaryAsync(a => a.AccountCode);

        // ─── Level 2: Main Categories (8 accounts) ────────────────
        var level2 = new List<Account>
        {
            // Under 1000 Assets
            Account.Create("1100", "أصول متداولة", "Current Assets", AccountType.Asset, 2,
                parentAccountId: l1["1000"].Id, isSystemAccount: true, colorCode: ColorAsset,
                description: "الأصول التي يمكن تحويلها إلى نقد خلال سنة",
                explanation: "الأصول التي يمكن تحويلها إلى نقد خلال سنة"),
            Account.Create("1200", "أصول ثابتة", "Fixed Assets", AccountType.Asset, 2,
                parentAccountId: l1["1000"].Id, isSystemAccount: true, colorCode: ColorAsset,
                description: "الأصول طويلة الأجل",
                explanation: "الأصول طويلة الأجل المستخدمة في التشغيل"),
            // Under 1300 Liabilities
            Account.Create("1310", "التزامات متداولة", "Current Liabilities", AccountType.Liability, 2,
                parentAccountId: l1["1300"].Id, isSystemAccount: true, colorCode: ColorLiability,
                description: "الالتزامات المستحقة خلال سنة",
                explanation: "الالتزامات المستحقة خلال سنة"),
            // Under 1400 Equity
            Account.Create("1410", "رأس المال والاحتياطيات", "Capital & Reserves", AccountType.Equity, 2,
                parentAccountId: l1["1400"].Id, isSystemAccount: true, colorCode: ColorEquity,
                description: "رأس المال والاحتياطيات",
                explanation: "رأس المال المدفوع والاحتياطيات النظامية"),
            Account.Create("1420", "الأرباح والخسائر", "Profit & Loss", AccountType.Equity, 2,
                parentAccountId: l1["1400"].Id, isSystemAccount: true, colorCode: ColorEquity,
                description: "أرباح وخسائر الفترة",
                explanation: "نتائج الأعمال من أرباح وخسائر"),
            // Under 1500 Revenue
            Account.Create("1510", "إيرادات النشاط", "Operating Revenue", AccountType.Revenue, 2,
                parentAccountId: l1["1500"].Id, isSystemAccount: true, colorCode: ColorRevenue,
                description: "إيرادات النشاط الرئيسي",
                explanation: "إيرادات النشاط الرئيسي للمنشأة"),
            // Under 1600 Expenses
            Account.Create("1610", "تكاليف النشاط", "Activity Costs", AccountType.Expense, 2,
                parentAccountId: l1["1600"].Id, isSystemAccount: true, colorCode: ColorExpense,
                description: "تكاليف مباشرة متعلقة بالنشاط",
                explanation: "التكاليف المباشرة المرتبطة بالنشاط"),
            Account.Create("1670", "مصاريف تشغيلية وإدارية", "Operating Expenses", AccountType.Expense, 2,
                parentAccountId: l1["1600"].Id, isSystemAccount: true, colorCode: ColorExpense,
                description: "مصاريف التشغيل والإدارة",
                explanation: "مصروفات التشغيل والإدارة اليومية"),
        };

        db.Set<Account>().AddRange(level2);
        await db.SaveChangesAsync();
        var l2 = await db.Set<Account>().ToDictionaryAsync(a => a.AccountCode);

        // ─── Level 3: Sub Categories (20 accounts) ────────────────
        var level3 = new List<Account>();

        // Under 1100 Current Assets
        level3.Add(Account.Create("1110", "النقدية", "Cash & Cash Equivalents", AccountType.Asset, 3,
            parentAccountId: l2["1100"].Id, colorCode: ColorAsset,
            description: "النقدية المتوفرة في الصندوق والبنوك",
            explanation: "النقدية المتوفرة في الصندوق والبنوك"));
        level3.Add(Account.Create("1120", "البنوك", "Bank Accounts", AccountType.Asset, 3,
            parentAccountId: l2["1100"].Id, colorCode: ColorAsset,
            description: "الأرصدة في الحسابات البنكية",
            explanation: "الحسابات الجارية لدى البنوك"));
        level3.Add(Account.Create("1130", "العملاء", "Accounts Receivable", AccountType.Asset, 3,
            parentAccountId: l2["1100"].Id, colorCode: ColorAsset,
            description: "المبالغ المستحقة من العملاء",
            explanation: "المبالغ المستحقة على العملاء"));
        level3.Add(Account.Create("1140", "المخزون", "Inventory", AccountType.Asset, 3,
            parentAccountId: l2["1100"].Id, colorCode: ColorAsset,
            description: "قيمة البضاعة في المخازن",
            explanation: "البضاعة الموجودة في المخازن"));
        level3.Add(Account.Create("1150", "أصول متداولة أخرى", "Other Current Assets", AccountType.Asset, 3,
            parentAccountId: l2["1100"].Id, colorCode: ColorAsset,
            description: "أصول متداولة أخرى",
            explanation: "الأصول المتداولة الأخرى"));
        level3.Add(Account.Create("1160", "تسوية المخزون", "Inventory Settlement", AccountType.Asset, 3,
            parentAccountId: l2["1100"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "تسويات وجرد المخزون الدوري — يسمح بحركات مباشرة",
            explanation: "تسويات وجرد المخزون الدوري"));

        // Under 1200 Fixed Assets
        level3.Add(Account.Create("1210", "أصول ثابتة ملموسة", "Tangible Fixed Assets", AccountType.Asset, 3,
            parentAccountId: l2["1200"].Id, colorCode: ColorAsset,
            description: "الأصول المادية طويلة الأجل",
            explanation: "الأصول المادية الثابتة للمنشأة"));
        level3.Add(Account.Create("1220", "أصول ثابتة غير ملموسة", "Intangible Fixed Assets", AccountType.Asset, 3,
            parentAccountId: l2["1200"].Id, colorCode: ColorAsset,
            description: "الأصول غير المادية طويلة الأجل",
            explanation: "الأصول غير المادية مثل البرمجيات والتراخيص"));
        level3.Add(Account.Create("1230", "مجمع الإهلاك", "Accumulated Depreciation", AccountType.Asset, 3,
            parentAccountId: l2["1200"].Id, colorCode: ColorAsset,
            description: "إهلاك الأصول الثابتة المتراكم",
            explanation: "إهلاك الأصول الثابتة المتراكم"));

        // Under 1310 Current Liabilities
        level3.Add(Account.Create("1320", "الموردون", "Accounts Payable", AccountType.Liability, 3,
            parentAccountId: l2["1310"].Id, colorCode: ColorLiability,
            description: "المبالغ المستحقة للموردين",
            explanation: "المبالغ المستحقة للموردين"));
        level3.Add(Account.Create("1330", "الضرائب", "Taxes Payable", AccountType.Liability, 3,
            parentAccountId: l2["1310"].Id, colorCode: ColorLiability,
            description: "ضرائب مستحقة للحكومة",
            explanation: "الضرائب المستحقة للحكومة"));
        level3.Add(Account.Create("1340", "التزامات متداولة أخرى", "Other Current Liabilities", AccountType.Liability, 3,
            parentAccountId: l2["1310"].Id, colorCode: ColorLiability,
            description: "التزامات متداولة أخرى",
            explanation: "الالتزامات المتداولة الأخرى"));

        // Under 1410 Capital & Reserves
        level3.Add(Account.Create("1411", "رأس المال", "Capital", AccountType.Equity, 3,
            parentAccountId: l2["1410"].Id, colorCode: ColorEquity, allowTransactions: true,
            description: "رأس مال المنشأة — يسمح بحركات مباشرة",
            explanation: "رأس مال المنشأة المستثمر"));

        // Under 1420 Profit & Loss
        level3.Add(Account.Create("1421", "أرباح مدورة", "Retained Earnings", AccountType.Equity, 3,
            parentAccountId: l2["1420"].Id, colorCode: ColorEquity, allowTransactions: true,
            description: "أرباح الفترات السابقة المدورة — يسمح بحركات مباشرة",
            explanation: "أرباح الفترات السابقة المدورة"));
        level3.Add(Account.Create("1422", "أرصدة افتتاحية", "Opening Balance Equity", AccountType.Equity, 3,
            parentAccountId: l2["1420"].Id, isSystemAccount: true, colorCode: ColorEquity, allowTransactions: true,
            description: "أرصدة افتتاحية للعملاء والموردين — يسمح بحركات مباشرة",
            explanation: "حساب يقابل أرصدة العملاء والموردين الافتتاحية عند بدء استخدام النظام — يتم ترحيل الرصيد الافتتاحي للعملاء مدينة لهذا الحساب والرصيد الافتتاحي للموردين دائنة"));

        // Under 1510 Operating Revenue
        level3.Add(Account.Create("1520", "إيرادات المبيعات", "Sales Revenue", AccountType.Revenue, 3,
            parentAccountId: l2["1510"].Id, colorCode: ColorRevenue, allowTransactions: true,
            description: "إيرادات بيع المنتجات والخدمات",
            explanation: "إيرادات بيع المنتجات والخدمات — تم دمج المبيعات النقدية والآجلة في حساب واحد"));
        level3.Add(Account.Create("1530", "إيرادات أخرى", "Other Revenue", AccountType.Revenue, 3,
            parentAccountId: l2["1510"].Id, colorCode: ColorRevenue, allowTransactions: true,
            description: "إيرادات غير تشغيلية متنوعة — يسمح بحركات مباشرة",
            explanation: "إيرادات غير تشغيلية متنوعة"));

        // Under 1610 Activity Costs
        level3.Add(Account.Create("1620", "تكلفة المبيعات", "Cost of Sales", AccountType.Expense, 3,
            parentAccountId: l2["1610"].Id, colorCode: ColorExpense,
            description: "تكلفة البضاعة والخدمات المباعة",
            explanation: "تكلفة البضاعة والخدمات المباعة"));
        level3.Add(Account.Create("1630", "المردودات", "Returns", AccountType.Expense, 3,
            parentAccountId: l2["1610"].Id, colorCode: ColorExpense,
            description: "مردودات المبيعات والمشتريات",
            explanation: "مردودات المبيعات من العملاء ومردودات المشتريات للموردين"));

        // Under 1670 Operating Expenses
        level3.Add(Account.Create("1680", "مصروفات عمومية وإدارية", "General & Admin Expenses", AccountType.Expense, 3,
            parentAccountId: l2["1670"].Id, colorCode: ColorExpense,
            description: "المصروفات الإدارية والتشغيلية",
            explanation: "المصروفات الإدارية والتشغيلية العامة"));
        level3.Add(Account.Create("1690", "مصروفات أخرى", "Other Expenses", AccountType.Expense, 3,
            parentAccountId: l2["1670"].Id, colorCode: ColorExpense, allowTransactions: true,
            description: "مصروفات غير تشغيلية متنوعة — يسمح بحركات مباشرة",
            explanation: "مصروفات غير تشغيلية متنوعة"));

        db.Set<Account>().AddRange(level3);
        await db.SaveChangesAsync();
        var l3 = await db.Set<Account>().ToDictionaryAsync(a => a.AccountCode);

        // ─── Level 4: Detail Accounts (26 accounts) ───────────────
        var level4 = new List<Account>();

        // Under 1110 Cash
        level4.Add(Account.Create("1111", "الصندوق", "Cash on Hand", AccountType.Asset, 4,
            parentAccountId: l3["1110"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "النقدية الموجودة في صندوق الخزينة",
            explanation: "النقدية الموجودة في صندوق الخزينة الرئيسي"));
        level4.Add(Account.Create("1112", "صندوق المصروفات النثرية", "Petty Cash", AccountType.Asset, 4,
            parentAccountId: l3["1110"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "نقدية المصروفات اليومية الصغيرة",
            explanation: "النقدية المخصصة للمصروفات اليومية الصغيرة"));

        // Under 1120 Banks
        level4.Add(Account.Create("1121", "البنك الأهلي", "National Bank", AccountType.Asset, 4,
            parentAccountId: l3["1120"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "الحساب الجاري في البنك الأهلي",
            explanation: "الحساب الجاري لدى البنك الأهلي"));
        level4.Add(Account.Create("1122", "بنك الرياض", "Riyad Bank", AccountType.Asset, 4,
            parentAccountId: l3["1120"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "الحساب الجاري في بنك الرياض",
            explanation: "الحساب الجاري لدى بنك الرياض"));

        // Under 1130 Accounts Receivable
        level4.Add(Account.Create("1131", "العميل النقدي", "Cash Customer", AccountType.Asset, 4,
            parentAccountId: l3["1130"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "عملاء الدفع النقدي",
            explanation: "حساب عملاء الدفع النقدي"));
        level4.Add(Account.Create("1132", "عملاء آجلون", "Credit Customers", AccountType.Asset, 4,
            parentAccountId: l3["1130"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "عملاء نظام الآجل",
            explanation: "حساب عملاء نظام البيع الآجل"));
        level4.Add(Account.Create("1133", "مخصص الديون المشكوك فيها", "Allowance for Doubtful Debts", AccountType.Asset, 4,
            parentAccountId: l3["1130"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "مخصص الديون المشكوك في تحصيلها",
            explanation: "مخصص الديون المشكوك في تحصيلها"));

        // Under 1140 Inventory
        level4.Add(Account.Create("1141", "بضاعة أول المدة", "Opening Inventory", AccountType.Asset, 4,
            parentAccountId: l3["1140"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "قيمة المخزون في بداية الفترة المالية",
            explanation: "قيمة المخزون في بداية الفترة المالية"));
        level4.Add(Account.Create("1142", "مخزون آخر المدة", "Closing Inventory", AccountType.Asset, 4,
            parentAccountId: l3["1140"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "قيمة المخزون في نهاية الفترة المالية",
            explanation: "قيمة المخزون في نهاية الفترة المالية"));

        // Under 1150 Other Current Assets
        level4.Add(Account.Create("1151", "مصروفات مدفوعة مقدماً", "Prepaid Expenses", AccountType.Asset, 4,
            parentAccountId: l3["1150"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "مصروفات مدفوعة عن فترات مستقبلية",
            explanation: "مصروفات مدفوعة عن فترات مستقبلية"));
        level4.Add(Account.Create("1152", "أوراق قبض", "Notes Receivable", AccountType.Asset, 4,
            parentAccountId: l3["1150"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "كمبيالات وأوراق مالية مستحقة القبض",
            explanation: "كمبيالات وأوراق مالية مستحقة القبض"));

        // Under 1210 Tangible Fixed Assets
        level4.Add(Account.Create("1211", "أثاث ومعدات", "Furniture & Equipment", AccountType.Asset, 4,
            parentAccountId: l3["1210"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "أثاث وتجهيزات ومعدات",
            explanation: "أثاث وتجهيزات ومعدات المنشأة"));
        level4.Add(Account.Create("1212", "أجهزة حاسب آلي", "Computer Equipment", AccountType.Asset, 4,
            parentAccountId: l3["1210"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "أجهزة حاسب وملحقاتها",
            explanation: "أجهزة الحاسب الآلي وملحقاتها"));

        // Under 1220 Intangible Fixed Assets
        level4.Add(Account.Create("1221", "برمجيات", "Software", AccountType.Asset, 4,
            parentAccountId: l3["1220"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "برامج الحاسب والتراخيص",
            explanation: "برامج الحاسب والتراخيص"));

        // Under 1230 Accumulated Depreciation
        level4.Add(Account.Create("1231", "مخصص إهلاك الأثاث", "Accum. Dep. Furniture", AccountType.Asset, 4,
            parentAccountId: l3["1230"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "إهلاك الأثاث والتجهيزات المتراكم",
            explanation: "إهلاك الأثاث والتجهيزات المتراكم"));
        level4.Add(Account.Create("1232", "مخصص إهلاك الحاسب", "Accum. Dep. Computers", AccountType.Asset, 4,
            parentAccountId: l3["1230"].Id, colorCode: ColorAsset, allowTransactions: true,
            description: "إهلاك أجهزة الحاسب المتراكم",
            explanation: "إهلاك أجهزة الحاسب المتراكم"));

        // Under 1320 Accounts Payable
        level4.Add(Account.Create("1321", "المورد النقدي", "Cash Supplier", AccountType.Liability, 4,
            parentAccountId: l3["1320"].Id, colorCode: ColorLiability, allowTransactions: true,
            description: "موردي الدفع النقدي",
            explanation: "حساب موردي الدفع النقدي"));
        level4.Add(Account.Create("1322", "موردون آجلون", "Credit Suppliers", AccountType.Liability, 4,
            parentAccountId: l3["1320"].Id, colorCode: ColorLiability, allowTransactions: true,
            description: "موردي نظام الآجل",
            explanation: "حساب موردي نظام الشراء الآجل"));

        // Under 1330 Taxes
        level4.Add(Account.Create("1331", "ضريبة المبيعات (خرج)", "VAT Output", AccountType.Liability, 4,
            parentAccountId: l3["1330"].Id, colorCode: ColorLiability, allowTransactions: true,
            description: "ضريبة القيمة المضافة المحصلة من المبيعات والمستحقة للحكومة",
            explanation: "ضريبة القيمة المضافة المحصلة على المبيعات"));
        level4.Add(Account.Create("1332", "ضريبة المشتريات (دخل)", "VAT Input", AccountType.Liability, 4,
            parentAccountId: l3["1330"].Id, colorCode: ColorLiability, allowTransactions: true,
            description: "ضريبة القيمة المضافة المدفوعة على المشتريات والقابلة للاسترداد",
            explanation: "ضريبة القيمة المضافة المدفوعة على المشتريات"));

        // Under 1340 Other Current Liabilities
        level4.Add(Account.Create("1341", "أوراق دفع", "Notes Payable", AccountType.Liability, 4,
            parentAccountId: l3["1340"].Id, colorCode: ColorLiability, allowTransactions: true,
            description: "كمبيالات وأوراق مالية مستحقة الدفع",
            explanation: "كمبيالات وأوراق مالية مستحقة الدفع"));

        // Under 1620 Cost of Sales
        level4.Add(Account.Create("1621", "تكلفة البضاعة المباعة", "Cost of Goods Sold", AccountType.Expense, 4,
            parentAccountId: l3["1620"].Id, colorCode: ColorExpense, allowTransactions: true,
            description: "تكلفة البضاعة المباعة خلال الفترة",
            explanation: "تكلفة البضاعة المباعة خلال الفترة"));

        // Under 1630 Returns
        level4.Add(Account.Create("1631", "مردودات مبيعات", "Sales Returns", AccountType.Expense, 4,
            parentAccountId: l3["1630"].Id, colorCode: ColorExpense, allowTransactions: true,
            description: "مردودات البضاعة المباعة",
            explanation: "مردودات البضاعة المباعة من العملاء"));
        level4.Add(Account.Create("1632", "مردودات مشتريات", "Purchase Returns", AccountType.Expense, 4,
            parentAccountId: l3["1630"].Id, colorCode: ColorExpense, allowTransactions: true,
            description: "مردودات المشتريات للموردين",
            explanation: "مردودات المشتريات المرتجعة للموردين"));

        // Under 1680 General & Admin Expenses
        level4.Add(Account.Create("1681", "مصروفات عمومية", "General Expenses", AccountType.Expense, 4,
            parentAccountId: l3["1680"].Id, colorCode: ColorExpense, allowTransactions: true,
            description: "المصروفات الإدارية والتشغيلية العامة",
            explanation: "المصروفات الإدارية والتشغيلية العامة"));

        // Under 1690 Other Expenses
        level4.Add(Account.Create("1691", "هالك المخزون", "Spoilage Loss", AccountType.Expense, 4,
            parentAccountId: l3["1690"].Id, colorCode: ColorExpense, allowTransactions: true,
            description: "خسائر التلف والهلاك في المخزون",
            explanation: "خسائر التلف والهلاك في المخزون"));
        level4.Add(Account.Create("1692", "عجز مخزون", "Inventory Shortage", AccountType.Expense, 4,
            parentAccountId: l3["1690"].Id, colorCode: ColorExpense, allowTransactions: true,
            description: "نقص في المخزون ينتج عن الجرد الدوري",
            explanation: "عجز المخزون الناتج عن أخطاء الجرد أو السرقة أو التلف غير المسجل"));
        level4.Add(Account.Create("1693", "زيادة مخزون", "Inventory Surplus", AccountType.Revenue, 4,
            parentAccountId: l3["1690"].Id, colorCode: ColorRevenue, allowTransactions: true,
            description: "زيادة في المخزون ينتج عن الجرد الدوري",
            explanation: "زيادة المخزون الناتجة عن أخطاء جرد سابقة أو إدخال غير مسجل"));

        db.Set<Account>().AddRange(level4);
        await db.SaveChangesAsync();

        var totalCount = 5 + 8 + 21 + 28; // 62
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

        // ─── Seed system account mappings (key-value pattern) ────
        var mappingList = new List<SystemAccountMapping>
        {
            SystemAccountMapping.Create(SystemAccountKey.DefaultCash, cashAccount.Id,
                descriptionAr: "الصندوق الافتراضي", descriptionEn: "Default Cash"),
            SystemAccountMapping.Create(SystemAccountKey.DefaultBank, bankAccount.Id,
                descriptionAr: "البنك الافتراضي", descriptionEn: "Default Bank"),
            SystemAccountMapping.Create(SystemAccountKey.AccountsReceivable, arAccount.Id,
                descriptionAr: "حساب العملاء", descriptionEn: "Accounts Receivable"),
            SystemAccountMapping.Create(SystemAccountKey.AccountsPayable, apAccount.Id,
                descriptionAr: "حساب الموردين", descriptionEn: "Accounts Payable"),
            SystemAccountMapping.Create(SystemAccountKey.Inventory, inventoryAccount.Id,
                descriptionAr: "أصل المخزون", descriptionEn: "Inventory Asset"),
            SystemAccountMapping.Create(SystemAccountKey.CostOfGoodsSold, cogsAccount.Id,
                descriptionAr: "تكلفة البضاعة المباعة", descriptionEn: "Cost of Goods Sold"),
            SystemAccountMapping.Create(SystemAccountKey.SalesRevenue, salesRevenueAccount.Id,
                descriptionAr: "إيرادات المبيعات", descriptionEn: "Sales Revenue"),
            SystemAccountMapping.Create(SystemAccountKey.SalesReturns, salesReturnAccount.Id,
                descriptionAr: "مردودات المبيعات", descriptionEn: "Sales Returns"),
            SystemAccountMapping.Create(SystemAccountKey.PurchaseReturns, purchaseReturnAccount.Id,
                descriptionAr: "مردودات المشتريات", descriptionEn: "Purchase Returns"),
            SystemAccountMapping.Create(SystemAccountKey.VatOutput, vatOutputAccount.Id,
                descriptionAr: "ضريبة المخرجات", descriptionEn: "VAT Output"),
            SystemAccountMapping.Create(SystemAccountKey.VatInput, vatInputAccount.Id,
                descriptionAr: "ضريبة المدخلات", descriptionEn: "VAT Input"),
            SystemAccountMapping.Create(SystemAccountKey.Capital, capitalAccount.Id,
                descriptionAr: "رأس المال", descriptionEn: "Capital"),
            SystemAccountMapping.Create(SystemAccountKey.OpeningBalanceEquity, openingBalanceEquityAccount.Id,
                descriptionAr: "حقوق ملكية الأرصدة الافتتاحية", descriptionEn: "Opening Balance Equity"),
            SystemAccountMapping.Create(SystemAccountKey.RetainedEarnings, allAccounts["1421"].Id,
                descriptionAr: "الأرباح المحتجزة", descriptionEn: "Retained Earnings"),
            SystemAccountMapping.Create(SystemAccountKey.InventoryShortage, shortageAccount.Id,
                descriptionAr: "عجز المخزون", descriptionEn: "Inventory Shortage"),
            SystemAccountMapping.Create(SystemAccountKey.InventorySurplus, surplusAccount.Id,
                descriptionAr: "زيادة المخزون", descriptionEn: "Inventory Surplus"),
        };

        db.Set<SystemAccountMapping>().AddRange(mappingList);

        await db.SaveChangesAsync();
        logger?.LogInformation("System account mappings seeded: {Count} keys.", mappingList.Count);
    }
}
