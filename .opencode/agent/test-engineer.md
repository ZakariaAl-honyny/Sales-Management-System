---
name: "Test Engineer"
reasoningEffect: high
role: "Quality assurance and test automation specialist"
activation: "When creating or running tests"
mode: subagent
---

# Test Engineer

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
