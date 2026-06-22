using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Infrastructure.Data.Seeders;
using System.Linq;

namespace SalesSystem.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(SalesDbContext db, ILogger? logger = null)
    {
        if (!await db.Set<Currency>().AnyAsync())
        {
            db.Set<Currency>().AddRange(
                Currency.Create("ريال يمني", "YER", "﷼", isBaseCurrency: true, fractionName: "فلس"),
                Currency.Create("دولار أمريكي", "USD", "$", fractionName: "سنت"),
                Currency.Create("ريال سعودي", "SAR", "﷼", fractionName: "هللة")
            );
            logger?.LogInformation("Seeded {Count} currencies.", 3);
        }

        if (!await db.Set<ProductCategory>().AnyAsync())
        {
            var generalCategory = ProductCategory.Create("عام", description: "التصنيف الافتراضي لجميع المنتجات", createdByUserId: 1);
            db.Set<ProductCategory>().Add(generalCategory);
            logger?.LogInformation("Seeded default product category 'عام'.");
        }

        await AccountingSeeder.SeedAsync(db, logger);

        if (!await db.Customers.AnyAsync())
        {
            var cashCustomerAccount = await db.Set<Account>()
                .FirstAsync(a => a.AccountCode == "11030001");
            var cashCustomerParty = Party.Create(
                name: "عميل نقدي",
                createdByUserId: null
            );
            db.Set<Party>().Add(cashCustomerParty);
            await db.SaveChangesAsync();
            db.Customers.Add(Customer.Create(
                partyId: cashCustomerParty.Id,
                accountId: cashCustomerAccount.Id,
                createdByUserId: null
            ));
            await db.SaveChangesAsync();
            logger?.LogInformation("Seeded default customer.");
        }

        if (!await db.Suppliers.AnyAsync())
        {
            var cashSupplierAccount = await db.Set<Account>()
                .FirstAsync(a => a.AccountCode == "21010001");
            var cashSupplierParty = Party.Create(
                name: "المورد الافتراضي في النظام",
                createdByUserId: null
            );
            db.Set<Party>().Add(cashSupplierParty);
            await db.SaveChangesAsync();
            db.Suppliers.Add(Supplier.Create(
                partyId: cashSupplierParty.Id,
                accountId: cashSupplierAccount.Id,
                createdByUserId: null
            ));
            await db.SaveChangesAsync();
            logger?.LogInformation("Seeded default supplier.");
        }

        if (!await db.SystemSettings.AnyAsync())
        {
            var settings = new List<SystemSetting>
            {
                SystemSetting.Create("CostingMethod", "1", settingType: 2, category: "Inventory", displayName: "طريقة تقييم المخزون", description: "1=WeightedAverage, 2=LastPurchasePrice, 3=SupplierPrice"),
                SystemSetting.Create("AllowNegativeStock", "false", settingType: 4, category: "Inventory", displayName: "السماح بالمخزون السالب", description: "السماح بجعل كمية المخزون أقل من صفر"),
                SystemSetting.Create("EnableFefo", "false", settingType: 4, category: "Inventory", displayName: "استخدام FEFO", description: "استخدام طريقة الصادر أولاً حسب تاريخ الانتهاء"),
                SystemSetting.Create("StockAlertDays", "5", settingType: 2, category: "Inventory", displayName: "تحذير المخزون (أيام)", description: "عدد الأيام للتحذير قبل نفاد المخزون"),
                SystemSetting.Create("AutoPostInvoices", "true", settingType: 4, category: "Sales", displayName: "الترحيل التلقائي", description: "ترحيل فاتورة البيع مباشرة عند الحفظ"),
                SystemSetting.Create("AllowDrafts", "true", settingType: 4, category: "Sales", displayName: "السماح بالمسودات", description: "السماح بحفظ فاتورة البيع كمسودة"),
                SystemSetting.Create("ShowProfitInInvoice", "true", settingType: 4, category: "Sales", displayName: "إظهار الربح", description: "إظهار هامش الربح في شاشة البيع"),
                SystemSetting.Create("PreventBelowRetailPrice", "false", settingType: 4, category: "Sales", displayName: "منع البيع أقل من السعر", description: "منع البيع بسعر أقل من السعر الرسمي"),
                SystemSetting.Create("AllowBelowCostSale", "true", settingType: 4, category: "Sales", displayName: "البيع أقل من التكلفة", description: "السماح بالبيع بسعر أقل من التكلفة مع تحذير"),
                SystemSetting.Create("DefaultCashCustomerId", "1", settingType: 2, category: "Sales", displayName: "العميل النقدي", description: "العميل الافتراضي لمبيعات النقد"),
                SystemSetting.Create("PurchaseAutoPost", "true", settingType: 4, category: "Purchases", displayName: "ترحيل المشتريات تلقائياً", description: "ترحيل فاتورة الشراء مباشرة عند الحفظ"),
                SystemSetting.Create("DefaultCashSupplierId", "1", settingType: 2, category: "Purchases", displayName: "المورد النقدي", description: "المورد الافتراضي لمشتريات النقد"),
                SystemSetting.Create("EnableBarcode", "true", settingType: 4, category: "Barcode", displayName: "تفعيل الباركود", description: "تفعيل الباركود في النظام بالكامل"),
                SystemSetting.Create("BarcodeInputType", "Scanner", settingType: 1, category: "Barcode", displayName: "نوع إدخال الباركود", description: "Scanner أو Camera"),
                SystemSetting.Create("AutoGenerateBarcode", "true", settingType: 4, category: "Barcode", displayName: "توليد باركود تلقائي", description: "توليد باركود تلقائي للمنتجات الجديدة"),
                SystemSetting.Create("AutoCreateJournalEntry", "true", settingType: 4, category: "Accounting", displayName: "إنشاء قيود محاسبية", description: "إنشاء قيد محاسبي عند ترحيل كل فاتورة"),
                SystemSetting.Create("DecimalPlaces", "2", settingType: 2, category: "General", displayName: "الكسور العشرية", description: "عدد الخانات العشرية للأسعار والمبالغ"),
                SystemSetting.Create("Language", "ar", settingType: 1, category: "General", displayName: "لغة النظام", description: "اللغة الافتراضية للنظام"),
                SystemSetting.Create("DateFormat", "dd/MM/yyyy", settingType: 1, category: "General", displayName: "تنسيق التاريخ", description: "تنسيق عرض التواريخ في النظام"),
                SystemSetting.Create("PaperSize", "A4", settingType: 1, category: "Print", displayName: "حجم الورق", description: "حجم الورق الافتراضي للطباعة"),
                SystemSetting.Create("PrintCopies", "1", settingType: 2, category: "Print", displayName: "عدد النسخ", description: "عدد نسخ الطباعة الافتراضية"),
                SystemSetting.Create("ShowBalanceOnPrint", "true", settingType: 4, category: "Print", displayName: "إظهار الرصيد", description: "إظهار رصيد الحساب في الفاتورة المطبوعة"),
                SystemSetting.Create("PrintSignature", "false", settingType: 4, category: "Print", displayName: "طباعة التوقيع", description: "طباعة التوقيع في أسفل الفاتورة"),
                SystemSetting.Create("HideTaxInSales", "false", settingType: 4, category: "Sales", displayName: "إخفاء الضريبة في المبيعات", description: "إخفاء حقل الضريبة في شاشة فاتورة البيع"),
                SystemSetting.Create("ShowExpiryInInvoices", "false", settingType: 4, category: "Sales", displayName: "إظهار تاريخ الانتهاء", description: "إظهار تاريخ انتهاء الصلاحية في الفاتورة"),
                SystemSetting.Create("HideTaxInPurchases", "false", settingType: 4, category: "Purchases", displayName: "إخفاء الضريبة في المشتريات", description: "إخفاء حقل الضريبة في شاشة فاتورة الشراء"),
                SystemSetting.Create("ShowLogo", "true", settingType: 4, category: "Print", displayName: "إظهار الشعار", description: "إظهار شعار المتجر في الفواتير المطبوعة"),
                SystemSetting.Create("FooterNote", "", settingType: 1, category: "Print", displayName: "ملاحظة في التذييل", description: "نص يظهر في تذييل جميع الفواتير المطبوعة"),
                SystemSetting.Create("LowStockAlert", "true", settingType: 4, category: "Notifications", displayName: "تنبيه المخزون المنخفض", description: "تفعيل التنبيه عند انخفاض المخزون عن الحد الأدنى"),
                SystemSetting.Create("ExpiryAlert", "true", settingType: 4, category: "Notifications", displayName: "تنبيه تواريخ الانتهاء", description: "تفعيل التنبيه عند اقتراب تاريخ انتهاء المنتجات"),
                SystemSetting.Create("ExpiryAlertDays", "30", settingType: 2, category: "Notifications", displayName: "أيام تنبيه الانتهاء", description: "عدد الأيام قبل تاريخ الانتهاء لإرسال التنبيه"),
                SystemSetting.Create("CreditLimitAlert", "true", settingType: 4, category: "Notifications", displayName: "تنبيه الحد الائتماني", description: "تفعيل التنبيه عند تجاوز الحد الائتماني للعميل"),
            };
            db.SystemSettings.AddRange(settings);
            logger?.LogInformation("Seeded {Count} SystemSettings key-value pairs.", settings.Count);
        }

        if (!await db.Set<Tax>().AnyAsync())
        {
            var taxes = new List<Tax>
            {
                Tax.Create("بدون ضريبة", "TAX-EXEMPT", 0m, taxType: 3, isDefault: true),
                Tax.Create("ضريبة القيمة المضافة 5%", "VAT-5", 5m),
                Tax.Create("ضريبة القيمة المضافة 15%", "VAT-15", 15m),
            };
            db.Set<Tax>().AddRange(taxes);
            logger?.LogInformation("Seeded {Count} tax records.", taxes.Count);
        }

        if (!await db.Set<SystemSetting>().AnyAsync(s => s.SettingKey == "Store.Name"))
        {
            var storeSettings = new List<SystemSetting>
            {
                SystemSetting.Create("Store.Name", "متجري", settingType: 1, category: "Store"),
                SystemSetting.Create("Store.Phone", "", settingType: 1, category: "Store"),
                SystemSetting.Create("Store.Address", "", settingType: 1, category: "Store"),
                SystemSetting.Create("Store.LogoPath", "", settingType: 1, category: "Store"),
                SystemSetting.Create("Store.Email", "", settingType: 1, category: "Store"),
                SystemSetting.Create("Store.CurrencyCode", "SAR", settingType: 1, category: "Store"),
                SystemSetting.Create("Store.TaxNumber", "", settingType: 1, category: "Store"),
                SystemSetting.Create("Store.EnableStockAlerts", "true", settingType: 4, category: "Store"),
                SystemSetting.Create("Store.AllowNegativeStock", "false", settingType: 4, category: "Store"),
                SystemSetting.Create("Store.AutoUpdatePrices", "true", settingType: 4, category: "Store"),
                SystemSetting.Create("Store.SignaturePath", "", settingType: 1, category: "Store"),
            };
            db.Set<SystemSetting>().AddRange(storeSettings);
            logger?.LogInformation("Seeded Store settings into SystemSettings.");
        }

        if (!await db.Set<Role>().AnyAsync())
        {
            var adminRole = Role.Create("مدير النظام", "Administrator - كامل الصلاحيات");
            var managerRole = Role.Create("مدير", "Manager - صلاحيات إدارية");
            var cashierRole = Role.Create("كاشير", "Cashier - صلاحيات مبيعات محدودة");
            var observerRole = Role.Create("مراقب", "Observer - صلاحيات مشاهدة فقط");
            var branchManagerRole = Role.Create("مدير فرع", "Branch Manager - صلاحيات محدودة بفرع محدد");
            db.Set<Role>().AddRange(adminRole, managerRole, cashierRole, observerRole, branchManagerRole);
            await db.SaveChangesAsync();
            logger?.LogInformation("Seeded 5 roles: Admin, Manager, Cashier, Observer, BranchManager.");
        }

        if (!await db.Set<Permission>().AnyAsync())
        {
            var permissions = new List<Permission>
            {
                Permission.Create("Sales.View", "عرض فواتير البيع", "Sales", isSystem: true),
                Permission.Create("Sales.Create", "إنشاء فاتورة بيع", "Sales", isSystem: true),
                Permission.Create("Sales.Edit", "تعديل فاتورة بيع", "Sales", isSystem: true),
                Permission.Create("Sales.Delete", "حذف فاتورة بيع", "Sales", isSystem: true),
                Permission.Create("Sales.Cancel", "إلغاء فاتورة بيع", "Sales", isSystem: true),
                Permission.Create("Sales.Return", "مرتجع مبيعات", "Sales", isSystem: true),
                Permission.Create("Sales.Print", "طباعة فاتورة بيع", "Sales", isSystem: true),
                Permission.Create("Purchases.View", "عرض فواتير الشراء", "Purchases", isSystem: true),
                Permission.Create("Purchases.Create", "إنشاء فاتورة شراء", "Purchases", isSystem: true),
                Permission.Create("Purchases.Edit", "تعديل فاتورة شراء", "Purchases", isSystem: true),
                Permission.Create("Purchases.Cancel", "إلغاء فاتورة شراء", "Purchases", isSystem: true),
                Permission.Create("Purchases.Print", "طباعة فاتورة شراء", "Purchases", isSystem: true),
                Permission.Create("Inventory.View", "عرض المخزون", "Inventory", isSystem: true),
                Permission.Create("Inventory.Transfer", "تحويل مخزون", "Inventory", isSystem: true),
                Permission.Create("Inventory.Adjust", "تعديل المخزون", "Inventory", isSystem: true),
                Permission.Create("Customers.View", "عرض العملاء", "Customers", isSystem: true),
                Permission.Create("Customers.Create", "إضافة عميل", "Customers", isSystem: true),
                Permission.Create("Customers.Edit", "تعديل عميل", "Customers", isSystem: true),
                Permission.Create("Suppliers.View", "عرض الموردين", "Suppliers", isSystem: true),
                Permission.Create("Suppliers.Create", "إضافة مورد", "Suppliers", isSystem: true),
                Permission.Create("Suppliers.Edit", "تعديل مورد", "Suppliers", isSystem: true),
                Permission.Create("Products.View", "عرض المنتجات", "Products", isSystem: true),
                Permission.Create("Products.Create", "إضافة منتج", "Products", isSystem: true),
                Permission.Create("Products.Edit", "تعديل منتج", "Products", isSystem: true),
                Permission.Create("Reports.View", "عرض التقارير", "Reports", isSystem: true),
                Permission.Create("Accounting.View", "عرض الحسابات", "Accounting", isSystem: true),
                Permission.Create("Accounting.Manage", "إدارة الحسابات", "Accounting", isSystem: true),
                Permission.Create("System.Settings", "إعدادات النظام", "System", isSystem: true),
                Permission.Create("System.Users", "إدارة المستخدمين", "System", isSystem: true),
                Permission.Create("Operations.Cashbox", "إدارة الصندوق", "Operations", isSystem: true),
                Permission.Create("Operations.Banking", "إدارة البنوك", "Operations", isSystem: true),
                Permission.Create("Operations.Expenses", "إدارة المصروفات", "Operations", isSystem: true),
                Permission.Create("Audit.Log", "سجل المراجعة", "Audit", isSystem: true),
            };
            db.Set<Permission>().AddRange(permissions);
            await db.SaveChangesAsync();
            logger?.LogInformation("Seeded {Count} permissions.", permissions.Count);

            // ─── RolePermission: Assign permissions to roles ───
            var adminRole = await db.Set<Role>().FirstAsync(r => r.Name == "مدير النظام");
            var managerRole = await db.Set<Role>().FirstAsync(r => r.Name == "مدير");
            var cashierRole = await db.Set<Role>().FirstAsync(r => r.Name == "كاشير");
            var observerRole = await db.Set<Role>().FirstAsync(r => r.Name == "مراقب");
            var branchManagerRole = await db.Set<Role>().FirstAsync(r => r.Name == "مدير فرع");

            var allPermissionIds = permissions.Select(p => p.Id).ToList();
            
            // Admin: All permissions
            foreach (var permId in allPermissionIds)
            {
                db.Set<RolePermission>().Add(RolePermission.Create(adminRole.Id, permId));
            }

            // Manager: Most permissions (exclude system settings and user management)
            var managerPermIds = permissions
                .Where(p => p.Category is "Sales" or "Purchases" or "Inventory" or "Customers" or "Suppliers" or "Products"
                    || p.Code is "Reports.View" or "Accounting.View" or "Accounting.Manage" or "Operations.Cashbox"
                    || p.Code is "Operations.Banking" or "Operations.Expenses" or "Audit.Log")
                .Select(p => p.Id)
                .ToList();
            foreach (var permId in managerPermIds)
            {
                db.Set<RolePermission>().Add(RolePermission.Create(managerRole.Id, permId));
            }

            // Cashier: Limited to sales, customer view, inventory view, cashbox
            var cashierPermCodes = new HashSet<string> { "Sales.View", "Sales.Create", "Sales.Print",
                "Customers.View", "Inventory.View", "Operations.Cashbox" };
            var cashierPermIds = permissions
                .Where(p => cashierPermCodes.Contains(p.Code))
                .Select(p => p.Id)
                .ToList();
            foreach (var permId in cashierPermIds)
            {
                db.Set<RolePermission>().Add(RolePermission.Create(cashierRole.Id, permId));
            }

            // Observer: View-only permissions
            var observerPermCodes = new HashSet<string> { "Sales.View", "Purchases.View", "Inventory.View",
                "Customers.View", "Suppliers.View", "Products.View", "Reports.View", "Accounting.View" };
            var observerPermIds = permissions
                .Where(p => observerPermCodes.Contains(p.Code))
                .Select(p => p.Id)
                .ToList();
            foreach (var permId in observerPermIds)
            {
                db.Set<RolePermission>().Add(RolePermission.Create(observerRole.Id, permId));
            }

            // Branch Manager: Sales, Customers, Products, Inventory, Purchases view, Reports, Cashbox
            var branchManagerPermIds = permissions
                .Where(p => p.Category is "Sales" or "Customers" or "Products" or "Inventory"
                    || p.Code is "Purchases.View" or "Reports.View" or "Operations.Cashbox")
                .Select(p => p.Id)
                .ToList();
            foreach (var permId in branchManagerPermIds)
            {
                db.Set<RolePermission>().Add(RolePermission.Create(branchManagerRole.Id, permId));
            }

            await db.SaveChangesAsync();
            logger?.LogInformation("Seeded RolePermissions: Admin={0}, Manager={1}, Cashier={2}, Observer={3}, BranchManager={4}",
                allPermissionIds.Count, managerPermIds.Count, cashierPermIds.Count, observerPermIds.Count, branchManagerPermIds.Count);
        }

        // ═══════════════════════════════════════════════════════════
        // ─── Missing Permissions (idempotent) ─────────────────────
        // ═══════════════════════════════════════════════════════════
        var existingCodes = await db.Set<Permission>().Select(p => p.Code).ToListAsync();
        var existingCodeSet = new HashSet<string>(existingCodes);

        var newPermissions = new List<Permission>();
        var permissionDefs = new (string code, string displayName, string category)[]
        {
            ("Purchases.Return",     "مرتجع مشتريات",       "Purchases"),
            ("Customers.Delete",     "حذف عميل",            "Customers"),
            ("Suppliers.Delete",     "حذف مورد",            "Suppliers"),
            ("Products.Delete",      "حذف منتج",            "Products"),
            ("Backup.Manage",        "إدارة النسخ الاحتياطي","System"),
            ("FiscalYear.Manage",    "إدارة السنة المالية",  "Accounting"),
            ("Employees.View",       "عرض الموظفين",        "Employees"),
            ("Employees.Manage",     "إدارة الموظفين",      "Employees"),
            ("Currencies.View",      "عرض العملات",         "Accounting"),
            ("Currencies.Manage",    "إدارة العملات",       "Accounting"),
            ("Warehouse.Manage",     "إدارة المخازن",       "Inventory"),
            ("Inventory.Count",      "جرد المخزون",         "Inventory"),
            ("Roles.Manage",         "إدارة الأدوار",       "System"),
        };

        foreach (var (code, displayName, category) in permissionDefs)
        {
            if (!existingCodeSet.Contains(code))
                newPermissions.Add(Permission.Create(code, displayName, category, isSystem: true));
        }

        if (newPermissions.Any())
        {
            db.Set<Permission>().AddRange(newPermissions);
            await db.SaveChangesAsync();
            logger?.LogInformation("Added {Count} missing permissions.", newPermissions.Count);
        }

        // ─── New Roles (idempotent) ──────────────────────────────────
        var newRoles = new List<Role>();
        if (!await db.Set<Role>().AnyAsync(r => r.Name == "محاسب"))
            newRoles.Add(Role.Create("محاسب", "Accountant - صلاحيات محاسبية"));
        if (!await db.Set<Role>().AnyAsync(r => r.Name == "أمين صندوق"))
            newRoles.Add(Role.Create("أمين صندوق", "Treasurer - صلاحيات الصندوق"));
        if (!await db.Set<Role>().AnyAsync(r => r.Name == "مشرف مخزن"))
            newRoles.Add(Role.Create("مشرف مخزن", "Warehouse Supervisor - صلاحيات المخازن"));
        if (!await db.Set<Role>().AnyAsync(r => r.Name == "موظف مبيعات"))
            newRoles.Add(Role.Create("موظف مبيعات", "Sales Employee - صلاحيات بيع محدودة"));

        if (newRoles.Any())
        {
            db.Set<Role>().AddRange(newRoles);
            await db.SaveChangesAsync();
            logger?.LogInformation("Added {Count} new roles: {Names}.", newRoles.Count,
                string.Join(", ", newRoles.Select(r => r.Name)));
        }

        // Reload all permissions now (includes newly added ones)
        var allPerms = await db.Set<Permission>().ToListAsync();

        // ─── New RolePermissions (idempotent) ────────────────────────
        // محاسب (Accountant): accounting, reports, view-only sales/purchases/customers/suppliers, audit
        await AssignRolePermissionsAsync(db, "محاسب", allPerms,
            p => p.Category is "Accounting" or "Reports" or "Audit"
                || p.Code is "Sales.View" or "Purchases.View" or "Inventory.View"
                || p.Code is "Customers.View" or "Suppliers.View" or "Products.View",
            logger);

        // أمين صندوق (Treasurer): cashbox operations, banking, sales view, customer view
        await AssignRolePermissionsAsync(db, "أمين صندوق", allPerms,
            p => p.Code is "Operations.Cashbox" or "Operations.Banking"
                || p.Code is "Sales.View" or "Customers.View",
            logger);

        // مشرف مخزن (Warehouse Supervisor): inventory full + products view + reports view
        await AssignRolePermissionsAsync(db, "مشرف مخزن", allPerms,
            p => p.Category == "Inventory"
                || p.Code is "Products.View" or "Reports.View" or "Sales.View",
            logger);

        // موظف مبيعات (Sales Employee): sales create/view/print/return + customer view + inventory view
        await AssignRolePermissionsAsync(db, "موظف مبيعات", allPerms,
            p => p.Category == "Sales"
                || p.Code is "Customers.View" or "Inventory.View" or "Operations.Cashbox",
            logger);

        // ─── Assign new permissions to Admin (idempotent) ──────────────
        var admin = await db.Set<Role>().FirstOrDefaultAsync(r => r.Name == "مدير النظام");
        if (admin != null)
        {
            var adminExistingPerms = await db.Set<RolePermission>()
                .Where(rp => rp.RoleId == admin.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();
            var adminExistingSet = new HashSet<int>(adminExistingPerms);
            var adminMissingPerms = allPerms.Where(p => !adminExistingSet.Contains(p.Id)).ToList();
            if (adminMissingPerms.Any())
            {
                foreach (var perm in adminMissingPerms)
                    db.Set<RolePermission>().Add(RolePermission.Create(admin.Id, perm.Id));
                await db.SaveChangesAsync();
                logger?.LogInformation("Added {Count} new permissions to Admin role.", adminMissingPerms.Count);
            }
        }

        // ─── Assign new permissions to Manager (idempotent) ───────────
        var manager = await db.Set<Role>().FirstOrDefaultAsync(r => r.Name == "مدير");
        if (manager != null)
        {
            var managerExistingPerms = await db.Set<RolePermission>()
                .Where(rp => rp.RoleId == manager.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();
            var managerExistingSet = new HashSet<int>(managerExistingPerms);
            // Manager gets all permissions except system-level settings and user management
            var managerMissingPerms = allPerms
                .Where(p => !managerExistingSet.Contains(p.Id)
                    && p.Code is not "System.Settings" and not "System.Users")
                .ToList();
            if (managerMissingPerms.Any())
            {
                foreach (var perm in managerMissingPerms)
                    db.Set<RolePermission>().Add(RolePermission.Create(manager.Id, perm.Id));
                await db.SaveChangesAsync();
                logger?.LogInformation("Added {Count} new permissions to Manager role.", managerMissingPerms.Count);
            }
        }

        // ─── Branches ──────────────────────────────────────────────
        if (!await db.Branches.AnyAsync())
        {
            var branch = Branch.Create("الفرع الرئيسي", "HQ");
            db.Branches.Add(branch);
            await db.SaveChangesAsync();
            logger?.LogInformation("Seeded default branch 'الفرع الرئيسي'.");
        }

        // ─── Departments ────────────────────────────────────────────
        if (!await db.Departments.AnyAsync())
        {
            var branch = await db.Branches.FirstAsync();
            db.Departments.Add(Department.Create("الإدارة العامة"));
            await db.SaveChangesAsync();
            logger?.LogInformation("Seeded default department 'الإدارة العامة'.");
        }

        // ─── ProductCategories ──────────────────────────────────────
        if (!await db.ProductCategories.AnyAsync())
        {
            db.ProductCategories.AddRange(
                ProductCategory.Create("مواد غذائية"),
                ProductCategory.Create("مشروبات"),
                ProductCategory.Create("منظفات")
            );
            await db.SaveChangesAsync();
            logger?.LogInformation("Seeded 3 product categories.");
        }

        if (await db.Users.AnyAsync())
        {
            logger?.LogInformation("Database already seeded. Skipping...");
            return;
        }

        string adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678", workFactor: 12);
        var adminUser = User.CreateWithPassword(
            userName: "admin",
            passwordHash: adminPasswordHash,
            employeeId: null,
            createdByUserId: null,
            mustChangePassword: true
        );
        db.Users.Add(adminUser);
        await db.SaveChangesAsync(); // Save to get admin user ID

        // Assign admin user to the "مدير النظام" role (CRITICAL — without this, admin has zero permissions)
        var adminRoleEntity = await db.Set<Role>().FirstAsync(r => r.Name == "مدير النظام");
        db.Set<UserRole>().Add(UserRole.Create(adminUser.Id, adminRoleEntity.Id));
        await db.SaveChangesAsync();
        logger?.LogInformation("Assigned admin user to role '{Role}'.", adminRoleEntity.Name);

        var warehouse = Warehouse.Create(
            branchId: 1,
            name: "المخزن الرئيسي",
            createdByUserId: null
        );
        db.Warehouses.Add(warehouse);

        var unitPiece = Unit.Create(name: "حبة", symbol: "pcs", isSystem: false, createdByUserId: null);
        var unitKg = Unit.Create(name: "كيلو", symbol: "kg", isSystem: false, createdByUserId: null);
        var unitLiter = Unit.Create(name: "لتر", symbol: "ltr", isSystem: false, createdByUserId: null);
        var unitMeter = Unit.Create(name: "متر", symbol: "m", isSystem: false, createdByUserId: null);
        var unitCarton = Unit.Create(name: "كرتون", symbol: "ctn", isSystem: false, createdByUserId: null);
        var unitBox = Unit.Create(name: "علبة", symbol: "box", isSystem: false, createdByUserId: null);
        var unitGram = Unit.Create(name: "جرام", symbol: "g", isSystem: false, createdByUserId: null);
        db.Units.AddRange(unitPiece, unitKg, unitLiter, unitMeter, unitCarton, unitBox, unitGram);
        await db.SaveChangesAsync();
        logger?.LogInformation("Seeded {Count} base units of measure.", 7);

        db.DocumentSequences.Add(DocumentSequence.Create("SalesInvoice"));
        db.DocumentSequences.Add(DocumentSequence.Create("PurchaseInvoice"));
        db.DocumentSequences.Add(DocumentSequence.Create("SalesReturn"));
        db.DocumentSequences.Add(DocumentSequence.Create("PurchaseReturn"));
        db.DocumentSequences.Add(DocumentSequence.Create("Transfer"));
        db.DocumentSequences.Add(DocumentSequence.Create("CustomerPayment"));
        db.DocumentSequences.Add(DocumentSequence.Create("SupplierPayment"));

        var productsWithoutUnits = await db.Products
            .Where(p => !db.ProductUnits.Any(pu => pu.ProductId == p.Id))
            .ToListAsync();

        foreach (var product in productsWithoutUnits)
        {
            db.ProductUnits.Add(ProductUnit.CreateBaseUnit(
                product.Id,
                unitPiece.Id
            ));
        }

        if (productsWithoutUnits.Any())
            logger?.LogInformation("Seeded base ProductUnits for {Count} products", productsWithoutUnits.Count);

        await db.SaveChangesAsync();

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

    // ═══════════════════════════════════════════════════════════════
    // Helper: Assign permissions to a role (idempotent)
    // ═══════════════════════════════════════════════════════════════
    private static async Task AssignRolePermissionsAsync(
        SalesDbContext db,
        string roleName,
        List<Permission> allPermissions,
        Func<Permission, bool> predicate,
        ILogger? logger)
    {
        var role = await db.Set<Role>().FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null) return;

        // Skip if already assigned — role already has any RolePermission record
        if (await db.Set<RolePermission>().AnyAsync(rp => rp.RoleId == role.Id))
        {
            logger?.LogInformation("Role '{RoleName}' already has permissions assigned — skipping.", roleName);
            return;
        }

        var selected = allPermissions.Where(predicate).ToList();
        foreach (var perm in selected)
        {
            db.Set<RolePermission>().Add(RolePermission.Create(role.Id, perm.Id));
        }

        await db.SaveChangesAsync();
        logger?.LogInformation("Assigned {Count} permissions to role '{RoleName}'.", selected.Count, roleName);
    }
}
