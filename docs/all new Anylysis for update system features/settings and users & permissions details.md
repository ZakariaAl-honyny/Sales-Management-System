Final Audit Summary

Business Settings (31)

| # | Key | Status | Consumer |
|---|-----|--------|----------|
| 1 | AllowNegativeStock | ✅ WIRED | SalesService, PurchaseReturnService, InventoryService |
| 2 | EnableFefo | ✅ WIRED | FifoAllocationService |
| 3 | StockAlertDays | ✅ WIRED | MinStockAlertWorker |
| 4 | AutoPostInvoices | ✅ WIRED | SalesService |
| 5 | AllowDrafts | ✅ WIRED | SalesService |
| 6 | ShowProfitInInvoice | ✅ WIRED | SalesInvoiceEditorViewModel |
| 7 | PreventBelowRetailPrice | ✅ WIRED | SalesService |
| 8 | AllowBelowCostSale | ✅ WIRED | SalesService |
| 9 | DefaultCashCustomerId | ✅ WIRED | SalesService |
| 10 | PurchaseAutoPost | ✅ WIRED | PurchaseService |
| 11 | DefaultCashSupplierId | ✅ WIRED | PurchaseService |
| 12 | EnableBarcode | ❌ DEAD | No consumer |
| 13 | BarcodeInputType | ❌ DEAD | No consumer |
| 14 | AutoGenerateBarcode | ✅ WIRED | ProductEditorViewModel |
| 15 | AutoCreateJournalEntry | ✅ WIRED | AccountingIntegrationService (17 methods) |
| 16 | DecimalPlaces | ❌ DEAD | Currency.DecimalPlaces is source of truth |
| 17 | Language | ❌ DEAD | No localization system |
| 18 | DateFormat | ❌ DEAD | No date formatting system |
| 19 | PaperSize | ✅ WIRED | PrintDataService → A4InvoiceDocument |
| 20 | PrintCopies | ✅ WIRED | PrintService |
| 21 | ShowBalanceOnPrint | ✅ WIRED | A4InvoiceDocument, ThermalReceiptGenerator |
| 22 | PrintSignature | ✅ WIRED | A4InvoiceDocument, ThermalReceiptGenerator |
| 23 | HideTaxInSales | ✅ WIRED | SalesInvoiceEditorViewModel |
| 24 | ShowExpiryInInvoices | ✅ WIRED | A4InvoiceDocument, ThermalReceiptGenerator |
| 25 | HideTaxInPurchases | ✅ WIRED | PurchaseInvoiceEditorViewModel |
| 26 | ShowLogo | ✅ WIRED | PrintDataService |
| 27 | FooterNote | ✅ WIRED | A4InvoiceDocument, ThermalReceiptGenerator |
| 28 | LowStockAlert | ✅ WIRED | MinStockAlertWorker |
| 29 | ExpiryAlert | ❌ DEAD | No expiry alert worker |
| 30 | ExpiryAlertDays | ❌ DEAD | No expiry alert worker |
| 31 | CreditLimitAlert | ✅ WIRED | SalesService |

Store Settings (11)

| # | Key | Status | Consumer |
|---|-----|--------|----------|
| 32 | Store.Name | ✅ WIRED | PrintDataService |
| 33 | Store.Phone | ✅ WIRED | PrintDataService |
| 34 | Store.Address | ✅ WIRED | PrintDataService |
| 35 | Store.LogoPath | ✅ WIRED | PrintDataService |
| 36 | Store.Email | ✅ WIRED | PrintDataService |
| 37 | Store.CurrencyCode | ✅ WIRED | PrintDataService |
| 38 | Store.TaxNumber | ✅ WIRED | PrintDataService |
| 39 | Store.EnableStockAlerts | ✅ WIRED | DashboardViewModel |
| 40 | Store.AllowNegativeStock | ✅ WIRED | PrintDataService (DTO) |
| 41 | Store.AutoUpdatePrices | ❌ DEAD | No consumer |
| 42 | Store.SignaturePath | ✅ WIRED | PrintDataService |

Final Count
- 34 WIRED ✅
- 8 DEAD ❌

The 8 dead settings are:
1. EnableBarcode — needs barcode toggle infrastructure
2. BarcodeInputType — needs camera/scanner mode
3. DecimalPlaces — redundant with Currency.DecimalPlaces
4. Language — needs WPF localization
5. DateFormat — needs date formatting system
6. ExpiryAlert — needs ExpiryAlertWorker
7. ExpiryAlertDays — needs ExpiryAlertWorker
8. Store.AutoUpdatePrices — needs auto-price-update logic

These are all "V2 deferred" items that require new infrastructure.

هذه القائمة جيدة، لكنني أرى أنها تخلط بين ثلاثة أنواع مختلفة من الإعدادات، وهذا سيجعلها تكبر مع الوقت ويصعب إدارتها.

أقترح أولاً تقسيمها منطقياً.

1. إعدادات تشغيل (Business Rules)

هذه تؤثر على سلوك النظام نفسه.

أحتفظ بها:

```text
AllowNegativeStock ✅

EnableFefo ✅

StockAlertDays ✅

AutoPostInvoices ✅

AllowDrafts ✅

PreventBelowRetailPrice ✅

AllowBelowCostSale ✅

DefaultCashCustomerId ✅

PurchaseAutoPost ✅

DefaultCashSupplierId ✅

AutoCreateJournalEntry ✅

LowStockAlert ✅

CreditLimitAlert ✅
```

وأضيف مستقبلاً:

```text
AllowInvoiceCancellation

AllowBackDateDocuments

AllowFutureDateDocuments

RequireCustomerInCashSales
```

هذه كلها قواعد عمل حقيقية.

━━━━━━━━━━━━━━━━━━━━
2. إعدادات واجهة المستخدم

هذه لا تؤثر على المحاسبة.

مثل:

```text
ShowProfitInInvoice

HideTaxInSales

HideTaxInPurchases
```

أنا لا أحب أن تكون إعدادات نظام عامة.

الأفضل أن تكون:

- صلاحية (Permission).
- أو تفضيل مستخدم (User Preference) إذا أضفتها مستقبلاً.

مثلاً:

```text
ShowProfitInInvoice
```

لا ينبغي أن يكون إعداداً عاماً، لأنه قد يكون:

- المدير يراه.
- البائع لا يراه.

وهذا يعتمد على الصلاحيات أكثر من الإعدادات.

━━━━━━━━━━━━━━━━━━━━
3. إعدادات الطباعة

هذه مجموعة مستقلة:

```text
PaperSize ✅

PrintCopies ✅

ShowBalanceOnPrint ✅

PrintSignature ✅

ShowLogo ✅

FooterNote ✅

ShowExpiryInInvoices ✅
```

وأنا أضيف:

```text
PrintBarcode

PrintQRCode

PrintCompanyAddress
```

━━━━━━━━━━━━━━━━━━━━
الإعدادات التي أرى أنها ليست ضرورية
━━━━━━━━━━━━━━━━━━━━

1.

```text
EnableBarcode
```

❌ أحذفه.

لأن نظامك كله مبني على الباركود.

لا معنى لتعطيله.

━━━━━━━━━━━━━━━━━━━━

2.

```text
BarcodeInputType
```

❌ لا أجعله System Setting.

هذه تفضيلات جهاز أو مستخدم.

مثال:

```text
Keyboard Scanner

Camera

Bluetooth Scanner
```

هذه ليست إعداداً عاماً للنظام.

━━━━━━━━━━━━━━━━━━━━

3.

```text
DecimalPlaces
```

✅ أحذفه.

كما ذكرت.

مصدر الحقيقة هو:

```text
Currencies.DecimalPlaces
```

━━━━━━━━━━━━━━━━━━━━

4.

```text
Language
```

إذا النظام عربي فقط حالياً:

❌ لا تضفه.

إذا دعمت الإنجليزية لاحقاً:

أضفه.

━━━━━━━━━━━━━━━━━━━━

5.

```text
DateFormat
```

❌ لا أنصح به.

اتركه للثقافة (Culture) الخاصة بالنظام.

━━━━━━━━━━━━━━━━━━━━

6.

```text
Store.AutoUpdatePrices
```

أنا أيضاً لا أرى فائدته في V1.

━━━━━━━━━━━━━━━━━━━━
الإعدادات التي أرى أنها ناقصة
━━━━━━━━━━━━━━━━━━━━

هذه أراها أهم من بعض الموجودة.

━━━━━━━━━━━━━━━━━━━━

```text
DefaultSalesTax
```

الضريبة الافتراضية.

━━━━━━━━━━━━━━━━━━━━

```text
DefaultPurchaseTax
```

━━━━━━━━━━━━━━━━━━━━

```text
AllowNegativeCash
```

هل يسمح بخروج الصندوق للسالب؟

━━━━━━━━━━━━━━━━━━━━

```text
AutoCreateCashCustomer
```

إذا لم يختر المستخدم عميلاً.

━━━━━━━━━━━━━━━━━━━━

```text
DefaultBranch
```

للشركة.

━━━━━━━━━━━━━━━━━━━━

```text
DefaultWarehouse
```

إذا كان الفرع يملك أكثر من مستودع.

━━━━━━━━━━━━━━━━━━━━

```text
RequireBatchOnPurchase
```

إذا كان TrackExpiry = true.

━━━━━━━━━━━━━━━━━━━━

```text
RequireExpiryOnPurchase
```

إجباري عند الشراء.

━━━━━━━━━━━━━━━━━━━━

```text
AllowDuplicateBarcode
```

أنا أوصي أن تكون:

```text
False
```

لكن كإعداد يمكن تغييره.

━━━━━━━━━━━━━━━━━━━━

```text
AutoPrintAfterPosting
```

مفيد جداً.

━━━━━━━━━━━━━━━━━━━━

```text
EnableAttachments
```

إذا أردت إخفاء الميزة عن بعض العملاء.

━━━━━━━━━━━━━━━━━━━━

```text
EnableNotifications
```

تشغيل أو إيقاف التنبيهات.

━━━━━━━━━━━━━━━━━━━━
ملاحظة معمارية مهمة
━━━━━━━━━━━━━━━━━━━━

أنت الآن لديك:

```text
CompanySettings
```

و:

```text
SystemSettings
```

وأرى أن التقسيم الصحيح هو:

CompanySettings:
- بيانات المنشأة الثابتة.

مثل:

```text
الاسم

العنوان

الهاتف

الشعار

الرقم الضريبي
```

ولا شيء آخر تقريباً.

أما جميع القواعد والسلوكيات فتكون في:

```text
SystemSettings
```

بهذا لا تختلط بيانات الشركة مع إعدادات تشغيل النظام.

الخلاصة: من أصل 42 إعداداً، أرى أن حوالي 30 منها مناسبة لـ V1، ويمكن حذف أو تأجيل الإعدادات المرتبطة ببنية غير موجودة (مثل Localization أو Expiry Worker)، مع نقل بعض الإعدادات الخاصة بالواجهة إلى نظام الصلاحيات أو تفضيلات المستخدم بدلاً من جعلها إعدادات عامة للنظام.

Taxs table


بخصوص الصلاحيات التي اقترحتها انا اريد ان اجعل المستخدم ان يعدل على الصلاحيات للحساب وتخصيصها وسحببها ماعداء مدير النظام لايمكن حذف او تعديل صلاحياته كيف يمكنني فعل ذلك اشرحلي بتفصيل وهل احتاج وجهات جديد لذلك وهل احتاج تبويب خاص بلمستخدمين وصلاحاتهم


أرى أن هذه من أهم الوحدات في النظام، وإذا صممتها جيداً فلن تحتاج لتعديلها لسنوات.

وأوصيك ألا تعتمد على "صلاحيات ثابتة في الكود"، بل على نظام RBAC (Role-Based Access Control) مع إمكانية إضافة استثناءات للمستخدم.

التصميم الذي أوصي به هو كالتالي.

أولاً: لا تجعل المستخدم يمتلك الصلاحيات مباشرة

بدلاً من:

```text
User
   └── Permissions
```

اجعل:

```text
User
    │
    ▼
Role
    │
    ▼
Permissions
```

مثال:

```text
مدير النظام

    │

    ▼

جميع الصلاحيات
```

```text
أمين صندوق

    │

    ▼

قبض
صرف
عرض الصندوق
```

وهذا يغطي 95% من الحالات.

ثانياً: أضف استثناءات للمستخدم

أحياناً تريد:

```text
أمين صندوق
```

لكن هذا الشخص بالذات:

```text
يسمح له بإلغاء الفواتير.
```

ولا تريد إعطاء هذه الصلاحية لكل أمناء الصندوق.

هنا نضيف:

```text
UserPermissions
```

مثال:

```sql
UserPermissions
---------------
Id

UserId

PermissionId

IsGranted
```

الفكرة:

إذا كان الدور لا يملك صلاحية:

```text
DeleteInvoice
```

لكن:

```text
UserPermissions

IsGranted = true
```

فالمستخدم يملكها.

والعكس أيضاً.

إذا الدور يملك صلاحية لكن تريد منع مستخدم معين:

```text
IsGranted = false
```

فيتم سحبها منه.

إذن يصبح لدينا:

```text
RolePermissions
```

للصلاحيات العامة.

و:

```text
UserPermissions
```

للاستثناءات.

ثالثاً: مدير النظام

أنصح بعدم تخزين صلاحيات مدير النظام في RolePermissions فقط.

بل يكون عنده خاصية:

```text
IsSystemAdmin
```

داخل Users.

مثال:

```sql
Users
-----
...
IsSystemAdmin BIT
```

إذا:

```text
IsSystemAdmin = true
```

فالنظام يتجاوز كل فحص للصلاحيات.

ولا يسمح:

- بحذف المستخدم.
- بتعطيله.
- بإزالة هذه الخاصية إلا من خلال عملية خاصة جداً (أو لا يسمح إطلاقاً).

وهذا أفضل من الاعتماد على اسم الدور فقط.

رابعاً: هل نحتاج شاشة مستقلة؟

✅ نعم.

بل أرى أنك تحتاج تبويباً كاملاً.

مثال:

```text
الإدارة

│
├── المستخدمون
├── الأدوار
├── الصلاحيات
├── صلاحيات المستخدمين
└── جلسات المستخدمين
```

خامساً: شاشة الأدوار

مثال:

```text
مدير النظام

محاسب

أمين صندوق

بائع

أمين مستودع
```

يمكن:

- إضافة دور.
- تعديل دور.
- تعطيل دور.

سادساً: شاشة الصلاحيات

هذه غالباً لا تحتاج تعديل.

لأنها تأتي مع النظام.

مثال:

```text
المبيعات

   إنشاء

   تعديل

   حذف

   ترحيل

المشتريات

   إنشاء

   تعديل

...
```

أي أن Permissions عبارة عن Seed Data.

ولا أوصي بالسماح للمستخدم بإضافة صلاحيات جديدة.

سابعاً: شاشة صلاحيات الدور

مثال:

```text
دور:

أمين صندوق
```

ويظهر:

```text
☑ إنشاء سند قبض

☑ إنشاء سند صرف

☑ عرض الصندوق

☐ حذف سند

☐ تعديل القيود

☐ فتح السنة المالية
```

ثم حفظ.

ثامناً: شاشة صلاحيات المستخدم

مثال:

المستخدم:

```text
محمد
```

الدور:

```text
أمين صندوق
```

ثم:

```text
الصلاحيات الإضافية
```

مثال:

```text
☑ إلغاء فاتورة

☑ تعديل سعر البيع

☐ حذف عميل
```

هذه الشاشة تعدل UserPermissions فقط.

تاسعاً: ماذا نفحص عند كل عملية؟

عند الضغط مثلاً على:

```text
ترحيل فاتورة
```

النظام يفحص بالترتيب:

1.

```text
هل IsSystemAdmin = true؟
```

إذا نعم:

يسمح مباشرة.

2.

إذا لا:

يفحص UserPermissions.

إذا وجد استثناء:

يطبقه.

3.

إذا لا يوجد:

يفحص RolePermissions.

وهذا يجعل النظام مرناً جداً.

عاشراً: تقسيم الصلاحيات

لا تجعلها عامة مثل:

```text
Sales
```

بل اجعلها دقيقة.

مثال:

```text
Sales.View

Sales.Create

Sales.Edit

Sales.Delete

Sales.Post

Sales.Cancel

Sales.Return

Sales.Print
```

ونفس الشيء لكل وحدة.

الخلاصة التي أوصي بها

الجداول:

```text
Users

Roles

Permissions

RolePermissions

UserPermissions

UserBranches

UserSessions
```

والشاشات:

```text
المستخدمون

الأدوار

صلاحيات الأدوار

صلاحيات المستخدمين

جلسات المستخدمين
```

أما مدير النظام فأوصي أن يكون مستخدماً نظامياً خاصاً (`IsSystemAdmin = true`) لا يمكن تعطيله أو حذفه أو سحب صلاحياته من خلال الواجهة. هذا أبسط وأكثر أماناً من الاعتماد على الدور وحده، ويمنع أن يقفل المستخدمون النظام على أنفسهم بالخطأ.