---
name: "Test Engineer"
reasoningEffect: high
role: "Quality assurance and test automation specialist"
activation: "When creating or running tests"
mode: subagent
---

# Test Engineer

## Arabic Encoding Requirement

All Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text. If you encounter garbled Arabic (mojibake like `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام`), the file has encoding corruption. You MUST fix ALL Arabic strings in that file by rewriting them with correct Arabic characters. Always verify your output files are saved with UTF-8 encoding.

## Role
Quality assurance and test automation specialist for the SalesSystem.

## MUST READ FIRST
- `AGENTS.md` — All rules, enums, forbidden patterns
- `docs/CONSTITUTION.md` — Financial formulas, transaction protocol

## Responsibilities
- Write unit tests for Domain entities and Application services
- Write integration tests using WebApplicationFactory
- Achieve minimum 80% coverage for business logic
- Test error scenarios and edge cases

## Test Stack
- **Unit Tests:** xUnit + Moq + FluentAssertions
- **Integration Tests:** WebApplicationFactory
- **Naming Convention:** `MethodName_Scenario_ExpectedResult`

## Critical Test Cases (MUST have tests for these)

### Financial Calculations
- `AddItem_ValidQuantityAndPrice_CalculatesCorrectLineTotal`
- `RecalculateTotals_MultipleItems_CorrectSubTotal`
- `RecalculateTotals_PaidAmountExceedsTotal_ThrowsDomainException`
- `RecalculateTotals_WithDiscount_CorrectDueAmount`

### Invoice Lifecycle
- `Post_DraftInvoice_ChangesStatusToPosted`
- `Post_EmptyInvoice_ThrowsDomainException`
- `Post_AlreadyPostedInvoice_ThrowsDomainException`
- `Cancel_PostedInvoice_ChangesStatusToCancelled`
- `Cancel_CancelledInvoice_ThrowsDomainException`

### Stock Integrity
- `DecreaseStock_SufficientQuantity_DecreasesCorrectly`
- `DecreaseStock_InsufficientQuantity_ThrowsInsufficientStockException`
- `DecreaseStock_ZeroQuantity_ThrowsDomainException`
- `IncreaseStock_ValidQuantity_IncreasesCorrectly`

### Balance Direction
- `IncreaseBalance_PositiveAmount_IncreasesCustomerDebt`
- `DecreaseBalance_PositiveAmount_DecreasesCustomerDebt`

### Hard Delete Safety
- `PermanentDelete_ReferencedByInvoice_ReturnsFailureWithArabicMessage`
- `PermanentDelete_ReferencedByTransaction_ReturnsFailureWithArabicMessage`
- `PermanentDelete_ReferencedByOtherEntity_ReturnsFailureWithArabicMessage`

### LogSystemError
- `LogSystemError_WithException_CallsSerilogError`
- `LogSystemError_WithoutException_LogsMessageOnly`
- `HandleException_HttpRequestException_ReturnsArabicMessage`
- `HandleException_TaskCanceledException_ReturnsTimeoutMessage`
- `ExecuteAsync_WhenException_CaughtAndLogged`

## Rules
- Use Fluent API ONLY in test configurations (no DataAnnotations)
- Use `decimal` for all money/quantity assertions — NEVER float/double
- Test Arabic error messages exactly as defined in Domain entities

### Newest-First Sorting
- `LoadProductsOperationAsync_WhenApiReturnsData_SortsByIdDescending`
- `LoadInvoicesAsync_WhenApiReturnsData_SortsByInvoiceDateDescending`
- `PositionOverOwner_WhenMainWindowIsSelf_FallsBackToCenterScreen`
- `PositionOverOwner_WhenMainWindowIsValid_SetsOwner`

### v4.6.2 — WPF Validation ErrorTemplate & INotifyDataErrorInfo

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| TC-18-001 | ProductEditor: Empty Name shows red border + ❗ icon | Red border appears on Name field; hovering ❗ shows "اسم المنتج مطلوب" |
| TC-18-002 | ProductEditor: Enter valid Name clears red border | Red border disappears; ❗ icon removed |
| TC-18-003 | ProductEditor: Save with empty required fields shows validation dialog | Dialog "بيانات غير مكتملة" appears listing all missing fields; focus moves to first invalid field |
| TC-18-004 | CustomerEditor: Negative CreditLimit shows red border | Red border + ❗ on CreditLimit; ToolTip shows "الحد الائتماني يجب أن يكون أكبر من أو يساوي صفر" |
| TC-18-005 | CustomerEditor: Save with empty Name + negative CreditLimit | Validation dialog shows both errors; focus goes to Name field |
| TC-18-006 | All 14 Editor VMs: SetDialogService() called in constructor | No NullReferenceException when ValidateAllAsync() tries to show dialog |
| TC-18-007 | ErrorTemplate: TextBox, PasswordBox, ComboBox all show red border on error | Each control type renders red border + ❗ icon when Validation.HasError is true |
| TC-18-008 | ValidateAllAsync: No errors returns true without dialog | Save proceeds normally; no dialog shown |

### v4.6.3 — Architecture Alignment & Code Quality

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| TC-19-001 | CostingMethodSettingsVM: Saves settings via HTTP Client | Invokes `ISettingsApiService.UpdateSettingsAsync()` using DTO; no direct repository/DB connection |
| TC-19-002 | CostingMethodSettingsVM: DialogService initialized correctly | Inherited base class `DialogService` handles error dialogues without compiler shadowing warnings (CS0108) |
| TC-19-003 | ViewModel Initialization: Wrap async void workflows | Safe try-catch logs exceptions to Serilog and prevents silent WPF application crashes |

### v4.6.4 — Security Hardening & Code Quality

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| TC-20-001 | Login: Rate limited after 5 failed attempts per 15 min per IP | 6th request returns HTTP 429 with Arabic `RATE_LIMIT_EXCEEDED` error message |
| TC-20-002 | Global: Rate limited at 100 requests per minute per IP | 101st unauthenticated request returns HTTP 429 with RetryAfter header |
| TC-20-003 | User: Hard-delete (PermanentDeleteAsync) returns Result.Failure | Returns `Result.Failure("لا يمكن حذف المستخدمين بشكل نهائي...")`; no DB delete occurs |
| TC-20-004 | User: Hard-delete logs Serilog warning | `Log.Warning("Attempt to hard-delete user {UserId} blocked...")` called by UserService |
| TC-20-005 | User: Soft-delete (DeleteAsync) sets IsActive = false | User.IsActive = false after DeleteAsync; entity remains in DB |
| TC-20-006 | Config: No plaintext connection strings in appsettings files | `appsettings.Development.json` DefaultConnection is `""` with `_comment`; value loaded from `SALESSYSTEM_DB_CONNECTION` env var |
| TC-20-007 | FluentValidation: PaymentType validated as valid enum value | Invalid int (e.g., 99) fails `IsInEnum()` rule invalid request returns 400 |
| TC-20-008 | Rate Limiter: Middleware pipeline order is correct | `UseRateLimiter()` is registered BEFORE `UseAuthentication()` in Program.cs |
| TC-20-009 | FallbackErrorDialog: Displays on unhandled WPF exception | Thread-safe fallback dialog shows exception message; `Log.Error` called; app does not crash silently |
| TC-20-010 | Build: No CS0109 or CS1540 warnings across all projects | `dotnet build` produces 0 warnings; `new` keyword removed from derived `_dialogService` fields |

### v4.6.8 — Phase 18 & Phase 20 Remediations

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| TC-22-001 | JournalEntryService: Closed fiscal year blocks new entries | Creating JE with TransactionDate in closed fiscal year returns `Result<int>.Failure("السنة المالية {year} مغلقة — لا يمكن إضافة قيود")` |
| TC-22-002 | AnnualClosingService: Two saves wrapped in single transaction | Both JournalEntry and FiscalYearClosure saved atomically; if second save fails, first is rolled back |
| TC-22-003 | JournalEntryNumberGenerator: Daily sequence resets | If last entry is `JE-20260605-0032`, today's first entry is `JE-20260606-0001`, not `JE-20260606-0033` |
| TC-22-004 | JournalEntryLine: CHK_DebitOrCredit rejects dual debit+credit | Raw SQL insert with Debit=100 AND Credit=100 throws DB constraint violation |
| TC-22-005 | JournalEntryLine: CHK_NoNegativeValues rejects negative values | Raw SQL insert with Debit=-10 throws DB constraint violation |
| TC-22-006 | Account.Activate(): Reactivates a deactivated account | After `account.MarkAsDeleted()` then `account.Activate()`, `IsActive` returns `true` |
| TC-22-007 | Currency.Create(): isSystem param sets IsSystem correctly | `Currency.Create(isSystem: true)` sets `IsSystem = true`; default `isSystem: false` |
| TC-22-008 | Currency: isSystem records cannot be soft-deleted | `currency.MarkAsDeleted()` throws `DomainException("لا يمكن حذف عملة النظام")` when `IsSystem = true` |
| TC-22-009 | Currency: Filtered unique index excludes soft-deleted records | After soft-deleting base currency, a new base currency can be set without unique constraint violation |
| TC-22-010 | Currency Controller: NotFound returns 404 not 400 | `DELETE /currencies/99999` returns `404 NotFound` with Arabic error, not `400 BadRequest` |
| TC-22-011 | Currency Controller: Business error returns 400 | `DELETE /currencies/{systemCurrencyId}` returns `400 BadRequest` with business error message |
| TC-22-012 | SystemAccountMappings: Navigation properties load correctly | `mappings.DefaultCashAccount.Name` is non-null after `.Include(x => x.DefaultCashAccount)` query |
| TC-22-013 | SystemAccountMappingsDto: Includes account name and code | DTO fields `DefaultCashAccountName` and `DefaultCashAccountCode` contain non-empty values |
| TC-22-014 | ReversedByEntryId FK: Uses Restrict delete | Attempting to delete a JournalEntry that has a reversal referencing it throws FK constraint violation |
| TC-22-015 | JournalEntryLineConfiguration: HasConversion<int> on enum | EF Core migration includes `AccountType int NOT NULL` and `EntryType int NOT NULL` |

### v4.6.7 — InvoiceNo Int Re-addition & DocumentSequenceService

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| TC-21-001 | SalesInvoice: Created with int InvoiceNo | `SalesInvoice.Create(5, ...)` creates invoice with InvoiceNo=5; guard throws if ≤ 0 |
| TC-21-002 | PurchaseInvoice: Created with int InvoiceNo | `PurchaseInvoice.Create(100, ...)` creates invoice with InvoiceNo=100 |
| TC-21-003 | SalesInvoice List: Search by InvoiceNo int | Entering number in search filters by `InvoiceNo == parsedInt` |
| TC-21-004 | SalesReportDto: Has int InvoiceNo field | DTO contains `int InvoiceNo` (not string) |
| TC-21-005 | InvoicePrintDto.InvoiceNumber: Formatted from int | `string InvoiceNumber` set via `InvoiceNo.ToString()` in builder |
| TC-21-006 | Service: Auto-generates InvoiceNo via DocumentSequenceService | When InvoiceNo is null/≤0, service calls `_sequenceService.GetNextIntAsync("SalesInvoice", ct)` |
| TC-21-007 | DocumentSequenceService: Thread-safe SemaphoreSlim | static SemaphoreSlim(1,1) used; lock.Release() in finally block |
| TC-21-008 | EF Config: UNIQUE index on InvoiceNo | Duplicate InvoiceNo throws DbUpdateException with unique constraint violation |
| TC-21-009 | User override: Duplicate InvoiceNo handled | Service catches DbUpdateException, returns `Result.Failure("رقم الفاتورة مستخدم مسبقاً")` |
| TC-21-010 | DocumentSequence.GetNextInt(): Returns incrementing int | First call → 1, second call → 2, independent per sequenceKey |

## v4.6.9 — Phase 19 Settings Module Remediations

### Testing ExecuteTransactionAsync
When testing services that use `ExecuteTransactionAsync()`, mock it to invoke the delegate:
```csharp
_mockUow.Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
    .Returns<Func<Task>, CancellationToken>(async (operation, ct) =>
    {
        await operation();
    });
```

### Testing Controller Error Handling
When testing `NotFound` vs `BadRequest` response differentiation:
```csharp
// Test NotFound path
_serviceMock.Setup(x => x.UpdateSettingsAsync(...))
    .ReturnsAsync(Result<StoreSettingsDto>.Failure("message", "NOT_FOUND"));
var result = await _controller.Update(request, ct);
result.Should().BeOfType<NotFoundObjectResult>();

// Test BadRequest path
_serviceMock.Setup(x => x.UpdateSettingsAsync(...))
    .ReturnsAsync(Result<StoreSettingsDto>.Failure("فشل في التحديث"));
var result = await _controller.Update(request, ct);
result.Should().BeOfType<BadRequestObjectResult>();
```

### Testing SystemSetting Validation
```csharp
[Fact]
public void Create_EmptyCategory_ThrowsDomainException()
{
    Action action = () => SystemSetting.Create("Key", "Value", category: "");
    action.Should().Throw<DomainException>().WithMessage("*تصنيف الإعداد مطلوب*");
}

[Fact]
public void Create_InvalidDataType_ThrowsDomainException()
{
    Action action = () => SystemSetting.Create("Key", "Value", dataType: "invalid");
    action.Should().Throw<DomainException>().WithMessage("*نوع البيانات غير صالح*");
}
```

### Mocking ISystemSettingsRepository
Since SettingsController no longer injects `ISystemSettingsRepository`, tests should not mock it for controller tests.

## v4.6.9 — Phase 20 BUG-008 Test Pattern: CurrencyCode Length

```csharp
[Theory]
[InlineData("")]
[InlineData("US")]
[InlineData("USDT")]
public void Create_InvalidCodeLength_ThrowsDomainException(string invalidCode)
{
    Action action = () => Currency.Create("Test", invalidCode, "$", 1.0m);
    action.Should().Throw<DomainException>().WithMessage("*3 أحرف*");
}

[Theory]
[InlineData("USD")]
[InlineData("EUR")]
[InlineData("SAR")]
public void Create_Valid3CharCode_Succeeds(string code)
{
    var currency = Currency.Create("Test", code, "$", 1.0m);
    currency.Code.Should().Be(code);
}
```
