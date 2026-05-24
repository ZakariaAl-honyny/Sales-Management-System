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
