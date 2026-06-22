# شرح خدمة إنشاء الحسابات المحاسبية (AccountService.CreateAsync)

## 📍 موقع الملف
`SalesSystem.Application\Services\AccountService.cs` — السطر 87

---

## 🧩 نظرة عامة

الـ `CreateAsync` هي المسؤولة عن إنشاء حساب جديد في **دليل الحسابات** (Chart of Accounts).  
هذه ليست مجرد إضافة سطر في قاعدة البيانات، بل عملية متكاملة تشمل:

1. التحقق من صحة المدخلات والعلاقات
2. توليد كود الحساب تلقائياً
3. إنشاء قيد محاسبي (Journal Entry) للرصيد الافتتاحي إن وجد
4. حفظ كل شيء في **ترانزاكشن واحد** لضمان التكامل

---

## 📋 الخطوات بالتفصيل مع أمثلة

### الخطوة 1: التحقق من الحساب الأب (Parent Account)

```csharp
if (request.ParentId.HasValue)
{
    parent = await _uow.Accounts.GetByIdAsync(request.ParentId.Value, ct);
    if (parent == null)
        return Result<AccountDto>.Failure("الحساب الأب غير موجود", ErrorCodes.NotFound);
    if (!parent.IsActive)
        return Result<AccountDto>.Failure("الحساب الأب غير نشط", ErrorCodes.InvalidOperation);
    if (parent.IsLeaf)
        return Result<AccountDto>.Failure(
            "لا يمكن إضافة حساب فرعي لحساب تفصيلي — الحساب الأب يجب أن يكون مجموعة",
            ErrorCodes.ValidationError);
}
```

#### مثال:
- تريد إضافة حساب `"مبيعات الجملة"` تحت `"إيرادات المبيعات"` (رقمه 1520)
- النظام يتحقق:
  - هل الحساب الأب `1520` موجود؟ ✅ نعم
  - هل هو نشط؟ ✅ نعم
  - هل هو Leaf (تفصيلي)؟ ❌ لا (لأنه مستوى 3 وليس Level 4)
- إذا كان `1520` تفصيلياً (`IsLeaf = true`) → يرفض النظام لأنك لا تستطيع إضافة أبناء لحساب تفصيلي

---

### الخطوة 2: منع تكرار الاسم تحت نفس الأب

```csharp
if (request.ParentId.HasValue)
{
    var duplicateName = await _uow.Accounts.AnyAsync(
        a => a.ParentId == request.ParentId.Value && a.NameAr == request.NameAr.Trim() && a.IsActive, ct);
    if (duplicateName)
        return Result<AccountDto>.Failure(
            $"يوجد حساب بنفس الاسم '{request.NameAr}' تحت نفس الحساب الأب",
            ErrorCodes.DuplicateEntry);
}
```

#### مثال:
- تحاول إضافة حساب باسم `"مبيعات الجملة"` تحت `1520 — إيرادات المبيعات`
- النظام يبحث: هل يوجد حساب نشط بنفس الاسم تحت نفس الأب؟
- يوجد بالفعل → `"يوجد حساب بنفس الاسم 'مبيعات الجملة' تحت نفس الحساب الأب"` ❌

---

### الخطوة 3: حساب المستوى (Level)

```csharp
byte level;
if (request.ParentId.HasValue && parent != null)
    level = (byte)(parent.Level + 1);
else
    level = 1;

// Leaf accounts must be detail level (4)
if (request.IsLeaf && level < 4)
    level = 4;

// Ensure non-leaf accounts don't exceed level 3
if (!request.IsLeaf && level > 3)
    level = 3;
```

#### أمثلة:
| الحالة | مستوى الأب | `IsLeaf` | النتيجة | الشرح |
|--------|-----------|----------|---------|-------|
| حساب رئيسي بدون أب | — | false | Level = 1 | أعلى مستوى في الشجرة |
| حساب فرعي تحت Level 1 | Level 1 | false | Level = 2 | مجموعة فرعية |
| حساب فرعي تحت Level 3 | Level 3 | true | Level = 4 | تفصيلي (يسمح بالحركات) |
| حساب فرعي تحت Level 2 | Level 2 | true | Level = 4 | تم رفعه من 3 إلى 4 لأن Leaf يجب أن يكون تفصيلياً |
| حساب غير تفصيلي تحت Level 3 | Level 3 | false | Level = 3 | تم تثبيته لأن المجموعات لا تتجاوز Level 3 |

---

### الخطوة 4: توليد كود الحساب (AccountCode)

```csharp
var codeResult = await _codeGenerator.GenerateCodeAsync(request.ParentId, level, ct);
```

النظام يولد الكود تلقائياً بنمط هرمي:

| المستوى | طول الكود | مثال |
|---------|-----------|------|
| Level 1 — مجموعة | رقم واحد | `1` — الأصول، `2` — الخصوم |
| Level 2 — رئيسي | رقمين | `11` — النقدية، `15` — الإيرادات |
| Level 3 — فرعي | 4 أرقام | `1520` — إيرادات المبيعات |
| Level 4 — تفصيلي | 8 أرقام | `15200001` — مبيعات الجملة |

> **ملاحظة:** المستخدم **لا يدخل** كود الحساب أبداً — النظام يولدّه تلقائياً.

---

### الخطوة 5: توليد لون الحساب تلقائياً

```csharp
var colorCode = IAccountCodeGeneratorService.GetColorCode(request.Nature);
```

| طبيعة الحساب (Nature) | اللون | مثال |
|----------------------|-------|------|
| 1 — أصل (Asset) | `#2196F3` (أزرق) | النقدية، البنوك، العملاء |
| 2 — خصم (Liability) | `#F44336` (أحمر) | الموردون، القروض |
| 3 — ملكية (Equity) | `#4CAF50` (أخضر) | رأس المال، الأرباح المحتجزة |
| 4 — إيراد (Revenue) | `#4CAF50` (أخضر) | إيرادات المبيعات |
| 5 — مصروف (Expense) | `#FF9800` (برتقالي) | تكلفة المبيعات، المصروفات العمومية |

---

### الخطوة 6: إنشاء كيان الحساب (Domain Entity)

```csharp
var account = Account.Create(
    accountCode: codeResult.Value!,
    nameAr: request.NameAr,
    nameEn: request.NameEn,
    nature: request.Nature,
    isLeaf: request.IsLeaf,
    parentId: request.ParentId,
    isSystem: request.IsSystem,
    categoryId: request.CategoryId,
    level: level,
    description: request.Description,
    colorCode: colorCode,
    notes: request.Notes,
    createdByUserId: userId);
```

الـ `Account.Create()` تقوم بالتحقق من:
- `accountCode` لا يمكن أن يكون فارغاً
- `nameAr` مطلوب (الاسم العربي)
- `nature` يجب أن يكون بين 1-5
- `level` يجب أن يكون بين 1-10
- `description` لا يتجاوز 500 حرف
- `colorCode` لا يتجاوز 7 أحرف (`#2196F3`)

---

### الخطوة 7: الحفظ في ترانزاكشن + الرصيد الافتتاحي

```csharp
await _uow.ExecuteTransactionAsync(async () =>
{
    await _uow.Accounts.AddAsync(account, ct);
    await _uow.SaveChangesAsync(ct);

    if (request.OpeningBalance.HasValue && request.OpeningBalance.Value > 0)
    {
        // البحث عن حساب الأرصدة الافتتاحية في SystemAccountMappings
        var systemMapping = await _uow.SystemAccountMappings.FirstOrDefaultAsync(
            m => m.MappingKey == SystemAccountKey.OpeningBalanceEquity.ToString(), ct);

        // إنشاء قيد يومي (Journal Entry)
        var je = JournalEntry.Create(...);

        // إذا كان الحساب مديناً بطبيعته (Asset/Expense):
        if (account.IsDebitNormal())
        {
            je.AddDebitLine(account.Id, request.OpeningBalance.Value, "الرصيد الافتتاحي");
            je.AddCreditLine(systemMapping.AccountId, request.OpeningBalance.Value, "الرصيد الافتتاحي");
        }
        else // Liability/Equity/Revenue — دائن بطبيعته
        {
            je.AddCreditLine(account.Id, request.OpeningBalance.Value, "الرصيد الافتتاحي");
            je.AddDebitLine(systemMapping.AccountId, request.OpeningBalance.Value, "الرصيد الافتتاحي");
        }

        await _uow.JournalEntries.AddAsync(je, ct);
        await _uow.SaveChangesAsync(ct);
    }
}, ct);
```

#### أمثلة على الرصيد الافتتاحي:

**مثال 1: حساب أصل (مدين بطبيعته) — فتح رصيد لصندوق نقدية**
```
طبيعة الحساب: Asset (Nature = 1) → IsDebitNormal() = true

قيود اليومية:
    1111 — الصندوق الرئيسي             10,000 ريال (مدين)
        1422 — الأرصدة الافتتاحية                10,000 ريال (دائن)
```

**مثال 2: حساب خصم (دائن بطبيعته) — رصيد مورد**
```
طبيعة الحساب: Liability (Nature = 2) → IsDebitNormal() = false

قيود اليومية:
    1422 — الأرصدة الافتتاحية             5,000 ريال (مدين)
        2100 — موردون                                 5,000 ريال (دائن)
```

---

## 🔗 شرح SystemAccountMappings

### ما هو SystemAccountMapping؟

`SystemAccountMapping` هو **جدول ربط** (Mapping Table) يربط بين **وظيفة محاسبية** (مثلاً "إيرادات المبيعات") و **حساب معين** في دليل الحسابات.

بدون هذا الجدول، الكود كان يستخدم أرقام حسابات ثابتة (Hardcoded). الآن هو ديناميكي — يمكن تغيير الحساب المرتبط بكل وظيفة دون تعديل الكود.

### هيكل الجدول

```csharp
public class SystemAccountMapping : Entity
{
    public string MappingKey { get; private set; }  // مثلاً "SalesRevenue"
    public int AccountId { get; private set; }       // مثلاً 1520 (إيرادات المبيعات)
    public Account? Account { get; private set; }     // Navigation property
    public short? BranchId { get; private set; }      // للفروع (null = عام)
}
```

### جميع المفاتيح (21 مفتاحاً)

| # | المفتاح (MappingKey) | الوظيفة | مثال الحساب المرتبط |
|---|---------------------|---------|---------------------|
| 1 | `DefaultCash` | الصندوق الافتراضي | 1111 — صندوق رئيسي |
| 2 | `DefaultBank` | البنك الافتراضي | 1120 — البنوك |
| 3 | `AccountsReceivable` | العملاء (ذمم مدينة) | 1130 — العملاء |
| 4 | `AccountsPayable` | الموردون (ذمم دائنة) | 1320 — الموردون |
| 5 | `Inventory` | المخزون | 1210 — المخزون |
| 6 | `CostOfGoodsSold` | تكلفة المبيعات | 5110 — تكلفة المبيعات |
| 7 | `SalesRevenue` | إيرادات المبيعات | 1520 — إيرادات المبيعات |
| 8 | `SalesReturns` | مردودات المبيعات | 1631 — مردودات مبيعات |
| 9 | `PurchaseReturns` | مردودات المشتريات | 1632 — مردودات مشتريات |
| 10 | `VatOutput` | ضريبة المبيعات (خرج) | 2510 — ضريبة المخرجات |
| 11 | `VatInput` | ضريبة المشتريات (دخل) | 2520 — ضريبة المدخلات |
| 12 | `Capital` | رأس المال | 3110 — رأس المال |
| 13 | `OpeningBalanceEquity` | الأرصدة الافتتاحية | 1422 — أرصدة افتتاحية |
| 14 | `RetainedEarnings` | الأرباح المحتجزة | 3120 — أرباح محتجزة |
| 15 | `UndistributedProfits` | أرباح وفاق (الإقفال السنوي) | 3130 — أرباح وفاق |
| 16 | `InventoryShortage` | عجز المخزون | 5120 — عجز مخزون |
| 17 | `InventorySurplus` | زيادة المخزون | 1710 — زيادة مخزون |
| 18 | `GeneralExpense` | مصروفات عمومية | 5210 — مصروفات عمومية |
| 19 | `SpoilageLoss` | هالك المخزون | 5130 — هالك مخزون |
| 20 | `EmployeeCustody` | عهد الموظفين | 1170 — عهد موظفين |
| 21 | `DeliveryChargesRevenue` | إيرادات التوصيل | 1533 — إيرادات توصيل |

### كيف يستخدمها AccountService.CreateAsync؟

في الخطوة 7 (الرصيد الافتتاحي)، يبحث النظام عن المفتاح `OpeningBalanceEquity`:

```csharp
var systemMapping = await _uow.SystemAccountMappings.FirstOrDefaultAsync(
    m => m.MappingKey == SystemAccountKey.OpeningBalanceEquity.ToString(), ct);
```

- `SystemAccountKey.OpeningBalanceEquity` هو `13`
- `.ToString()` يعطي `"OpeningBalanceEquity"`
- النظام يجد السجل الذي `MappingKey = "OpeningBalanceEquity"` ويقرأ `AccountId`
- هذا الـ `AccountId` (مثلاً `1422`) هو الحساب المقابل للأرصدة الافتتاحية
- يستخدمه في القيد المحاسبي

### كيف تستخدمها الخدمات الأخرى؟

مثال من `CreateSalesPostEntryAsync` (ترحيل فاتورة بيع):

```csharp
// البحث عن حساب إيرادات المبيعات
var salesRevenueMapping = await _uow.SystemAccountMappings
    .FirstOrDefaultAsync(m => m.MappingKey == "SalesRevenue", ct);

// البحث عن حساب ضريبة المخرجات
var vatOutputMapping = await _uow.SystemAccountMappings
    .FirstOrDefaultAsync(m => m.MappingKey == "VatOutput", ct);

// إنشاء قيد اليومية
je.AddCreditLine(salesRevenueMapping!.AccountId, subTotal, "إيرادات المبيعات");
je.AddCreditLine(vatOutputMapping!.AccountId, taxAmount, "ضريبة المبيعات");
```

#### مثال واقعي — ترحيل فاتورة بيع بقيمة 1,150 ريال (ضريبة 15%):

```
حساب إيرادات المبيعات (MappingKey = "SalesRevenue") → AccountId = 1520
حساب ضريبة المخرجات (MappingKey = "VatOutput") → AccountId = 2510

القيود:
    1130 — العملاء (المدين)             1,150 ريال (مدين)
        1520 — إيرادات المبيعات                     1,000 ريال (دائن)
        2510 — ضريبة المخرجات                          150 ريال (دائن)
```

لو غيرت `SystemAccountMappings` لتربط `SalesRevenue` بحساب `1521` بدلاً من `1520`،  
فإن **ترحيل الفواتير القادمة** ستستخدم `1521` تلقائياً — **بدون أي تعديل في الكود**.

---

## 📌 الفرق بين `Account.Create()` في Domain و `AccountService.CreateAsync()` في Application

| `Account.Create()` (Domain) | `AccountService.CreateAsync()` (Application) |
|---------------------------|----------------------------------------------|
| تتحقق من صحة البيانات (Guard Clauses) | تتحقق من صحة العلاقات (الأب موجود؟ نشط؟) |
| تنشئ الكيان فقط | تبحث عن تكرار الاسم |
| لا تعرف شيئاً عن قاعدة البيانات | تحسب المستوى (Level) تلقائياً |
| لا تعرف شيئاً عن SystemAccountMappings | تولد كود الحساب عبر `AccountCodeGeneratorService` |
| | تنشئ قيد اليومية للرصيد الافتتاحي |
| | تحفظ كل شيء في ترانزاكشن واحد |

---

## 🧪 أمثلة شاملة لسيناريوهات CreateAsync

### السيناريو 1: إنشاء حساب تفصيلي جديد تحت إيرادات المبيعات

**المدخلات:**
```csharp
var request = new CreateAccountRequest(
    NameAr: "مبيعات الجملة",
    NameEn: "Wholesale Sales",
    Nature: 4,          // Revenue
    IsLeaf: true,       // تفصيلي — يسمح بالحركات
    ParentId: 1520,     // تحت إيرادات المبيعات
    IsSystem: false,    // ليس نظامياً
    CategoryId: null,
    Description: "مبيعات الجملة للعملاء",
    Notes: null,
    OpeningBalance: null  // لا يوجد رصيد افتتاحي
);
```

**ما يحدث في الخلفية:**
1. ✅ الأب `1520` موجود ونشط وليس Leaf
2. ✅ لا يوجد حساب بنفس الاسم تحت `1520`
3. `Level` = الأب (3) + 1 = 4 (لأن `IsLeaf = true`)
4. كود الحساب المُوَلَّد: `15200001` (الابن الأول تحت `1520`)
5. اللون: `#4CAF50` (إيراد)
6. إنشاء الكيان → حفظ في قاعدة البيانات

### السيناريو 2: إنشاء حساب مع رصيد افتتاحي

```csharp
var request = new CreateAccountRequest(
    NameAr: "صندوق الرياض",
    NameEn: "Riyadh Cash Box",
    Nature: 1,           // Asset
    IsLeaf: true,        // تفصيلي
    ParentId: 1110,      // تحت النقدية
    IsSystem: false,
    CategoryId: null,
    Description: null,
    Notes: null,
    OpeningBalance: 50000  // رصيد افتتاحي بقيمة 50,000 ريال
);
```

**ما يحدث:**
1. ✅ كل التحققات السابقة
2. الرصيد الافتتاحي `50000` > 0 → يتم إنشاء قيد يومي:
```
    1111 — صندوق الرياض       50,000 ريال (مدين)
        1422 — الأرصدة الافتتاحية       50,000 ريال (دائن)
```
3. يتم حفظ الحساب + القيد في **ترانزاكشن واحد**

### السيناريو 3: خطأ — تكرار اسم تحت نفس الأب

```csharp
// يوجد بالفعل حساب "مبيعات التجزئة" تحت 1520
// محاولة إنشاء حساب آخر بنفس الاسم
var request = new CreateAccountRequest(
    NameAr: "مبيعات التجزئة",      // مكرر!
    ...
    ParentId: 1520
);
```

**النتيجة:** ❌ `"يوجد حساب بنفس الاسم 'مبيعات التجزئة' تحت نفس الحساب الأب"`

### السيناريو 4: خطأ — الأب غير موجود

```csharp
var request = new CreateAccountRequest(
    NameAr: "حساب وهمي",
    ParentId: 99999  // حساب غير موجود
);
```

**النتيجة:** ❌ `"الحساب الأب غير موجود"`

### السيناريو 5: خطأ — الأب تفصيلي (Leaf)

```csharp
// حساب 15200001 هو Leaf (تفصيلي)
var request = new CreateAccountRequest(
    NameAr: "حفيد",
    ParentId: 15200001  // هذا الحساب IsLeaf = true
);
```

**النتيجة:** ❌ `"لا يمكن إضافة حساب فرعي لحساب تفصيلي — الحساب الأب يجب أن يكون مجموعة"`

---

## 🎯 الخلاصة

- `AccountService.CreateAsync()` هي خدمة ذكية لا تقتصر على إضافة سطر في قاعدة البيانات
- تقوم بـ 7 خطوات متكاملة تشمل التحقق، التوليد التلقائي، وإنشاء القيود المحاسبية
- `SystemAccountMappings` تسمح بتغيير الحسابات المرتبطة بالوظائف المحاسبية دون تعديل الكود
- جميع العمليات الحساسة تُنفذ داخل `ExecuteTransactionAsync` لضمان التكامل (ACID)
- لكل حساب: **رمز فريد** + **لون** حسب طبيعته + **مستوى هرمي** + **رصيد افتتاحي** (اختياري)
