# خطة تطبيق صلاحيات المستخدمين على واجهة WPF

## الهدف
إخفاء عناصر القائمة الجانبية (Sidebar) وقائمة الـ Menu Bar عن المستخدمين الذين لا يملكون الصلاحية الكافية، بناءً على دور المستخدم المسجّل في الجلسة (`ISessionService`).

---

## الوضع الحالي

### البنية الموجودة ✅
- `Permission.cs` — Enum كامل لكل الصلاحيات موجود ومرتبط بالأدوار
- `SessionService.CanAccess(Permission)` — دالة التحقق من الصلاحية موجودة
- `MainWindow.xaml` — الشريط الجانبي يحتوي `x:Name` لبعض العناصر لكن ليس الكل
- `MainWindow.xaml.cs` — يمتلك `_session` ومنطق التنقل `NavigateTo()`

### المشكلة ❌
- لا يوجد كود يُخفي أي عنصر بناءً على الصلاحية
- بعض عناصر الـ ListBoxItem تفتقر إلى `x:Name` مما يجعل التحكم فيها صعباً من code-behind

---

## خطة التنفيذ — 3 خطوات

### الخطوة 1: إضافة `x:Name` للعناصر التي تفتقرها في `MainWindow.xaml`

العناصر الحالية **بدون** `x:Name` والتي تحتاج صلاحيات:

| العنصر | Tag | الصلاحية المطلوبة |
|--------|-----|-------------------|
| المبيعات | `Sales` | `SalesInvoice` — AllStaff |
| العملاء | `Customers` | `CustomerView` — AllStaff |
| سداد العملاء | `CustomerPayments` | `CustomerPayment` — AllStaff |
| مرتجعات المبيعات | `SalesReturns` | `SalesReturn` — AllStaff |
| نواقص المخزون | `LowStock` | `Reports` — ManagerAndAbove |

> العناصر التي تملك `x:Name` مسبقاً: `NavPurchasesItem`, `NavProductsItem`, `NavSuppliersItem`, `NavWarehousesItem`, `NavSupplierPaymentsItem`, `NavStockTransfersItem`, `NavReportsItem`, `NavPurchaseReturnsItem`, `NavCategoriesItem`, `NavUnitsItem`, `NavUsersItem`, `NavSettingsItem`

**الملفات التي تتأثر:** `MainWindow.xaml`

---

### الخطوة 2: تعديل `MainWindow.xaml.cs` — دالة `ApplyPermissions()`

إضافة دالة جديدة تُستدعى فور تحميل النافذة وتُطبّق الرؤية على كل عنصر:

```csharp
private void ApplyPermissions()
{
    var s = _session;

    // ─── AllStaff (Cashier+) ─────────────────────────────────────
    // Sales + SalesReturns + Customers + CustomerPayments — مرئية للجميع دائماً

    // ─── ManagerAndAbove ─────────────────────────────────────────
    NavPurchasesItem.Visibility       = s.CanAccess(Permission.PurchaseInvoice)    ? Visibility.Visible : Visibility.Collapsed;
    NavPurchaseReturnsItem.Visibility = s.CanAccess(Permission.PurchaseReturn)      ? Visibility.Visible : Visibility.Collapsed;
    NavProductsItem.Visibility        = s.CanAccess(Permission.ProductManagement)   ? Visibility.Visible : Visibility.Collapsed;
    NavSuppliersItem.Visibility       = s.CanAccess(Permission.SupplierManagement)  ? Visibility.Visible : Visibility.Collapsed;
    NavSupplierPaymentsItem.Visibility= s.CanAccess(Permission.SupplierManagement)  ? Visibility.Visible : Visibility.Collapsed;
    NavStockTransfersItem.Visibility  = s.CanAccess(Permission.StockTransfer)       ? Visibility.Visible : Visibility.Collapsed;
    NavReportsItem.Visibility         = s.CanAccess(Permission.Reports)             ? Visibility.Visible : Visibility.Collapsed;
    NavLowStockItem.Visibility        = s.CanAccess(Permission.Reports)             ? Visibility.Visible : Visibility.Collapsed;
    NavCategoriesItem.Visibility      = s.CanAccess(Permission.ProductManagement)   ? Visibility.Visible : Visibility.Collapsed;
    NavUnitsItem.Visibility           = s.CanAccess(Permission.ProductManagement)   ? Visibility.Visible : Visibility.Collapsed;

    // ─── AdminOnly ───────────────────────────────────────────────
    NavWarehousesItem.Visibility      = s.CanAccess(Permission.WarehouseManagement) ? Visibility.Visible : Visibility.Collapsed;
    NavUsersItem.Visibility           = s.CanAccess(Permission.UserManagement)      ? Visibility.Visible : Visibility.Collapsed;
    NavSettingsItem.Visibility        = s.CanAccess(Permission.Settings)            ? Visibility.Visible : Visibility.Collapsed;
}
```

**الملفات التي تتأثر:** `MainWindow.xaml.cs`

---

### الخطوة 3: الحماية الثانوية في `NavigateTo()` — منع الوصول المباشر

حتى لو تجاوز المستخدم القائمة (عبر اختصار أو غيره)، يتم التحقق مرة ثانية:

```csharp
private bool CanNavigateTo(string tag)
{
    return tag switch
    {
        "Purchases"        => _session.CanAccess(Permission.PurchaseInvoice),
        "PurchaseReturns"  => _session.CanAccess(Permission.PurchaseReturn),
        "Products"         => _session.CanAccess(Permission.ProductManagement),
        "Suppliers"        => _session.CanAccess(Permission.SupplierManagement),
        "SupplierPayments" => _session.CanAccess(Permission.SupplierManagement),
        "StockTransfers"   => _session.CanAccess(Permission.StockTransfer),
        "Reports"          => _session.CanAccess(Permission.Reports),
        "LowStock"         => _session.CanAccess(Permission.Reports),
        "Warehouses"       => _session.CanAccess(Permission.WarehouseManagement),
        "Users"            => _session.CanAccess(Permission.UserManagement),
        "Settings"         => _session.CanAccess(Permission.Settings),
        "Categories"       => _session.CanAccess(Permission.ProductManagement),
        "Units"            => _session.CanAccess(Permission.ProductManagement),
        _ => true  // Dashboard, Sales, SalesReturns, Customers, CustomerPayments
    };
}
```

**الملفات التي تتأثر:** `MainWindow.xaml.cs`

---

## ملخص الصلاحيات حسب الدور

| الشاشة | Cashier | Manager | Admin |
|--------|:-------:|:-------:|:-----:|
| لوحة التحكم | ✅ | ✅ | ✅ |
| المبيعات | ✅ | ✅ | ✅ |
| مرتجعات المبيعات | ✅ | ✅ | ✅ |
| العملاء | ✅ | ✅ | ✅ |
| سداد العملاء | ✅ | ✅ | ✅ |
| المشتريات | ❌ | ✅ | ✅ |
| مرتجعات المشتريات | ❌ | ✅ | ✅ |
| المنتجات | ❌ | ✅ | ✅ |
| الموردين | ❌ | ✅ | ✅ |
| سداد الموردين | ❌ | ✅ | ✅ |
| نقل المخزون | ❌ | ✅ | ✅ |
| التقارير | ❌ | ✅ | ✅ |
| نواقص المخزون | ❌ | ✅ | ✅ |
| التصنيفات | ❌ | ✅ | ✅ |
| الوحدات | ❌ | ✅ | ✅ |
| المستودعات | ❌ | ❌ | ✅ |
| المستخدمين | ❌ | ❌ | ✅ |
| الإعدادات | ❌ | ❌ | ✅ |

---

## الملفات المتأثرة (فقط)
1. `MainWindow.xaml` — إضافة `x:Name` للعناصر الناقصة
2. `MainWindow.xaml.cs` — إضافة `ApplyPermissions()` + `CanNavigateTo()`

> **لا تغييرات على:** `Permission.cs`, `SessionService.cs`, أو أي ViewModel.
