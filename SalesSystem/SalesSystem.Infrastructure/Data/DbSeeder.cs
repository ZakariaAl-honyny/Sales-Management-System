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
        // 1. Seed Currencies — seeded FIRST as other entities may reference them
        // ═══════════════════════════════════════════════════
        if (!await db.Set<Currency>().AnyAsync())
        {
            db.Set<Currency>().AddRange(
                Currency.Create("ريال يمني", "YER", "﷼", 1.0m, isBaseCurrency: true, fractionName: "فلس"),
                Currency.Create("دولار أمريكي", "USD", "$", 550m, fractionName: "سنت"),
                Currency.Create("ريال سعودي", "SAR", "﷼", 71.4m, fractionName: "هللة")
            );
            logger?.LogInformation("Seeded {Count} currencies.", 3);
        }

        // ═══════════════════════════════════════════════════
        // 2. Seed Default Categories — seeded BEFORE Products
        // ═══════════════════════════════════════════════════
        if (!await db.Set<Category>().AnyAsync())
        {
            var generalCategory = Category.Create("عام", "التصنيف الافتراضي لجميع المنتجات", createdByUserId: 1);
            db.Set<Category>().Add(generalCategory);
            logger?.LogInformation("Seeded default category 'عام'.");
        }

        // ═══════════════════════════════════════════════════
        // 3. Default CustomerGroup — seeded BEFORE Customers
        // ═══════════════════════════════════════════════════
        if (!await db.Set<CustomerGroup>().AnyAsync())
        {
            db.Set<CustomerGroup>().Add(CustomerGroup.Create(
                name: "عام",
                description: "مجموعة العملاء الافتراضية"
            ));
            logger?.LogInformation("Seeded default customer group.");
        }

        // ═══════════════════════════════════════════════════
        // 4. Default customer — seeded BEFORE SystemSettings so DefaultCashCustomerId = "1"
        // ═══════════════════════════════════════════════════
        if (!await db.Customers.AnyAsync())
        {
            db.Customers.Add(Customer.Create(
                name: "عميل نقدي",
                openingBalance: 0m,
                createdByUserId: null,
                customerGroupId: 1
            ));
            logger?.LogInformation("Seeded default customer.");
        }

        // ═══════════════════════════════════════════════════
        // 5. Default supplier — seeded BEFORE SystemSettings so DefaultCashSupplierId = "1"
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
        // 6. Seed SystemSettings (key-value pairs) — references customer/supplier Id=1
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
                // ── Sales (continued) ──
                SystemSetting.Create("HideTaxInSales", "false", "bool", "Sales", "إخفاء الضريبة في المبيعات", "إخفاء حقل الضريبة في شاشة فاتورة البيع"),
                SystemSetting.Create("ShowExpiryInInvoices", "false", "bool", "Sales", "إظهار تاريخ الانتهاء", "إظهار تاريخ انتهاء الصلاحية في الفاتورة"),
                // ── Purchases (continued) ──
                SystemSetting.Create("HideTaxInPurchases", "false", "bool", "Purchases", "إخفاء الضريبة في المشتريات", "إخفاء حقل الضريبة في شاشة فاتورة الشراء"),
                // ── Print (continued) ──
                SystemSetting.Create("ShowLogo", "true", "bool", "Print", "إظهار الشعار", "إظهار شعار المتجر في الفواتير المطبوعة"),
                SystemSetting.Create("FooterNote", "", "string", "Print", "ملاحظة في التذييل", "نص يظهر في تذييل جميع الفواتير المطبوعة"),
                // ── Notifications ──
                SystemSetting.Create("LowStockAlert", "true", "bool", "Notifications", "تنبيه المخزون المنخفض", "تفعيل التنبيه عند انخفاض المخزون عن الحد الأدنى"),
                SystemSetting.Create("ExpiryAlert", "true", "bool", "Notifications", "تنبيه تواريخ الانتهاء", "تفعيل التنبيه عند اقتراب تاريخ انتهاء المنتجات"),
                SystemSetting.Create("ExpiryAlertDays", "30", "int", "Notifications", "أيام تنبيه الانتهاء", "عدد الأيام قبل تاريخ الانتهاء لإرسال التنبيه"),
                SystemSetting.Create("CreditLimitAlert", "true", "bool", "Notifications", "تنبيه الحد الائتماني", "تفعيل التنبيه عند تجاوز الحد الائتماني للعميل"),
            };
            db.SystemSettings.AddRange(settings);
            logger?.LogInformation("Seeded {Count} SystemSettings key-value pairs.", settings.Count);
        }

        // ═══════════════════════════════════════════════════
        // 7. Seed Taxes - independent guard
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
        // 8. Seed StoreSettings - independent guard
        // ═══════════════════════════════════════════════════
        if (!await db.StoreSettings.AnyAsync())
        {
            var storeSettings = StoreSettings.Create("متجري", currencyCode: "SAR", defaultTaxRate: 0m, isTaxEnabled: false, enableStockAlerts: true, allowNegativeStock: false, autoUpdatePrices: true, invoicePrefix: "INV");
            db.StoreSettings.Add(storeSettings);
            logger?.LogInformation("Seeded StoreSettings.");
        }

        // ═══════════════════════════════════════════════════
        // 9. Seed Permissions + RolePermissions
        // ═══════════════════════════════════════════════════
        if (!await db.Set<Permission>().AnyAsync())
        {
            var permissions = new List<Permission>
            {
                // ── Sales (1–7) ──
                Permission.Create("Sales.View", "عرض فواتير البيع", "Sales", isSystem: true),
                Permission.Create("Sales.Create", "إنشاء فاتورة بيع", "Sales", isSystem: true),
                Permission.Create("Sales.Edit", "تعديل فاتورة بيع", "Sales", isSystem: true),
                Permission.Create("Sales.Delete", "حذف فاتورة بيع", "Sales", isSystem: true),
                Permission.Create("Sales.Cancel", "إلغاء فاتورة بيع", "Sales", isSystem: true),
                Permission.Create("Sales.Return", "مرتجع مبيعات", "Sales", isSystem: true),
                Permission.Create("Sales.Print", "طباعة فاتورة بيع", "Sales", isSystem: true),
                // ── Purchases (8–12) ──
                Permission.Create("Purchases.View", "عرض فواتير الشراء", "Purchases", isSystem: true),
                Permission.Create("Purchases.Create", "إنشاء فاتورة شراء", "Purchases", isSystem: true),
                Permission.Create("Purchases.Edit", "تعديل فاتورة شراء", "Purchases", isSystem: true),
                Permission.Create("Purchases.Cancel", "إلغاء فاتورة شراء", "Purchases", isSystem: true),
                Permission.Create("Purchases.Print", "طباعة فاتورة شراء", "Purchases", isSystem: true),
                // ── Inventory (13–15) ──
                Permission.Create("Inventory.View", "عرض المخزون", "Inventory", isSystem: true),
                Permission.Create("Inventory.Transfer", "تحويل مخزون", "Inventory", isSystem: true),
                Permission.Create("Inventory.Adjust", "تعديل المخزون", "Inventory", isSystem: true),
                // ── Customers (16–18) ──
                Permission.Create("Customers.View", "عرض العملاء", "Customers", isSystem: true),
                Permission.Create("Customers.Create", "إضافة عميل", "Customers", isSystem: true),
                Permission.Create("Customers.Edit", "تعديل عميل", "Customers", isSystem: true),
                // ── Suppliers (19–21) ──
                Permission.Create("Suppliers.View", "عرض الموردين", "Suppliers", isSystem: true),
                Permission.Create("Suppliers.Create", "إضافة مورد", "Suppliers", isSystem: true),
                Permission.Create("Suppliers.Edit", "تعديل مورد", "Suppliers", isSystem: true),
                // ── Products (22–24) ──
                Permission.Create("Products.View", "عرض المنتجات", "Products", isSystem: true),
                Permission.Create("Products.Create", "إضافة منتج", "Products", isSystem: true),
                Permission.Create("Products.Edit", "تعديل منتج", "Products", isSystem: true),
                // ── Reports (25) ──
                Permission.Create("Reports.View", "عرض التقارير", "Reports", isSystem: true),
                // ── Accounting (26–27) ──
                Permission.Create("Accounting.View", "عرض الحسابات", "Accounting", isSystem: true),
                Permission.Create("Accounting.Manage", "إدارة الحسابات", "Accounting", isSystem: true),
                // ── System (28–29) ──
                Permission.Create("System.Settings", "إعدادات النظام", "System", isSystem: true),
                Permission.Create("System.Users", "إدارة المستخدمين", "System", isSystem: true),
                // ── Operations (30–32) ──
                Permission.Create("Operations.Cashbox", "إدارة الصندوق", "Operations", isSystem: true),
                Permission.Create("Operations.Banking", "إدارة البنوك", "Operations", isSystem: true),
                Permission.Create("Operations.Expenses", "إدارة المصروفات", "Operations", isSystem: true),
                // ── Audit (33) ──
                Permission.Create("Audit.Log", "سجل المراجعة", "Audit", isSystem: true),
            };
            db.Set<Permission>().AddRange(permissions);
            await db.SaveChangesAsync();
            logger?.LogInformation("Seeded {Count} permissions.", permissions.Count);

            // ── RolePermissions ──
            // Admin (1) — ALL permissions
            var allPermissionIds = permissions.Select(p => p.Id).ToList();
            foreach (var permId in allPermissionIds)
            {
                db.Set<RolePermission>().Add(RolePermission.Create(UserRole.Admin, permId));
            }

            // Manager (2) — Sales, Purchases, Inventory, Customers, Suppliers, Products, Reports, Accounting.View, Audit.Log
            var managerPermIds = permissions
                .Where(p => p.Category is "Sales" or "Purchases" or "Inventory" or "Customers" or "Suppliers" or "Products"
                    || p.Name is "Reports.View" or "Accounting.View" or "Audit.Log")
                .Select(p => p.Id)
                .ToList();
            foreach (var permId in managerPermIds)
            {
                db.Set<RolePermission>().Add(RolePermission.Create(UserRole.Manager, permId));
            }

            // Cashier (3) — Sales (View, Create, Print), Customers.View, Inventory.View, Operations.Cashbox
            var cashierPermNames = new HashSet<string> { "Sales.View", "Sales.Create", "Sales.Print",
                "Customers.View", "Inventory.View", "Operations.Cashbox" };
            var cashierPermIds = permissions
                .Where(p => cashierPermNames.Contains(p.Name))
                .Select(p => p.Id)
                .ToList();
            foreach (var permId in cashierPermIds)
            {
                db.Set<RolePermission>().Add(RolePermission.Create(UserRole.Cashier, permId));
            }

            await db.SaveChangesAsync();
            logger?.LogInformation("Seeded RolePermissions: Admin={0}, Manager={1}, Cashier={2}",
                allPermissionIds.Count, managerPermIds.Count, cashierPermIds.Count);
        }

        // Continue with existing seed logic below — system accounts, users, etc.
        // Skip remaining seed if admin user already exists
        if (await db.Users.AnyAsync())
        {
            logger?.LogInformation("Database already seeded. Skipping...");
            return;
        }

        // 10. Admin user — created with default password "12345678" (MustChangePassword=true).
        // On first login, the admin must change their password.
        string adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678", workFactor: 12);
        var adminUser = User.CreateWithPassword(
            userName: "admin",
            passwordHash: adminPasswordHash,
            fullName: "Administrator",
            role: UserRole.Admin,
            mustChangePassword: true,
            createdByUserId: null
        );
        db.Users.Add(adminUser);

        // 11. Default warehouse
        var warehouse = Warehouse.Create(
            name: "المخزن الرئيسي",
            location: null,
            isDefault: true,
            createdByUserId: null
        );
        db.Warehouses.Add(warehouse);

        // 12. Base units of measure — save first so IDs are available for ProductUnit creation
        var unitPiece = Unit.Create("حبة", "pcs", null);
        var unitKg = Unit.Create("كيلو", "kg", null);
        var unitLiter = Unit.Create("لتر", "ltr", null);
        var unitMeter = Unit.Create("متر", "m", null);
        var unitCarton = Unit.Create("كرتون", "ctn", null);
        var unitBox = Unit.Create("علبة", "box", null);
        var unitGram = Unit.Create("جرام", "g", null);
        db.Units.AddRange(unitPiece, unitKg, unitLiter, unitMeter, unitCarton, unitBox, unitGram);
        await db.SaveChangesAsync();
        logger?.LogInformation("Seeded {Count} base units of measure.", 7);

        // 13. Document sequences for all document types (year 2026)
        db.DocumentSequences.Add(DocumentSequence.Create("INV", "INV", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("PUR", "PUR", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("SR", "SR", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("PR", "PR", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("TRF", "TRF", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("CP", "CP", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("SP", "SP", 2026));

        // 14. Product base units for existing products without any ProductUnit
        var productsWithoutUnits = await db.Products
            .Where(p => !db.ProductUnits.Any(pu => pu.ProductId == p.Id))
            .ToListAsync();

        foreach (var product in productsWithoutUnits)
        {
            db.ProductUnits.Add(ProductUnit.CreateBaseUnit(
                product.Id,
                unitPiece.Id  // حبة as default base unit
            ));
        }

        if (productsWithoutUnits.Any())
            logger?.LogInformation("Seeded base ProductUnits for {Count} products", productsWithoutUnits.Count);

        // 15. Seed accounting foundation (chart of accounts + system mappings)
        await AccountingSeeder.SeedAsync(db, logger);

        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════════════
        // 16. Seed ProductPrices for each product's base unit
        //     (requires ProductUnits to be saved first for IDs)
        // ═══════════════════════════════════════════════════
        if (!await db.Set<ProductPrice>().AnyAsync())
        {
            var baseProductUnits = await db.ProductUnits
                .Where(pu => pu.IsBaseUnit)
                .ToListAsync();

            if (baseProductUnits.Any())
            {
                var productPrices = new List<ProductPrice>();
                foreach (var pu in baseProductUnits)
                {
                    // PriceLevel removed — price is now strictly (ProductUnitId + CurrencyId) → Price
                    productPrices.Add(ProductPrice.Create(
                        productUnitId: pu.Id,
                        currencyId: 1,
                        price: 100m,
                        effectiveFrom: DateTime.UtcNow,
                        createdByUserId: 1
                    ));
                }

                db.Set<ProductPrice>().AddRange(productPrices);
                await db.SaveChangesAsync();
                logger?.LogInformation("Seeded {Count} ProductPrices for {ProductUnitCount} base units (PriceLevel removed).",
                    productPrices.Count, baseProductUnits.Count);
            }
        }

        logger?.LogInformation("Seed data completed successfully.");
    }
}
