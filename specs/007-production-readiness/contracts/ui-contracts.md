# UI Contracts: Phase 7 — Production Readiness

**Phase**: 1 — Design & Contracts  
**Date**: 2026-05-23

---

## Settings Screen (`Views/Settings/SettingsView.xaml`)

**Access**: Admin only (sidebar entry hidden for Manager/Cashier)  
**ViewModel**: `SettingsViewModel` (registered in `App.xaml.cs` DI)

### Fields

| Label (Arabic) | Binding | Control | Validation |
|----------------|---------|---------|------------|
| اسم المتجر * | `StoreName` | `TextBox` | NotEmpty, MaxLength 200 |
| رقم الهاتف | `PhoneNumber` | `TextBox` | Optional, MaxLength 50 |
| العنوان | `Address` | `TextBox` | Optional, MaxLength 500 |
| الشعار | `LogoPath` | `TextBox` + Browse Button | Optional |
| نسبة الضريبة % | `DefaultTaxRate` | `TextBox` (decimal) | 0–100 |
| تفعيل الضريبة | `IsTaxEnabled` | `CheckBox` | — |
| المخزن الافتراضي | `DefaultWarehouseId` | `ComboBox` | Must select valid warehouse |
| طريقة احتساب التكلفة | `CostingMethod` | `RadioButton` group (3 options) | Must select one |

### Buttons

| Button | Arabic Label | Action |
|--------|-------------|--------|
| Save | حفظ الإعدادات | Always enabled; `Validate()` on click → `SaveAsync()` |
| Browse Logo | استعراض | Opens `OpenFileDialog` filtered to image files |

### Costing Method RadioButton Labels

| Value | Arabic Label | Helper Text |
|-------|-------------|-------------|
| 1 | متوسط التكلفة المرجح | يحسب متوسط تكلفة المخزون القديم والجديد — مناسب للتقارير الضريبية |
| 2 | آخر سعر توريد | يستبدل التكلفة بسعر آخر فاتورة شراء مباشرةً |
| 3 | سعر المورد | يعتمد السعر المدخل في بطاقة الصنف من قائمة المورد |

### Behavior
- On load: `GET /api/v1/settings` → populate all fields
- On save: `Validate()` → if valid → `PUT /api/v1/settings` → `IToastNotificationService.ShowSuccess("تم حفظ الإعدادات")`
- On error: `IDialogService.ShowErrorAsync("خطأ", "فشل حفظ الإعدادات")`

---

## User Management Screen (`Views/Users/UsersListView.xaml`)

**Access**: Admin only  
**ViewModel**: `UsersListViewModel`

### List Grid Columns

| Column | Binding | Notes |
|--------|---------|-------|
| # | `Id` | Auto-width |
| الاسم الكامل | `FullName` | Sortable |
| اسم المستخدم | `UserName` | |
| الدور | `RoleName` | Admin / مدير / كاشير |
| الحالة | `IsActive` | Active badge / Inactive badge |
| تاريخ الإنشاء | `CreatedAt` | `dd/MM/yyyy` format |
| إجراءات | — | Edit + Deactivate/Restore buttons |

### Toolbar Buttons

| Button | Arabic Label | Action |
|--------|-------------|--------|
| New User | مستخدم جديد | Opens `UserEditorView` via `ScreenWindowService` |
| Show Inactive | عرض غير النشطين | Toggles `includeInactive` filter |
| Refresh | تحديث | Reloads list from API |

---

## User Editor Screen (`Views/Users/UserEditorView.xaml`)

**ViewModel**: `UserEditorViewModel` (calls `SetDialogService()` in constructor)

### Fields

| Label (Arabic) | Binding | Control | Validation |
|----------------|---------|---------|------------|
| الاسم الكامل * | `FullName` | `TextBox` | NotEmpty, MaxLength 150 |
| اسم المستخدم * | `UserName` | `TextBox` | NotEmpty, unique, no spaces |
| كلمة المرور * | `Password` | `PasswordBox` | MinLength 8 (create only; empty = no change on edit) |
| الدور * | `SelectedRole` | `ComboBox` | Must select one |
| نشط | `IsActive` | `CheckBox` | Read-only on create; editable on edit |

### Buttons

| Button | Arabic Label | Behavior |
|--------|-------------|---------|
| Save | حفظ | Always enabled; `ValidateAsync()` on click |
| Cancel | إلغاء | Closes editor window without saving |

### Validation Messages (Arabic)
- `"الاسم الكامل مطلوب"`
- `"اسم المستخدم مطلوب"`
- `"كلمة المرور يجب أن تكون 8 أحرف على الأقل"`
- `"يجب اختيار دور للمستخدم"`

---

## Backup Screen (`Views/Settings/BackupView.xaml`)

**Access**: Admin only  
**ViewModel**: `BackupViewModel`

### Layout

```
┌─────────────────────────────────────────┐
│ النسخ الاحتياطي واستعادة البيانات        │
├─────────────────────────────────────────┤
│ [إنشاء نسخة احتياطية الآن]              │
│ مسار النسخ: C:\SalesSystemBackups        │
├─────────────────────────────────────────┤
│ النسخ المتاحة:                           │
│  ┌──────────────────┬──────┬──────────┐  │
│  │ اسم الملف        │ الحجم│ التاريخ  │  │
│  ├──────────────────┼──────┼──────────┤  │
│  │ SalesSystem_...  │ 10MB │ 23/05/26 │  │
│  └──────────────────┴──────┴──────────┘  │
│  [استعادة من النسخة المحددة]             │
└─────────────────────────────────────────┘
```

### Buttons

| Button | Arabic Label | Action |
|--------|-------------|--------|
| Backup Now | إنشاء نسخة احتياطية الآن | `POST /api/v1/backup` → toast on success |
| Restore Selected | استعادة من النسخة المحددة | Confirmation dialog → `POST /api/v1/backup/restore` → force re-login |

### Restore Confirmation Dialog
- Title: `تحذير — استعادة قاعدة البيانات`
- Message: `سيؤدي هذا الإجراء إلى استبدال جميع البيانات الحالية بالنسخة الاحتياطية المحددة. لا يمكن التراجع عن هذا الإجراء. هل أنت متأكد؟`
- Buttons: `تأكيد الاستعادة` (red) | `إلغاء`

---

## Database Error Dialog (`Views/Common/DatabaseErrorDialog.xaml`)

**Trigger**: Desktop startup health check failure  
**Behavior**: Shown before login window appears

### Layout

```
┌──────────────────────────────────────────┐
│ ⚠️  تعذر الاتصال بقاعدة البيانات          │
│                                          │
│  تأكد من أن خدمة النظام تعمل وأن SQL    │
│  Server متاح، ثم حاول مرة أخرى.         │
│                                          │
│  [إعادة المحاولة]     [إغلاق البرنامج]   │
└──────────────────────────────────────────┘
```

- Retry: calls health check again (up to 3 times with 2s delay)
- Exit: `Application.Current.Shutdown(1)`
- Self-ownership guard applied (`PositionOverOwner()` pattern)

---

## Auto-Update Prompt (`Views/Common/UpdateAvailableDialog.xaml`)

**Trigger**: Background check finds newer version  
**Behavior**: Shown on UI thread after login; non-blocking

### Layout

```
┌──────────────────────────────────────────┐
│ 🔄  تحديث جديد متاح — الإصدار 4.6.4      │
│                                          │
│  ملاحظات الإصدار: [...]                  │
│                                          │
│  [تثبيت الآن]    [تذكيري لاحقاً]  [تخطي]│
└──────────────────────────────────────────┘
```

- Install Now: launches downloaded installer via `Process.Start()`
- Remind Later: dismisses without marking as skipped
- Skip: writes `skippedVersion` to `settings.json`
