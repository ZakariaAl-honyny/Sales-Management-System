using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds the chart of accounts (81 accounts across 4 levels) and global SystemAccountMappings.
/// Idempotent — skips if the Accounts table already has records.
/// Uses a four-pass approach: seeds Level 1 → saves → queries Ids → Level 2 → saves → Level 3 → saves → Level 4 → saves.
/// Hierarchical code scheme: Level 1 = 1 digit, Level 2 = 2 digits, Level 3 = 4 digits, Level 4 = 8 digits.
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

        const string assetColor = "#2196F3";
        const string liabilityColor = "#F44336";
        const string equityColor = "#4CAF50";
        const string revenueColor = "#4CAF50";
        const string expenseColor = "#FF9800";

        // ─── Level 1: Groups (5 accounts, level=1, isLeaf=false, isSystem=true) ───
        var level1 = new List<Account>
        {
            Account.Create("1", "الأصول", "Assets",
                nature: (byte)AccountType.Asset, isLeaf: false, isSystem: true, level: 1,
                description: "يمثل هذا الحساب جميع الموارد الاقتصادية التي تملكها المنشأة كالنقدية والبنوك والمخزون والأصول الثابتة.",
                colorCode: assetColor),

            Account.Create("2", "الخصوم", "Liabilities",
                nature: (byte)AccountType.Liability, isLeaf: false, isSystem: true, level: 1,
                description: "يمثل هذا الحساب جميع الالتزامات المالية المستحقة على المنشأة للغير كالموردين والضرائب.",
                colorCode: liabilityColor),

            Account.Create("3", "حقوق الملكية", "Equity",
                nature: (byte)AccountType.Equity, isLeaf: false, isSystem: true, level: 1,
                description: "يمثل هذا الحساب حقوق المالكين في المنشأة شاملاً رأس المال والاحتياطيات والأرباح المدورة.",
                colorCode: equityColor),

            Account.Create("4", "الإيرادات", "Revenue",
                nature: (byte)AccountType.Revenue, isLeaf: false, isSystem: true, level: 1,
                description: "يمثل هذا الحساب جميع الإيرادات التي تحققها المنشأة من نشاطها الرئيسي والأنشطة الأخرى.",
                colorCode: revenueColor),

            Account.Create("5", "المصروفات", "Expenses",
                nature: (byte)AccountType.Expense, isLeaf: false, isSystem: true, level: 1,
                description: "يمثل هذا الحساب جميع المصروفات التي تتكبدها المنشأة في سبيل تحقيق الإيرادات.",
                colorCode: expenseColor),
        };

        db.Set<Account>().AddRange(level1);
        await db.SaveChangesAsync();
        var l1 = await db.Set<Account>().ToDictionaryAsync(a => a.AccountCode);
        logger?.LogInformation("Level 1 seeded: {Count} group accounts.", level1.Count);

        // ─── Level 2: Main Categories (8 accounts, level=2, isLeaf=false, isSystem=true) ───
        var level2 = new List<Account>
        {
            // Under "1" Assets
            Account.Create("11", "أصول متداولة", "Current Assets",
                nature: (byte)AccountType.Asset, isLeaf: false,
                parentId: l1["1"].Id, isSystem: true, level: 2,
                description: "يشمل الأصول التي تتوقع المنشأة تحويلها إلى نقد أو بيعها أو استهلاكها خلال دورة التشغيل العادية.",
                colorCode: assetColor),

            Account.Create("12", "أصول ثابتة", "Fixed Assets",
                nature: (byte)AccountType.Asset, isLeaf: false,
                parentId: l1["1"].Id, isSystem: true, level: 2,
                description: "يشمل الأصول طويلة الأجل التي تستخدمها المنشأة في عملياتها ولا تتوقع بيعها ضمن دورة التشغيل العادية.",
                colorCode: assetColor),

            // Under "2" Liabilities
            Account.Create("21", "التزامات متداولة", "Current Liabilities",
                nature: (byte)AccountType.Liability, isLeaf: false,
                parentId: l1["2"].Id, isSystem: true, level: 2,
                description: "يشمل الالتزامات التي تستحق السداد خلال دورة التشغيل العادية أو خلال سنة من تاريخ الميزانية.",
                colorCode: liabilityColor),

            // Under "3" Equity
            Account.Create("31", "رأس المال والاحتياطيات", "Capital & Reserves",
                nature: (byte)AccountType.Equity, isLeaf: false,
                parentId: l1["3"].Id, isSystem: true, level: 2,
                description: "يشمل الحسابات المتعلقة برأس مال المنشأة والمسحوبات والاحتياطيات المختلفة.",
                colorCode: equityColor),

            Account.Create("32", "الأرباح والخسائر", "Profit & Loss",
                nature: (byte)AccountType.Equity, isLeaf: false,
                parentId: l1["3"].Id, isSystem: true, level: 2,
                description: "يشمل الحسابات المتعلقة بالأرباح المدورة والأرصدة الافتتاحية والأرباح غير الموزعة المستخدمة في الإقفال السنوي.",
                colorCode: equityColor),

            // Under "4" Revenue
            Account.Create("41", "إيرادات النشاط", "Operating Revenue",
                nature: (byte)AccountType.Revenue, isLeaf: false,
                parentId: l1["4"].Id, isSystem: true, level: 2,
                description: "يشمل الإيرادات الناتجة عن النشاط الرئيسي للمنشأة كمبيعات المنتجات والخدمات.",
                colorCode: revenueColor),

            // Under "5" Expenses
            Account.Create("51", "تكاليف النشاط", "Activity Costs",
                nature: (byte)AccountType.Expense, isLeaf: false,
                parentId: l1["5"].Id, isSystem: true, level: 2,
                description: "يشمل التكاليف المباشرة المرتبطة بنشاط المنشأة كتكلفة المبيعات والمردودات.",
                colorCode: expenseColor),

            Account.Create("52", "مصاريف تشغيلية وإدارية", "Operating Expenses",
                nature: (byte)AccountType.Expense, isLeaf: false,
                parentId: l1["5"].Id, isSystem: true, level: 2,
                description: "يشمل المصروفات التشغيلية والإدارية للمنشأة كالرواتب والإيجارات والمرافق العامة.",
                colorCode: expenseColor),
        };

        db.Set<Account>().AddRange(level2);
        await db.SaveChangesAsync();
        var l2 = await db.Set<Account>().ToDictionaryAsync(a => a.AccountCode);
        logger?.LogInformation("Level 2 seeded: {Count} main category accounts.", level2.Count);

        // ─── Level 3: Sub Categories (24 accounts, level=3, isLeaf=false, not isSystem) ───
        var level3 = new List<Account>();

        // Under "11" Current Assets
        level3.Add(Account.Create("1101", "النقدية", "Cash & Cash Equivalents",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["11"].Id, level: 3,
            description: "يشمل النقدية الموجودة في صندوق المنشأة والبنوك وما في حكمها من أموال سائلة.",
            colorCode: assetColor));

        level3.Add(Account.Create("1102", "البنوك", "Bank Accounts",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["11"].Id, level: 3,
            description: "يشمل الحسابات الجارية والتوفير لدى البنوك المختلفة.",
            colorCode: assetColor));

        level3.Add(Account.Create("1103", "العملاء", "Accounts Receivable",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["11"].Id, level: 3,
            description: "يشمل المبالغ المستحقة للمنشأة لدى العملاء مقابل مبيعات آجلة.",
            colorCode: assetColor));

        level3.Add(Account.Create("1104", "المخزون", "Inventory",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["11"].Id, level: 3,
            description: "يشمل قيمة البضاعة الموجودة لدى المنشأة في بداية ونهاية الفترة المحاسبية.",
            colorCode: assetColor));

        level3.Add(Account.Create("1105", "أصول متداولة أخرى", "Other Current Assets",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["11"].Id, level: 3,
            description: "يشمل الأصول المتداولة الأخرى غير المصنفة ضمن النقدية والبنوك والعملاء والمخزون كمصروفات مدفوعة مقدماً.",
            colorCode: assetColor));

        level3.Add(Account.Create("1106", "تسوية المخزون", "Inventory Settlement",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["11"].Id, level: 3,
            description: "حساب مؤقت يستخدم لتسوية فروق المخزون بين النظام والجرد الفعلي.",
            colorCode: assetColor));

        level3.Add(Account.Create("1107", "عهد الموظفين", "Employee Custody",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["11"].Id, level: 3,
            description: "يشمل المبالغ الممنوحة للموظفين كعهد أو سلف لاغراض العمل.",
            colorCode: assetColor));

        // Under "12" Fixed Assets
        level3.Add(Account.Create("1201", "أصول ثابتة ملموسة", "Tangible Fixed Assets",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["12"].Id, level: 3,
            description: "يشمل الأصول الثابتة المادية كالأثاث والمعدات وأجهزة الحاسب.",
            colorCode: assetColor));

        level3.Add(Account.Create("1202", "أصول ثابتة غير ملموسة", "Intangible Fixed Assets",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["12"].Id, level: 3,
            description: "يشمل الأصول غير المادية كالبرمجيات وحقوق الملكية الفكرية.",
            colorCode: assetColor));

        level3.Add(Account.Create("1203", "مجمع الإهلاك", "Accumulated Depreciation",
            nature: (byte)AccountType.Asset, isLeaf: false, parentId: l2["12"].Id, level: 3,
            description: "يشمل إهلاك الأصول الثابتة الملموسة المتراكم عبر عمرها الإنتاجي مع مخصصات الإهلاك.",
            colorCode: assetColor));

        // Under "21" Current Liabilities
        level3.Add(Account.Create("2101", "الموردون", "Accounts Payable",
            nature: (byte)AccountType.Liability, isLeaf: false, parentId: l2["21"].Id, level: 3,
            description: "يشمل المبالغ المستحقة للموردين مقابل مشتريات آجلة.",
            colorCode: liabilityColor));

        level3.Add(Account.Create("2102", "الضرائب", "Taxes Payable",
            nature: (byte)AccountType.Liability, isLeaf: false, parentId: l2["21"].Id, level: 3,
            description: "يشمل الضرائب المستحقة للجهات الحكومية كضريبة المبيعات وضريبة المشتريات.",
            colorCode: liabilityColor));

        level3.Add(Account.Create("2103", "التزامات متداولة أخرى", "Other Current Liabilities",
            nature: (byte)AccountType.Liability, isLeaf: false, parentId: l2["21"].Id, level: 3,
            description: "يشمل الالتزامات المتداولة الأخرى غير المصنفة كثمن الأوراق الدائنة.",
            colorCode: liabilityColor));

        // Under "31" Capital & Reserves
        level3.Add(Account.Create("3101", "رأس المال", "Capital",
            nature: (byte)AccountType.Equity, isLeaf: false, parentId: l2["31"].Id, level: 3,
            description: "يشمل حساب رأس المال المستثمر في المنشأة.",
            colorCode: equityColor));

        level3.Add(Account.Create("3102", "المسحوبات", "Drawings",
            nature: (byte)AccountType.Equity, isLeaf: false, parentId: l2["31"].Id, level: 3,
            description: "يشمل مسحوبات المالك من المنشأة للاستخدام الشخصي.",
            colorCode: equityColor));

        // Under "32" Profit & Loss
        level3.Add(Account.Create("3201", "أرباح مدورة", "Retained Earnings",
            nature: (byte)AccountType.Equity, isLeaf: false, parentId: l2["32"].Id, level: 3,
            description: "يشمل الأرباح المتراكمة للمنشأة من الفترات السابقة.",
            colorCode: equityColor));

        level3.Add(Account.Create("3202", "أرصدة افتتاحية", "Opening Balance Equity",
            nature: (byte)AccountType.Equity, isLeaf: false, parentId: l2["32"].Id, level: 3,
            description: "حساب مؤقت يستخدم في بداية النظام لترحيل الأرصدة الافتتاحية قبل بدء التشغيل.",
            colorCode: equityColor));

        level3.Add(Account.Create("3203", "أرباح غير موزعة", "Undistributed Profits",
            nature: (byte)AccountType.Equity, isLeaf: false, parentId: l2["32"].Id, level: 3,
            description: "يشمل الأرباح غير الموزعة المستخدمة في الإقفال السنوي للحسابات.",
            colorCode: equityColor));

        // Under "41" Operating Revenue
        level3.Add(Account.Create("4101", "إيرادات المبيعات", "Sales Revenue",
            nature: (byte)AccountType.Revenue, isLeaf: false, parentId: l2["41"].Id, level: 3,
            description: "يشمل الإيرادات الناتجة عن بيع المنتجات والسلع للعملاء.",
            colorCode: revenueColor));

        level3.Add(Account.Create("4102", "إيرادات أخرى", "Other Revenue",
            nature: (byte)AccountType.Revenue, isLeaf: false, parentId: l2["41"].Id, level: 3,
            description: "يشمل الإيرادات غير التشغيلية كإيرادات النقل والخصم المكتسب وإيرادات التوصيل.",
            colorCode: revenueColor));

        // Under "51" Activity Costs
        level3.Add(Account.Create("5101", "تكلفة المبيعات", "Cost of Sales",
            nature: (byte)AccountType.Expense, isLeaf: false, parentId: l2["51"].Id, level: 3,
            description: "يشمل تكلفة البضاعة المباعة خلال الفترة المحاسبية ويمثل المصروف الرئيسي لنشاط المنشأة.",
            colorCode: expenseColor));

        level3.Add(Account.Create("5102", "المردودات", "Returns",
            nature: (byte)AccountType.Expense, isLeaf: false, parentId: l2["51"].Id, level: 3,
            description: "يشمل مردودات المبيعات ومردودات المشتريات من العملاء والموردين.",
            colorCode: expenseColor));

        // Under "52" Operating Expenses
        level3.Add(Account.Create("5201", "مصروفات عمومية وإدارية", "General & Admin Expenses",
            nature: (byte)AccountType.Expense, isLeaf: false, parentId: l2["52"].Id, level: 3,
            description: "يشمل المصروفات الإدارية والتشغيلية كالرواتب والإيجارات والكهرباء والمياه.",
            colorCode: expenseColor));

        level3.Add(Account.Create("5202", "مصروفات أخرى", "Other Expenses",
            nature: (byte)AccountType.Expense, isLeaf: false, parentId: l2["52"].Id, level: 3,
            description: "يشمل المصروفات غير التشغيلية كهالك المخزون وعجز المخزون وزيادة المخزون.",
            colorCode: expenseColor));

        db.Set<Account>().AddRange(level3);
        await db.SaveChangesAsync();
        var l3 = await db.Set<Account>().ToDictionaryAsync(a => a.AccountCode);
        logger?.LogInformation("Level 3 seeded: {Count} sub-category accounts.", level3.Count);

        // ─── Level 4: Detail Accounts (44 accounts, level=4, isLeaf=true, not isSystem) ───
        var level4 = new List<Account>();

        // Under "1101" Cash & Cash Equivalents
        level4.Add(Account.Create("11010001", "الصندوق", "Cash on Hand",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1101"].Id, level: 4,
            description: "يمثل النقدية الموجودة فعلياً في خزينة المنشأة.",
            colorCode: assetColor));

        level4.Add(Account.Create("11010002", "صندوق المصروفات النثرية", "Petty Cash",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1101"].Id, level: 4,
            description: "يمثل المبلغ المخصص للمصروفات اليومية الصغيرة والنثرية.",
            colorCode: assetColor));

        // Under "1102" Bank Accounts
        level4.Add(Account.Create("11020001", "البنك الأهلي", "National Bank",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1102"].Id, level: 4,
            description: "الحساب الجاري لدى البنك الأهلي.",
            colorCode: assetColor));

        level4.Add(Account.Create("11020002", "بنك الرياض", "Riyad Bank",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1102"].Id, level: 4,
            description: "الحساب الجاري لدى بنك الرياض.",
            colorCode: assetColor));

        // Under "1103" Accounts Receivable
        level4.Add(Account.Create("11030001", "العميل النقدي", "Cash Customer",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1103"].Id, level: 4,
            description: "حساب مبيعات العملاء النقديين يتم الترحيل إليه عند بيع النقدي.",
            colorCode: assetColor));

        level4.Add(Account.Create("11030002", "عملاء آجلون", "Credit Customers",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1103"].Id, level: 4,
            description: "حساب المبيعات الآجلة للعملاء الذين لهم حسابات مفتوحة.",
            colorCode: assetColor));

        level4.Add(Account.Create("11030003", "مخصص الديون المشكوك فيها", "Allowance for Doubtful Debts",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1103"].Id, level: 4,
            description: "مخصص للديون التي يشك في تحصيلها مستقبلاً ويظهر كحساب مقابل للأصول.",
            colorCode: assetColor));

        // Under "1104" Inventory
        level4.Add(Account.Create("11040001", "بضاعة أول المدة", "Opening Inventory",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1104"].Id, level: 4,
            description: "قيمة المخزون في بداية الفترة المحاسبية.",
            colorCode: assetColor));

        level4.Add(Account.Create("11040002", "مخزون آخر المدة", "Closing Inventory",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1104"].Id, level: 4,
            description: "قيمة المخزون في نهاية الفترة المحاسبية بعد إجراء الجرد.",
            colorCode: assetColor));

        // Under "1105" Other Current Assets
        level4.Add(Account.Create("11050001", "مصروفات مدفوعة مقدماً", "Prepaid Expenses",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1105"].Id, level: 4,
            description: "المصروفات التي تم دفعها مقدماً عن فترات قادمة كالتأمين والإيجار المدفوع مقدماً.",
            colorCode: assetColor));

        level4.Add(Account.Create("11050002", "أوراق قبض", "Notes Receivable",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1105"].Id, level: 4,
            description: "كمبيالات وأوراق تجارية مستحقة للمنشأة لدى الغير.",
            colorCode: assetColor));

        // Under "1107" Employee Custody
        level4.Add(Account.Create("11070001", "عهدة الموظفين", "Employee Custody Detail",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1107"].Id, level: 4,
            description: "تفاصيل العهد والسلف الممنوحة للموظفين.",
            colorCode: assetColor));

        // Under "1201" Tangible Fixed Assets
        level4.Add(Account.Create("12010001", "أثاث ومعدات", "Furniture & Equipment",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1201"].Id, level: 4,
            description: "قيمة الأثاث والمعدات المكتبية.",
            colorCode: assetColor));

        level4.Add(Account.Create("12010002", "أجهزة حاسب آلي", "Computer Equipment",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1201"].Id, level: 4,
            description: "قيمة أجهزة الحاسب الآلي وملحقاتها.",
            colorCode: assetColor));

        // Under "1202" Intangible Fixed Assets
        level4.Add(Account.Create("12020001", "برمجيات", "Software",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1202"].Id, level: 4,
            description: "قيمة البرمجيات وبرامج الحاسب وتراخيصها.",
            colorCode: assetColor));

        // Under "1203" Accumulated Depreciation
        level4.Add(Account.Create("12030001", "مخصص إهلاك الأثاث", "Accum. Dep. Furniture",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1203"].Id, level: 4,
            description: "مجمع إهلاك الأثاث والمعدات المتراكم.",
            colorCode: assetColor));

        level4.Add(Account.Create("12030002", "مخصص إهلاك الحاسب", "Accum. Dep. Computers",
            nature: (byte)AccountType.Asset, isLeaf: true, parentId: l3["1203"].Id, level: 4,
            description: "مجمع إهلاك أجهزة الحاسب الآلي المتراكم.",
            colorCode: assetColor));

        // Under "2101" Accounts Payable
        level4.Add(Account.Create("21010001", "المورد النقدي", "Cash Supplier",
            nature: (byte)AccountType.Liability, isLeaf: true, parentId: l3["2101"].Id, level: 4,
            description: "حساب الموردين الذين يتم السداد لهم نقداً.",
            colorCode: liabilityColor));

        level4.Add(Account.Create("21010002", "موردون آجلون", "Credit Suppliers",
            nature: (byte)AccountType.Liability, isLeaf: true, parentId: l3["2101"].Id, level: 4,
            description: "حساب الموردين الذين لهم حسابات آجلة مفتوحة لدى المنشأة.",
            colorCode: liabilityColor));

        // Under "2102" Taxes Payable
        level4.Add(Account.Create("21020001", "ضريبة المبيعات (خرج)", "VAT Output",
            nature: (byte)AccountType.Liability, isLeaf: true, parentId: l3["2102"].Id, level: 4,
            description: "ضريبة القيمة المضافة المحصلة من العملاء على المبيعات وتسدد للهيئة.",
            colorCode: liabilityColor));

        level4.Add(Account.Create("21020002", "ضريبة المشتريات (دخل)", "VAT Input",
            nature: (byte)AccountType.Liability, isLeaf: true, parentId: l3["2102"].Id, level: 4,
            description: "ضريبة القيمة المضافة المسددة للموردين على المشتريات وتخصم من المستحق للهيئة.",
            colorCode: liabilityColor));

        // Under "2103" Other Current Liabilities
        level4.Add(Account.Create("21030001", "أوراق دفع", "Notes Payable",
            nature: (byte)AccountType.Liability, isLeaf: true, parentId: l3["2103"].Id, level: 4,
            description: "كمبيالات وأوراق تجارية مستحقة على المنشأة للغير.",
            colorCode: liabilityColor));

        // Under "3101" Capital
        level4.Add(Account.Create("31010001", "رأس المال", "Capital Account",
            nature: (byte)AccountType.Equity, isLeaf: true, parentId: l3["3101"].Id, level: 4,
            description: "رأس المال المستثمر في المنشأة من قبل المالك أو الشركاء.",
            colorCode: equityColor));

        // Under "3102" Drawings
        level4.Add(Account.Create("31020001", "المسحوبات الشخصية", "Owner Drawings",
            nature: (byte)AccountType.Equity, isLeaf: true, parentId: l3["3102"].Id, level: 4,
            description: "المبالغ أو الأصول التي يسحبها المالك من المنشأة للاستخدام الشخصي.",
            colorCode: equityColor));

        // Under "3201" Retained Earnings
        level4.Add(Account.Create("32010001", "أرباح مدورة", "Retained Earnings Account",
            nature: (byte)AccountType.Equity, isLeaf: true, parentId: l3["3201"].Id, level: 4,
            description: "الأرباح المتراكمة للمنشأة من الفترات السابقة التي لم توزع بعد.",
            colorCode: equityColor));

        // Under "3202" Opening Balance Equity (isSystem=true — protected)
        level4.Add(Account.Create("32020001", "رصيد افتتاحي", "Opening Balance Equity Detail",
            nature: (byte)AccountType.Equity, isLeaf: true, parentId: l3["3202"].Id, level: 4,
            isSystem: true,
            description: "حساب مؤقت يستخدم لترحيل الأرصدة الافتتاحية عند بدء استخدام النظام المحاسبي.",
            colorCode: equityColor));

        // Under "3203" Undistributed Profits (isSystem=true — protected)
        level4.Add(Account.Create("32030001", "أرباح غير موزعة", "Undistributed Profits Detail",
            nature: (byte)AccountType.Equity, isLeaf: true, parentId: l3["3203"].Id, level: 4,
            isSystem: true,
            description: "الأرباح غير الموزعة التي ترحل نهاية السنة المالية في عملية الإقفال السنوي.",
            colorCode: equityColor));

        // Under "4101" Sales Revenue
        level4.Add(Account.Create("41010001", "إيرادات المبيعات", "Sales Revenue Account",
            nature: (byte)AccountType.Revenue, isLeaf: true, parentId: l3["4101"].Id, level: 4,
            description: "إيرادات بيع المنتجات والسلع للعملاء سواء نقداً أو آجلاً.",
            colorCode: revenueColor));

        // Under "4102" Other Revenue
        level4.Add(Account.Create("41020001", "إيراد النقل", "Transport Revenue",
            nature: (byte)AccountType.Revenue, isLeaf: true, parentId: l3["4102"].Id, level: 4,
            description: "الإيرادات الناتجة عن خدمات النقل والتوصيل.",
            colorCode: revenueColor));

        level4.Add(Account.Create("41020002", "الخصم المكتسب", "Discount Received",
            nature: (byte)AccountType.Revenue, isLeaf: true, parentId: l3["4102"].Id, level: 4,
            description: "الخصومات التي تحصل عليها المنشأة من الموردين عند السداد المبكر.",
            colorCode: revenueColor));

        level4.Add(Account.Create("41020003", "إيرادات التوصيل", "Delivery Charges Revenue",
            nature: (byte)AccountType.Revenue, isLeaf: true, parentId: l3["4102"].Id, level: 4,
            description: "الإيرادات الناتجة عن رسوم التوصيل التي يتحملها العملاء.",
            colorCode: revenueColor));

        // Under "5101" Cost of Sales
        level4.Add(Account.Create("51010001", "تكلفة البضاعة المباعة", "Cost of Goods Sold",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["5101"].Id, level: 4,
            description: "التكلفة المباشرة للبضاعة التي تم بيعها خلال الفترة المحاسبية.",
            colorCode: expenseColor));

        // Under "5102" Returns
        level4.Add(Account.Create("51020001", "مردودات مبيعات", "Sales Returns",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["5102"].Id, level: 4,
            description: "قيمة البضاعة التي أعادها العملاء بعد بيعها وتخفيض إيرادات المبيعات.",
            colorCode: expenseColor));

        level4.Add(Account.Create("51020002", "مردودات مشتريات", "Purchase Returns",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["5102"].Id, level: 4,
            description: "قيمة البضاعة التي أعادتها المنشأة للموردين بعد شرائها.",
            colorCode: expenseColor));

        // Under "5201" General & Admin Expenses
        level4.Add(Account.Create("52010001", "مصروفات عمومية", "General Expenses",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["5201"].Id, level: 4,
            description: "المصروفات العمومية والإدارية العامة للمنشأة.",
            colorCode: expenseColor));

        level4.Add(Account.Create("52010002", "الرواتب والأجور", "Salaries & Wages",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["5201"].Id, level: 4,
            description: "الرواتب والأجور المدفوعة للموظفين والعاملين.",
            colorCode: expenseColor));

        level4.Add(Account.Create("52010003", "الكهرباء", "Electricity",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["5201"].Id, level: 4,
            description: "فواتير استهلاك الكهرباء للمنشأة.",
            colorCode: expenseColor));

        level4.Add(Account.Create("52010004", "المياه", "Water",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["5201"].Id, level: 4,
            description: "فواتير استهلاك المياه للمنشأة.",
            colorCode: expenseColor));

        level4.Add(Account.Create("52010005", "الإيجارات", "Rent",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["5201"].Id, level: 4,
            description: "إيجار مباني ومكاتب ومستودعات المنشأة.",
            colorCode: expenseColor));

        level4.Add(Account.Create("52010006", "النقل", "Transport",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["5201"].Id, level: 4,
            description: "مصروفات النقل والمواصلات المتعلقة بنشاط المنشأة.",
            colorCode: expenseColor));

        level4.Add(Account.Create("52010007", "الخصم المسموح به", "Discount Allowed",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["5201"].Id, level: 4,
            description: "الخصومات التي تمنحها المنشأة للعملاء عند السداد المبكر.",
            colorCode: expenseColor));

        // Under "5202" Other Expenses
        level4.Add(Account.Create("52020001", "هالك المخزون", "Spoilage Loss",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["5202"].Id, level: 4,
            description: "الخسائر الناتجة عن تلف أو انتهاء صلاحية المخزون.",
            colorCode: expenseColor));

        level4.Add(Account.Create("52020002", "عجز مخزون", "Inventory Shortage",
            nature: (byte)AccountType.Expense, isLeaf: true, parentId: l3["5202"].Id, level: 4,
            description: "الخسائر الناتجة عن نقص المخزون بين المسجل والفعلي.",
            colorCode: expenseColor));

        level4.Add(Account.Create("52020003", "زيادة مخزون", "Inventory Surplus",
            nature: (byte)AccountType.Revenue, isLeaf: true, parentId: l3["5202"].Id, level: 4,
            description: "الإيرادات الناتجة عن زيادة المخزون بين المسجل والفعلي.",
            colorCode: revenueColor));

        db.Set<Account>().AddRange(level4);
        await db.SaveChangesAsync();
        var l4 = await db.Set<Account>().ToDictionaryAsync(a => a.AccountCode);
        logger?.LogInformation("Level 4 seeded: {Count} detail accounts.", level4.Count);

        // ─── Account Count Summary ─────────────────────────────────
        var totalCount = 5 + 8 + 24 + 44; // 81
        logger?.LogInformation(
            "Chart of accounts seeded successfully: {Total} accounts (5 L1 + 8 L2 + 24 L3 + 44 L4).", totalCount);

        // ─── Query ALL accounts by code for System Account Mappings ──
        var allAccounts = await db.Set<Account>().ToDictionaryAsync(a => a.AccountCode);

        // ─── Seed System Account Mappings (21 key-value pairs) ─────
        var mappings = new List<SystemAccountMapping>
        {
            SystemAccountMapping.Create(
                SystemAccountKey.DefaultCash.ToString(),
                allAccounts["11010001"].Id),  // الصندوق

            SystemAccountMapping.Create(
                SystemAccountKey.DefaultBank.ToString(),
                allAccounts["11020001"].Id),  // البنك الأهلي

            SystemAccountMapping.Create(
                SystemAccountKey.AccountsReceivable.ToString(),
                allAccounts["11030001"].Id),  // العميل النقدي

            SystemAccountMapping.Create(
                SystemAccountKey.AccountsPayable.ToString(),
                allAccounts["21010001"].Id),  // المورد النقدي

            SystemAccountMapping.Create(
                SystemAccountKey.Inventory.ToString(),
                allAccounts["11040001"].Id),  // بضاعة أول المدة

            SystemAccountMapping.Create(
                SystemAccountKey.CostOfGoodsSold.ToString(),
                allAccounts["51010001"].Id),  // تكلفة البضاعة المباعة

            SystemAccountMapping.Create(
                SystemAccountKey.SalesRevenue.ToString(),
                allAccounts["41010001"].Id),  // إيرادات المبيعات

            SystemAccountMapping.Create(
                SystemAccountKey.SalesReturns.ToString(),
                allAccounts["51020001"].Id),  // مردودات مبيعات

            SystemAccountMapping.Create(
                SystemAccountKey.PurchaseReturns.ToString(),
                allAccounts["51020002"].Id),  // مردودات مشتريات

            SystemAccountMapping.Create(
                SystemAccountKey.VatOutput.ToString(),
                allAccounts["21020001"].Id),  // ضريبة المبيعات (خرج)

            SystemAccountMapping.Create(
                SystemAccountKey.VatInput.ToString(),
                allAccounts["21020002"].Id),  // ضريبة المشتريات (دخل)

            SystemAccountMapping.Create(
                SystemAccountKey.Capital.ToString(),
                allAccounts["31010001"].Id),  // رأس المال

            SystemAccountMapping.Create(
                SystemAccountKey.OpeningBalanceEquity.ToString(),
                allAccounts["32020001"].Id),  // رصيد افتتاحي

            SystemAccountMapping.Create(
                SystemAccountKey.RetainedEarnings.ToString(),
                allAccounts["32010001"].Id),  // أرباح مدورة

            SystemAccountMapping.Create(
                SystemAccountKey.UndistributedProfits.ToString(),
                allAccounts["32030001"].Id),  // أرباح غير موزعة

            SystemAccountMapping.Create(
                SystemAccountKey.InventoryShortage.ToString(),
                allAccounts["52020002"].Id),  // عجز مخزون

            SystemAccountMapping.Create(
                SystemAccountKey.InventorySurplus.ToString(),
                allAccounts["52020003"].Id),  // زيادة مخزون

            SystemAccountMapping.Create(
                SystemAccountKey.GeneralExpense.ToString(),
                allAccounts["52010001"].Id),  // مصروفات عمومية

            SystemAccountMapping.Create(
                SystemAccountKey.SpoilageLoss.ToString(),
                allAccounts["52020001"].Id),  // هالك المخزون

            SystemAccountMapping.Create(
                SystemAccountKey.EmployeeCustody.ToString(),
                allAccounts["11070001"].Id),  // عهدة الموظفين

            SystemAccountMapping.Create(
                SystemAccountKey.DeliveryChargesRevenue.ToString(),
                allAccounts["41020003"].Id),  // إيرادات التوصيل
        };

        db.Set<SystemAccountMapping>().AddRange(mappings);
        await db.SaveChangesAsync();
        logger?.LogInformation("System account mappings seeded: {Count} keys.", mappings.Count);
    }
}
