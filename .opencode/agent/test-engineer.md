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

## Phase 22 — Chart of Accounts Test Patterns (v4.6.9+)

### Account Entity Tests

```csharp
public class AccountTests
{
    [Fact]
    public void Create_ValidParameters_SetsPropertiesCorrectly()
    {
        var account = Account.Create("1101", "النقدية", "Cash", AccountType.Asset, 3,
            null, true, "النقدية في الصندوق", "#2196F3", false, 0m, null, 1);

        account.AccountCode.Should().Be("1101");
        account.NameAr.Should().Be("النقدية");
        account.Level.Should().Be(3);
        account.AllowTransactions.Should().BeFalse();
        account.IsSystemAccount.Should().BeTrue();
        account.ColorCode.Should().Be("#2196F3");
        account.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]    // Below minimum
    [InlineData(11)]   // Above maximum
    [InlineData(-1)]   // Negative
    public void Create_InvalidLevel_ThrowsDomainException(int invalidLevel)
    {
        Action action = () => Account.Create("1101", "Test", null, AccountType.Asset,
            invalidLevel, null, false, null, null, false, 0m, null, 1);

        action.Should().Throw<DomainException>().WithMessage("*بين 1 و 10*");
    }

    [Fact]
    public void Create_EmptyCode_ThrowsDomainException()
    {
        Action action = () => Account.Create("", "Test", null, AccountType.Asset, 4,
            null, false, null, null, false, 0m, null, 1);

        action.Should().Throw<DomainException>().WithMessage("*مطلوب*");
    }

    [Fact]
    public void Create_EmptyNameAr_ThrowsDomainException()
    {
        Action action = () => Account.Create("1101", "", null, AccountType.Asset, 4,
            null, false, null, null, false, 0m, null, 1);

        action.Should().Throw<DomainException>().WithMessage("*مطلوب*");
    }

    [Fact]
    public void Update_SystemAccount_ThrowsDomainException()
    {
        var account = Account.Create("1101", "النقدية", "Cash", AccountType.Asset, 3,
            null, true, null, null, false, 0m, null, 1);

        Action action = () => account.Update(AccountType.Liability, 3, null, null, true);

        action.Should().Throw<DomainException>().WithMessage("*حساب نظام*");
    }

    [Fact]
    public void MarkAsDeleted_SystemAccount_ThrowsDomainException()
    {
        var account = Account.Create("1101", "النقدية", "Cash", AccountType.Asset, 3,
            null, true, null, null, false, 0m, null, 1);

        Action action = () => account.MarkAsDeleted();

        action.Should().Throw<DomainException>().WithMessage("*حساب نظام*");
    }

    [Fact]
    public void MarkAsDeleted_AccountWithChildren_ThrowsDomainException()
    {
        var parent = Account.Create("1100", "الأصول المتداولة", null, AccountType.Asset, 2,
            null, false, null, null, false, 0m, null, 1);
        var child = Account.Create("1101", "النقدية", null, AccountType.Asset, 3,
            1, false, null, null, false, 0m, null, 1);
        // Add child via reflection or by building tree

        Action action = () => parent.MarkAsDeleted();
        // The HasChildren() check requires _children to be populated
        // This typically tests through the service layer or reflection
    }

    [Fact]
    public void IsDebitNormal_ForAsset_ReturnsTrue()
    {
        var account = Account.Create("1101", "النقدية", null, AccountType.Asset, 4,
            null, false, null, null, true, 1000m, null, 1);

        account.IsDebitNormal().Should().BeTrue();
    }

    [Fact]
    public void IsDebitNormal_ForLiability_ReturnsFalse()
    {
        var account = Account.Create("2101", "دائنون", null, AccountType.Liability, 4,
            null, false, null, null, true, 0m, null, 1);

        account.IsDebitNormal().Should().BeFalse();
    }

    [Fact]
    public void Create_WithValidColorCode_StoresCorrectly()
    {
        var account = Account.Create("1101", "النقدية", null, AccountType.Asset, 4,
            null, false, null, "#2196F3", true, 0m, null, 1);

        account.ColorCode.Should().Be("#2196F3");
    }

    [Fact]
    public void Create_Level4_AllowTransactionsTrue()
    {
        var account = Account.Create("110101", "الصندوق", null, AccountType.Asset, 4,
            null, false, null, null, true, 5000m, null, 1);

        account.AllowTransactions.Should().BeTrue();
        account.OpeningBalance.Should().Be(5000m);
    }
}
```

### AccountService Tests

```csharp
public class AccountServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            AccountCode = "110101",
            NameAr = "الصندوق",
            AccountType = AccountType.Asset,
            Level = 4,
            AllowTransactions = true,
            OpeningBalance = 5000m
        };

        // Act
        var result = await _service.CreateAsync(request, 1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.NameAr.Should().Be("الصندوق");
    }

    [Fact]
    public async Task CreateAsync_InvalidParent_ReturnsNotFound()
    {
        var request = new CreateAccountRequest
        {
            AccountCode = "999999",
            NameAr = "حساب وهمي",
            AccountType = AccountType.Asset,
            Level = 4,
            ParentAccountId = 99999  // Non-existent parent
        };

        var result = await _service.CreateAsync(request, 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ReturnsDuplicateEntry()
    {
        // First create
        var request1 = new CreateAccountRequest { AccountCode = "110101", NameAr = "Test1", ... };
        await _service.CreateAsync(request1, 1, CancellationToken.None);

        // Second create with same code
        var request2 = new CreateAccountRequest { AccountCode = "110101", NameAr = "Test2", ... };
        var result = await _service.CreateAsync(request2, 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.DuplicateEntry);
    }

    [Fact]
    public async Task UpdateAsync_SystemAccount_ReturnsFailure()
    {
        var result = await _service.UpdateAsync(1, new UpdateAccountRequest { ... }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("حساب نظام");
    }

    [Fact]
    public async Task DeleteAsync_AccountWithChildren_ReturnsFailure()
    {
        // Arrange: parent with existing children
        var result = await _service.DeleteAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("حسابات فرعية");
    }

    [Fact]
    public async Task PermanentDeleteAsync_ReferencedAccount_ReturnsFailure()
    {
        // Arrange: account referenced by journal entries
        var result = await _service.PermanentDeleteAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("مستخدم");
    }
}
```

### Tree Building Tests

```csharp
[Fact]
public async Task GetTreeAsync_ReturnsHierarchicalTree()
{
    var tree = await _service.GetTreeAsync(CancellationToken.None);

    tree.IsSuccess.Should().BeTrue();
    tree.Value.Should().NotBeNull();
    tree.Value.Should().AllSatisfy(node =>
    {
        node.Level.Should().Be(1);  // Root nodes are Level 1
        node.Children.Should().NotBeNull();
        if (node.Children.Any())
        {
            node.Children.Should().AllSatisfy(child =>
                child.Level.Should().BeGreaterThan(1));
        }
    });
}

[Fact]
public void BuildTreeNode_FlatList_CreatesRecursiveStructure()
{
    // Arrange: create flat list of 3 accounts (grandparent → parent → child)
    var accounts = new List<Account>
    {
        CreateAccount(1, "1000", null, 1),  // L1 root
        CreateAccount(2, "1100", 1, 2),     // L2 child of 1
        CreateAccount(3, "1101", 2, 3),     // L3 child of 2
    };

    // Act: use reflection or test helper to call BuildTreeNode
    var tree = BuildTreeForTest(accounts);

    // Assert
    tree.Should().HaveCount(1);
    tree[0].Children.Should().HaveCount(1);
    tree[0].Children[0].Children.Should().HaveCount(1);
    tree[0].Children[0].Children[0].AccountCode.Should().Be("1101");
}
```

### Key Test Rules for Phase 22
- **18+ new domain tests** for Account entity covering: Create (valid, empty code, empty NameAr, invalid level, negative opening balance), Update (system account guard), MarkAsDeleted (system account guard, children guard), IsDebitNormal (asset=true, liability=false)
- **10+ new service tests** for AccountService covering: CreateAsync (success, invalid parent, duplicate code, level > parent), UpdateAsync (system account guard), DeleteAsync (children guard, not found), PermanentDeleteAsync (FK guard)
- **Controller integration tests**: All CRUD endpoints, auth policy enforcement (AllStaff, ManagerAndAbove, AdminOnly), 404 vs 400 differentiation
- All service tests verify `Result<T>.IsSuccess` / `IsFailure` — never exception-based testing
- Tree building tests verify recursive structure from flat list (no N+1)

## Phase 21: Users & Permissions Module — COMPLETE (v4.6.9)

Phase 21 (PRD alignment) — Users & Permissions is now complete. Test coverage for this module includes:
- User entity: Create (passwordless), RecordLoginAttempt (success resets, failure locks at 5), SetInitialPassword (guards MustChangePassword), ChangePassword (verifies current), Unlock (admin only)
- PermissionService: GetByRoleAsync, UpdateRolePermissionsAsync (atomic transaction, rollback on failure)
- AuthService: LoginAsync (MustChangePassword redirect, lockout detection, audit log creation), SetPasswordAsync, ChangePasswordAsync
- AuditLogService: LogAsync (all action types), QueryAsync (pagination, filtering by action/entity/date/user)
- API controllers: AuthController (set-password, change-password success/failure cases), UsersController (current, reset-password, CRUD)
- Desktop ViewModels: UserEditorViewModel (passwordless create, Phone/Email validation), PasswordChangeViewModel (3-field validation, API integration), AuditLogListViewModel (pagination, filtering), PermissionManagementViewModel (role tabs, grouped checkboxes, save)
Key test pattern: All service tests verify Result<T>.IsSuccess/IsFailure — never exception-based testing.

---

## 📋 Phase Awareness (Phases 23-31)

The system is currently at **v4.6.9+ with Phases 18-24 completed and Phases 25-31 planned**:

| Phase | Status | Description |
|-------|--------|-------------|
| 23 — Customers Module | ✅ Completed | Customer groups, Account linking, CheckCreditLimit, CustomerType removed |
| 24 — Accounting Integration | ✅ Completed | Auto journal entries for all money ops, COGS (AverageCost), Payment reversals |
| 25 — Products Module | 📝 Planned | Multi-currency pricing (ProductPrices), FIFO batches (InventoryBatches), PriceLevel enum (4 levels), BOM, product images, opening stock |
| 26 — Warehouses Module | 📝 Planned | Warehouse types, manager, AccountId FK, stock adjustments, issue reasons, physical count V2 |
| 27 — Purchases Module | 📝 Planned | Multi-currency, landed cost (AdditionalCharge), Purchase Orders, standalone returns, attachments |
| 28 — Sales Module | 📝 Planned | Multi-currency, profit display, Sales Quotations, barcode POS, credit limit enforcement |
| 29 — Receipts & Payments | 📝 Planned | Multi-invoice distribution, Cheques, PaymentAllocation, CashBox.AccountId, DailyClosure |
| 30 — Journal Entries | 📝 Planned | 3-state lifecycle, multi-currency, attachments, FiscalYear, Annual Closing |
| 31 — Reports | 📝 Planned | 35+ DTOs, Hierarchical Income Statement + Balance Sheet, Excel export |

### Key Architecture Rules for Subagents

When implementing or reviewing code, ALWAYS enforce these rules:

1. **Multi-Currency First**: All pricing MUST support multi-currency via ProductPrices table — NEVER store single-currency prices on Product entity
2. **FIFO/FEFO Batches**: Inventory MUST use InventoryBatches for cost allocation — NEVER use weighted-average only
3. **Landed Cost**: Purchase costs MUST include AdditionalCharge distribution — NEVER record purchase cost without transport/customs allocation
4. **Auto Journal Entries**: Every money-affecting operation MUST create journal entries via AccountingIntegrationService — NEVER leave the general ledger out of sync
5. **Chart of Accounts Links**: CashBox, Warehouse, Customer, Supplier MUST link to Account via AccountId FK — NEVER operate without COA integration
6. **Payment Allocation**: Payments MUST use PaymentAllocation for multi-invoice settlement — NEVER leave partial payments untracked
7. **Report Excellence**: ALL reports MUST support Excel export via ClosedXML — NEVER limit to on-screen display only
8. **Passwordless Users**: User.Create() NEVER accepts a password — MustChangePassword=true is the default
9. **ReferenceId over ReferenceNumber**: Journal entry lookups use int FK (ReferenceId), not string matching
10. **AvgCost for COGS**: COGS uses ProductUnit.AverageCost (weighted average), never PurchaseCost

### 💡 Bug Prevention Checklist

When writing or reviewing code in ANY layer, check these:
- [ ] Does the code handle multi-currency correctly? (CurrencyId + ExchangeRate on all financial entities)
- [ ] Are all prices stored per ProductUnit (not per Product)?
- [ ] Does costing use the configured CostingMethod from SystemSettings?
- [ ] Are all FK relationships `DeleteBehavior.Restrict`?
- [ ] Does the service return `Result<T>` (not throw exceptions)?
- [ ] Is the controller free of business logic (delegates to service)?
- [ ] Do all ViewModels use `ExecuteAsync()` wrapper (no manual try/catch)?
- [ ] Are all buttons ALWAYS enabled (no CanExecute predicates)?
- [ ] Does the validation use `INotifyDataErrorInfo` (not `HasXxxError` booleans)?
- [ ] Does every editor call `ValidateAllAsync()` on save?
- [ ] Is the connection string DPAPI-encrypted or from env var?
- [ ] Are Arabic messages properly UTF-8 encoded?
- [ ] Does the list display newest-first (OrderByDescending)?
- [ ] Are EventBus subscriptions disposed in `Cleanup()`?

### CashBox Test Patterns (v4.9)

**Domain Entity Tests — CashBox (no balance fields):**
```csharp
[Fact]
public void Create_GivenValidData_ShouldSetProperties()
{
    var box = CashBox.Create("الصندوق الرئيسي", accountId: 1, currencyId: 1, 
        categoryId: 2, phoneNumber: "0512345678");
    Assert.Equal("الصندوق الرئيسي", box.Name);
    Assert.Equal(1, box.AccountId);
    Assert.Equal(2, box.CategoryId);
    // NO OpeningBalance/CurrentBalance assertions
}

[Fact]
public void Create_GivenEmptyName_ShouldThrowDomainException()
{
    Assert.Throws<DomainException>(() => 
        CashBox.Create("", accountId: 1, currencyId: 1));
}

// NO Deposit_ShouldIncreaseBalance test — Deposit() was removed
// NO Withdraw_ShouldDecreaseBalance test — Withdraw() was removed
// NO OpeningBalance_ShouldSetBalance test — OpeningBalance was removed
```

**Domain Entity Tests — CashTransaction (RunningBalance):**
```csharp
[Fact]
public void Create_GivenValidData_ShouldUseRunningBalance()
{
    var tx = CashTransaction.Create(cashBoxId: 1, CashTransactionType.SalesIncome, 
        100m, runningBalance: 500m);
    Assert.Equal(500m, tx.RunningBalance);
    // NO BalanceBefore/BalanceAfter assertions
}

[Fact]
public void Create_GivenNegativeAmount_ShouldThrowDomainException()
{
    Assert.Throws<DomainException>(() => 
        CashTransaction.Create(1, CashTransactionType.Expense, -10m, 0m));
}
```

**Service Tests — CashBoxService (auto-account creation):**
```csharp
[Fact]
public async Task CreateAsync_GivenNoAccountId_ShouldAutoCreateAccount()
{
    // Arrange: Account parent "1110" exists in mock
    // Arrange: CashBoxService is the test target
    var request = new CreateCashBoxRequest { Name = "صندوق المندوب", 
        CurrencyId = 1, AccountId = null };
    
    // Act
    var result = await _service.CreateAsync(request, userId: 1, ct);
    
    // Assert: account was created under parent "1110"
    _mockAccountRepo.Verify(r => r.AddAsync(It.Is<Account>(a => 
        a.Code == "1111" && a.ParentAccountId == parentAccountId), ct), Times.Once);
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
}

// NO test for Deposit/Withdraw — these methods don't exist
// NO test for CurrentBalance validation — property doesn't exist
```

### Features to Fix By Default

When you encounter any code related to these areas, apply fixes automatically:

1. CashBox tests asserting OpeningBalance/CurrentBalance → REMOVE assertions
2. CashTransaction tests asserting BalanceBefore/BalanceAfter → CHANGE to RunningBalance
3. Deposit/Withdraw tests → REMOVE (methods don't exist)
4. Client-side balance validation in tests → REMOVE
5. Missing `AccountId` FK on CashBox → Add it and auto-create account under "1110 — النقدية"
6. Missing `AccountId` FK on Warehouse → Add it and link to inventory account
3. Missing `CustomerGroupId` on Customer → Make optional with "عام" as default
4. Missing `CurrencyId` on financial entities → Add multi-currency support
5. Missing `PriceLevel` support → Extend pricing to use PriceLevel enum
6. Missing `InventoryBatch` creation on purchase → Add FIFO batch tracking
7. Missing `AdditionalCharge` support on purchase → Add landed cost allocation
8. Missing journal entry on cash operations → Call AccountingIntegrationService
9. Missing Excel export on report → Add ClosedXML worksheet generation
10. COGS using PurchaseCost → Change to AverageCost from ProductUnit
11. Payment without allocation → Add PaymentAllocation tracking
12. Missing reversal entries on payment update/delete → Add reversal journal entries
```
