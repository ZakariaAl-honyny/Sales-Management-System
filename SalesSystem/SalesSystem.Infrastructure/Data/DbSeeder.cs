using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Infrastructure.Data.Seeders;
using System.Linq;

namespace SalesSystem.Infrastructure.Data;

/// <summary>
/// Seeds initial reference data required for the system to function:
/// admin user, default warehouse, default customer, default supplier,
/// default cash box, base units, and document sequences.
/// Idempotent — skips if any users already exist.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Seeds initial data into the database. Safe to call multiple times —
    /// checks for existing users first and skips if already seeded.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="logger">Optional logger for recording seed progress.</param>
    public static async Task SeedAsync(SalesDbContext db, ILogger? logger = null)
    {
        // ═══════════════════════════════════════════════════
        // 1. Default customer — seeded BEFORE SystemSettings so DefaultCashCustomerId = "1"
        // ═══════════════════════════════════════════════════
        if (!await db.Customers.AnyAsync())
        {
            db.Customers.Add(Customer.Create(
                name: "العميل الافتراضي في النظام",
                openingBalance: 0m,
                createdByUserId: null
            ));
            logger?.LogInformation("Seeded default customer.");
        }

        // ═══════════════════════════════════════════════════
        // 2. Default supplier — seeded BEFORE SystemSettings so DefaultCashSupplierId = "1"
        // ═══════════════════════════════════════════════════
        if (!await db.Suppliers.AnyAsync())
        {
            db.Suppliers.Add(Supplier.Create(
                name: "المورد الافتراضي في النظام",
                openingBalance: 0m,
                createdByUserId: null
            ));
            logger?.LogInformation("Seeded default supplier.");
        }

        // ═══════════════════════════════════════════════════
        // 3. Seed SystemSettings (key-value pairs) — references customer/supplier Id=1
        // ═══════════════════════════════════════════════════
        if (!await db.SystemSettings.AnyAsync())
        {
            var settings = new List<SystemSetting>
            {
                // ── Inventory ──
                SystemSetting.Create("CostingMethod", "1", "int", "Inventory", "طريقة تقييم المخزون", "1=WeightedAverage, 2=LastPurchasePrice, 3=SupplierPrice"),
                SystemSetting.Create("AllowNegativeStock", "false", "bool", "Inventory", "السماح بالمخزون السالب", "السماح بجعل كمية المخزون أقل من صفر"),
                SystemSetting.Create("EnableFefo", "false", "bool", "Inventory", "استخدام FEFO", "استخدام طريقة الصادر أولاً حسب تاريخ الانتهاء"),
                SystemSetting.Create("StockAlertDays", "5", "int", "Inventory", "تحذير المخزون (أيام)", "عدد الأيام للتحذير قبل نفاد المخزون"),
                // ── Sales ──
                SystemSetting.Create("AutoPostInvoices", "true", "bool", "Sales", "الترحيل التلقائي", "ترحيل فاتورة البيع مباشرة عند الحفظ"),
                SystemSetting.Create("AllowDrafts", "true", "bool", "Sales", "السماح بالمسودات", "السماح بحفظ فاتورة البيع كمسودة"),
                SystemSetting.Create("ShowProfitInInvoice", "true", "bool", "Sales", "إظهار الربح", "إظهار هامش الربح في شاشة البيع"),
                SystemSetting.Create("PreventBelowRetailPrice", "false", "bool", "Sales", "منع البيع أقل من السعر", "منع البيع بسعر أقل من السعر الرسمي"),
                SystemSetting.Create("AllowBelowCostSale", "false", "bool", "Sales", "البيع أقل من التكلفة", "السماح بالبيع بسعر أقل من التكلفة مع تحذير"),
                SystemSetting.Create("DefaultCashCustomerId", "1", "int", "Sales", "العميل النقدي", "العميل الافتراضي لمبيعات النقد"),
                // ── Purchases ──
                SystemSetting.Create("PurchaseAutoPost", "true", "bool", "Purchases", "ترحيل المشتريات تلقائياً", "ترحيل فاتورة الشراء مباشرة عند الحفظ"),
                SystemSetting.Create("DefaultCashSupplierId", "1", "int", "Purchases", "المورد النقدي", "المورد الافتراضي لمشتريات النقد"),
                // ── Barcode ──
                SystemSetting.Create("EnableBarcode", "true", "bool", "Barcode", "تفعيل الباركود", "تفعيل الباركود في النظام بالكامل"),
                SystemSetting.Create("BarcodeInputType", "Scanner", "string", "Barcode", "نوع إدخال الباركود", "Scanner أو Camera"),
                SystemSetting.Create("AutoGenerateBarcode", "true", "bool", "Barcode", "توليد باركود تلقائي", "توليد باركود تلقائي للمنتجات الجديدة"),
                // ── Accounting ──
                SystemSetting.Create("AutoCreateJournalEntry", "true", "bool", "Accounting", "إنشاء قيود محاسبية", "إنشاء قيد محاسبي عند ترحيل كل فاتورة"),
                // ── General ──
                SystemSetting.Create("DecimalPlaces", "2", "int", "General", "الكسور العشرية", "عدد الخانات العشرية للأسعار والمبالغ"),
                SystemSetting.Create("Language", "ar", "string", "General", "لغة النظام", "اللغة الافتراضية للنظام"),
                SystemSetting.Create("DateFormat", "dd/MM/yyyy", "string", "General", "تنسيق التاريخ", "تنسيق عرض التواريخ في النظام"),
                // ── Print (new keys) ──
                SystemSetting.Create("PaperSize", "A4", "string", "Print", "حجم الورق", "حجم الورق الافتراضي للطباعة"),
                SystemSetting.Create("PrintCopies", "1", "int", "Print", "عدد النسخ", "عدد نسخ الطباعة الافتراضية"),
                SystemSetting.Create("ShowBalanceOnPrint", "true", "bool", "Print", "إظهار الرصيد", "إظهار رصيد الحساب في الفاتورة المطبوعة"),
                SystemSetting.Create("PrintSignature", "false", "bool", "Print", "طباعة التوقيع", "طباعة التوقيع في أسفل الفاتورة"),
            };
            db.SystemSettings.AddRange(settings);
            logger?.LogInformation("Seeded {Count} SystemSettings key-value pairs.", settings.Count);
        }

        // ═══════════════════════════════════════════════════
        // 4. Seed Taxes - independent guard
        // ═══════════════════════════════════════════════════
        if (!await db.Set<Tax>().AnyAsync())
        {
            var taxes = new List<Tax>
            {
                Tax.Create("بدون ضريبة", 0m, isDefault: true),
                Tax.Create("ضريبة القيمة المضافة 5%", 5m),
                Tax.Create("ضريبة القيمة المضافة 15%", 15m),
            };
            db.Set<Tax>().AddRange(taxes);
            logger?.LogInformation("Seeded {Count} tax records.", taxes.Count);
        }

        // ═══════════════════════════════════════════════════
        // 5. Seed StoreSettings - independent guard
        // ═══════════════════════════════════════════════════
        if (!await db.StoreSettings.AnyAsync())
        {
            var storeSettings = StoreSettings.Create("متجري", currencyCode: "SAR", defaultTaxRate: 15m, isTaxEnabled: false, enableStockAlerts: true, allowNegativeStock: false, autoUpdatePrices: true, invoicePrefix: "INV");
            db.StoreSettings.Add(storeSettings);
            logger?.LogInformation("Seeded StoreSettings.");
        }

        // Continue with existing seed logic below — system accounts, users, etc.
        // Skip remaining seed if admin user already exists
        if (await db.Users.AnyAsync())
        {
            logger?.LogInformation("Database already seeded. Skipping...");
            return;
        }

        // 6. Admin user — password: Admin@123, BCrypt work factor 12
        var adminUser = User.Create(
            userName: "admin",
            passwordHash: BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12),
            fullName: "Administrator",
            role: UserRole.Admin,
            createdByUserId: null
        );
        db.Users.Add(adminUser);

        // 7. Default warehouse
        var warehouse = Warehouse.Create(
            name: "المخزن الرئيسي",
            location: null,
            isDefault: true,
            createdByUserId: null
        );
        db.Warehouses.Add(warehouse);

        // 8. Default cash box — main cash drawer for daily operations
        var defaultCashBox = CashBox.Create(
            boxName: "الصندوق الرئيسي",
            initialBalance: 0m
        );
        db.CashBoxes.Add(defaultCashBox);

        // 9. Base units of measure
        db.Units.Add(Unit.Create("قطعة", "pcs", null));
        db.Units.Add(Unit.Create("كيلو", "kg", null));
        db.Units.Add(Unit.Create("لتر", "ltr", null));
        db.Units.Add(Unit.Create("متر", "m", null));
        db.Units.Add(Unit.Create("صندوق", "box", null));

        // 10. Document sequences for all document types (year 2026)
        db.DocumentSequences.Add(DocumentSequence.Create("INV", "INV", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("PUR", "PUR", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("SR", "SR", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("PR", "PR", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("TRF", "TRF", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("CP", "CP", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("SP", "SP", 2026));

        // 11. Product base units for existing products without any ProductUnit
        var productsWithoutUnits = await db.Products
            .Where(p => !db.ProductUnits.Any(pu => pu.ProductId == p.Id))
            .ToListAsync();

        foreach (var product in productsWithoutUnits)
        {
            db.ProductUnits.Add(ProductUnit.CreateBaseUnit(
                product.Id,
                "قطعة",
                product.RetailPrice,
                product.PurchasePrice
            ));
        }

        if (productsWithoutUnits.Any())
            logger?.LogInformation("Seeded base ProductUnits for {Count} products", productsWithoutUnits.Count);

        // 12. Seed accounting foundation (chart of accounts + system mappings)
        await AccountingSeeder.SeedAsync(db, logger);

        await db.SaveChangesAsync();
        logger?.LogInformation("Seed data completed successfully.");
    }
}
