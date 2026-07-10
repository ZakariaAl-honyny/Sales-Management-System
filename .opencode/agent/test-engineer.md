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

The system is currently at **v4.10.1+ with Phases 18-24 completed and Phases 25-31 in progress**:

| Phase | Status | Description |
|-------|--------|-------------|
| 23 — Customers Module | ✅ Completed | Parties-based (Party entity), no CustomerGroup/SupplierType, Account auto-created under 1210/2100, no balance fields on Customer/Supplier |
| 24 — Accounting Integration | ✅ Completed | Auto journal entries, COGS (AverageCost), Payment reversals, per-entity account routing |
| 25 — Products Module | ✅ Completed | ProductPrices (per unit×currency×effective dates), Units independent table (smallint PK), ProductUnit with Factor/IsBaseUnit, InventoryBatches (FIFO), Perpetual Inventory, product images, opening stock |
| 26 — Warehouses Module | 📝 Planned | WarehouseTransfer/WarehouseTransferLine (replaces StockTransfer), InventoryTransaction/InventoryTransactionLine (replaces InventoryMovement), warehouse types, AccountId FK |
| 27 — Purchases Module | 🟡 Partial — OtherCharges Landed Cost, Purchase Return Standalone ✅ | Multi-currency, landed cost via OtherCharges (AdditionalCharge), Purchase Orders, standalone returns (+ OtherCharges Landed Cost, Purchase Return Standalone) |
| 28 — Sales Module | 🟡 Partial — Price Enforcement, DeliveryChargesRevenue, Flexible Input ✅ | Multi-currency, profit display, Sales Quotations, barcode POS, credit limit enforcement, Price Enforcement, DeliveryChargesRevenue, Flexible Input |
| 29 — Receipts & Payments | 🟡 Partial — CashBox ✅ | CashBox refactored (no balance fields, AccountId FK, RunningBalance); Cheques, PaymentAllocation, DailyClosure planned |
| 30 — Journal Entries | 📝 Planned | 3-state lifecycle, multi-currency (CurrencyId + ExchangeRate), attachments, FiscalYear, Annual Closing |
| 31 — Reports | 📝 Planned | 35+ DTOs, Hierarchical Income Statement + Balance Sheet, Excel export via ClosedXML |

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
- [ ] Are all prices stored per ProductUnit × Currency (ProductPrices table) — NOT per Product?
- [ ] Are inventory batches tracked via InventoryBatches (FIFO/FEFO) — NOT just weighted average?
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
- [ ] Are lookup table PKs `smallint` (not `int`)? (Units, Roles, Departments, Currencies, etc.)
- [ ] Is AuditLog PK `long` (bigint) for high-volume data?
- [ ] Are filtered unique indexes on soft-deletable entities using `.HasFilter("[IsActive] = 1")`?
- [ ] Is `Currency.IsBaseCurrency` immutable (no user-facing toggle after creation)?
- [ ] Is `IsSystem` flag protecting seed data on Units, Currencies, Permissions?
- [ ] Are all Customer/Supplier entities missing CustomerGroup/SupplierType (deferred to V2)?
- [ ] Are there no OpeningBalance/CurrentBalance/CurrencyId fields on Customer/Supplier?
- [ ] Does every Customer/Supplier have a mandatory PartyId + AccountId?
- [ ] Is the AccountId auto-created under 1210 (customers) or 2100 (suppliers)?
- [ ] Is Perpetual Inventory enforced (no Purchases account — Dr Inventory directly)?
- [ ] Do purchase returns credit PurchaseReturnAccountId (not Inventory)?
- [ ] Are InventoryTransactions used instead of InventoryMovements?
- [ ] Are WarehouseTransfers used instead of StockTransfers?
- [ ] Do all InventoryTransaction/WarehouseTransfer tests verify both forward and reversal entries?

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
    // Arrange: Account parent "1101" exists in mock
    // Arrange: CashBoxService is the test target
    var request = new CreateCashBoxRequest { Name = "صندوق المندوب", 
        CurrencyId = 1, AccountId = null };
    
    // Act
    var result = await _service.CreateAsync(request, userId: 1, ct);
    
    // Assert: account was created under parent "1101"
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
4. Client-side balance validation tests → REMOVE
5. Missing `AccountId` FK on CashBox → Add it and auto-create account under "1101 — النقدية صناديق"
6. Missing `AccountId` FK on Warehouse → Add it and link to inventory account
7. `CustomerGroupId` on Customer → REMOVE (deferred to V2 — payment type is per-invoice)
8. `CustomerType` / `SupplierType` on Customer/Supplier → REMOVE (not in V1)
9. `OpeningBalance`, `CurrentBalance`, `CurrencyId` on Customer/Supplier → REMOVE (balance on linked Account)
10. User-supplied `AccountId` on Customer/Supplier requests → REMOVE (auto-created by service under 1210/2100)
11. Missing `PartyId` FK on Customer/Supplier → ADD Party entity for shared contact data
12. Missing `ProductPrices` table → ADD ProductPrice entity (ProductUnitId + CurrencyId + Price + EffectiveFrom/To)
13. Missing `InventoryBatches` table on purchase → ADD InventoryBatch (ProductId, WarehouseId, BatchNo, ExpiryDate, QuantityRemaining, UnitCost)
14. Missing `AdditionalCharge` support on purchase → ADD AdditionalCharge entity for landed cost distribution
15. `PriceLevel` enum references → REMOVE (not in V1; pricing is per ProductUnit × Currency)
16. `RetailPrice` / `WholesalePrice` / `PurchasePrice` / `AvgCost` on Product → REMOVE (use ProductPrices + InventoryBatches)
17. `InventoryMovement` references → REPLACE with `InventoryTransaction` / `InventoryTransactionLine`
18. `StockTransfer` / `StockTransferItem` references → REPLACE with `WarehouseTransfer` / `WarehouseTransferLine`
19. Missing `Factor` / `IsBaseUnit` on ProductUnit → ADD for unit conversion logic
20. `decimal(18,4)` for money → CHANGE to `decimal(18,2)`
21. `int` PK for `AuditLog` → CHANGE to `long` PK via `.HasColumnType("bigint")`
22. Lookup tables (Units, Roles, Departments, Currencies) → CHANGE PK to `smallint` via `.HasColumnType("smallint")`
23. Missing filtered unique index on soft-deletable entities → ADD `.HasFilter("[IsActive] = 1")`
24. Missing `CurrencyId` on financial entities → ADD multi-currency support with `decimal(18,2)` ExchangeRate
25. Missing journal entry on cash operations → Call AccountingIntegrationService
26. Missing Excel export on report → ADD ClosedXML worksheet generation
27. COGS using PurchaseCost → CHANGE to AverageCost from ProductUnit
28. Payment without allocation → ADD PaymentAllocation tracking
29. Missing reversal entries on payment update/delete → ADD reversal journal entries
30. Perpetual Inventory violation (using Purchases account) → CHANGE to Dr Inventory directly
31. Purchase return crediting InventoryAssetAccountId → CHANGE to credit PurchaseReturnAccountId
32. `IsBaseCurrency` mutable after creation → CHANGE to immutable (locked at setup)
33. Missing `IsSystem` flag protection on Units/Currencies → ADD guard on MarkAsDeleted()

## New Entity Test Patterns (Phases 25-31)

### ProductPrices Entity Tests

```csharp
[Fact]
public void Create_ValidData_SetsPropertiesCorrectly()
{
    var price = ProductPrice.Create(productUnitId: 1, currencyId: 1, 25.50m,
        effectiveFrom: new DateTime(2026, 1, 1));
    price.ProductUnitId.Should().Be(1);
    price.CurrencyId.Should().Be(1);
    price.Price.Should().Be(25.50m);
    price.EffectiveFrom.Should().Be(new DateTime(2026, 1, 1));
    price.EffectiveTo.Should().BeNull();
}

[Fact]
public void Create_NegativePrice_ThrowsDomainException()
{
    Action action = () => ProductPrice.Create(1, 1, -10m, DateTime.UtcNow);
    action.Should().Throw<DomainException>().WithMessage("*السعر لا يمكن أن يكون سالباً*");
}

[Fact]
public void IsEffective_WithinRange_ReturnsTrue()
{
    var price = ProductPrice.Create(1, 1, 25m, new DateTime(2026, 1, 1));
    var date = new DateTime(2026, 6, 1);
    price.IsEffective(date).Should().BeTrue();
}

[Fact]
public void IsEffective_AfterEffectiveTo_ReturnsFalse()
{
    var price = ProductPrice.Create(1, 1, 25m,
        new DateTime(2026, 1, 1), effectiveTo: new DateTime(2026, 3, 1));
    price.IsEffective(new DateTime(2026, 6, 1)).Should().BeFalse();
}
```

### InventoryBatch Entity Tests (FIFO/FEFO)

```csharp
[Fact]
public void Deduct_ValidQuantity_DecreasesQuantityRemaining()
{
    var batch = InventoryBatch.Create(1, 1, "BATCH-001", quantityReceived: 100m,
        unitCost: 15m, expiryDate: new DateTime(2027, 1, 1));
    batch.Deduct(30m);
    batch.QuantityRemaining.Should().Be(70m);
}

[Fact]
public void Deduct_ExceedsRemaining_ThrowsDomainException()
{
    var batch = InventoryBatch.Create(1, 1, "BATCH-001", quantityReceived: 10m, unitCost: 15m);
    Action action = () => batch.Deduct(20m);
    action.Should().Throw<DomainException>().WithMessage("*الكمية المتاحة*");
}

[Fact]
public void Deduct_ZeroQuantity_ThrowsDomainException()
{
    var batch = InventoryBatch.Create(1, 1, "BATCH-001", quantityReceived: 10m, unitCost: 15m);
    Action action = () => batch.Deduct(0m);
    action.Should().Throw<DomainException>().WithMessage("*أكبر من الصفر*");
}

[Fact]
public void IsExpired_ExpiryDatePassed_ReturnsTrue()
{
    var batch = InventoryBatch.Create(1, 1, "BATCH-001", 100m, 15m,
        expiryDate: new DateTime(2025, 1, 1));
    batch.IsExpired(new DateTime(2026, 6, 1)).Should().BeTrue();
}
```

### Unit Entity Tests (smallint PK, IsSystem)

```csharp
[Fact]
public void Create_ValidData_SetsProperties()
{
    var unit = Unit.Create("حبة", "PCS");
    unit.Name.Should().Be("حبة");
    unit.Symbol.Should().Be("PCS");
    unit.IsSystem.Should().BeFalse();
    unit.IsActive.Should().BeTrue();
}

[Fact]
public void MarkAsDeleted_SystemUnit_ThrowsDomainException()
{
    var unit = Unit.Create("حبة", "PCS", isSystem: true);
    Action action = () => unit.MarkAsDeleted();
    action.Should().Throw<DomainException>().WithMessage("*وحدة نظام*");
}

[Fact]
public void MarkAsDeleted_UserUnit_SetsInactive()
{
    var unit = Unit.Create("كرتون", "CTN", isSystem: false);
    unit.MarkAsDeleted();
    unit.IsActive.Should().BeFalse();
}
```

### ProductUnit Entity Tests (Factor, IsBaseUnit)

```csharp
[Fact]
public void Create_BaseUnit_FactorIsOne()
{
    var pu = ProductUnit.Create(productId: 1, unitId: 1, factor: 1m, isBaseUnit: true);
    pu.Factor.Should().Be(1m);
    pu.IsBaseUnit.Should().BeTrue();
}

[Fact]
public void Create_DerivedUnit_FactorGreaterThanOne()
{
    var pu = ProductUnit.Create(productId: 1, unitId: 2, factor: 24m, isBaseUnit: false);
    pu.Factor.Should().Be(24m);
}

[Fact]
public void ConvertToUnit_BaseToDerived_CorrectConversion()
{
    var basePu = ProductUnit.Create(1, 1, factor: 1m, isBaseUnit: true);
    var derivedPu = ProductUnit.Create(1, 2, factor: 24m, isBaseUnit: false);
    var result = basePu.ConvertToUnit(48m, derivedPu.Factor);
    result.Should().Be(2m); // 48 base units = 2 cartons (48/24)
}

[Fact]
public void ConvertToUnit_DerivedToBase_CorrectConversion()
{
    var basePu = ProductUnit.Create(1, 1, factor: 1m, isBaseUnit: true);
    var derivedPu = ProductUnit.Create(1, 2, factor: 24m, isBaseUnit: false);
    var result = derivedPu.ConvertToUnit(3m, basePu.Factor);
    result.Should().Be(72m); // 3 cartons = 72 base units (3*24)
}
```

### WarehouseTransfer / WarehouseTransferLine Entity Tests

```csharp
[Fact]
public void Create_ValidData_SetsProperties()
{
    var transfer = WarehouseTransfer.Create(1, 2, createdByUserId: 1);
    transfer.SourceWarehouseId.Should().Be(1);
    transfer.DestinationWarehouseId.Should().Be(2);
    transfer.Status.Should().Be(WarehouseTransferStatus.Draft);
}

[Fact]
public void AddLine_ValidItem_AddsToLines()
{
    var transfer = WarehouseTransfer.Create(1, 2, createdByUserId: 1);
    transfer.AddLine(productUnitId: 1, quantity: 10m, batchNo: "B001");
    transfer.Lines.Should().HaveCount(1);
    transfer.Lines[0].Quantity.Should().Be(10m);
}

[Fact]
public void Post_EmptyTransfer_ThrowsDomainException()
{
    var transfer = WarehouseTransfer.Create(1, 2, createdByUserId: 1);
    Action action = () => transfer.Post();
    action.Should().Throw<DomainException>().WithMessage("*أصناف النقل*");
}

[Fact]
public void Cancel_PostedTransfer_ReversesStock()
{
    var transfer = WarehouseTransfer.Create(1, 2, createdByUserId: 1);
    transfer.AddLine(1, 10m, "B001");
    transfer.Post();
    transfer.Cancel();
    transfer.Status.Should().Be(WarehouseTransferStatus.Cancelled);
}
```

### InventoryTransaction / InventoryTransactionLine Entity Tests

```csharp
[Fact]
public void Create_ValidTransaction_SetsProperties()
{
    var tx = InventoryTransaction.Create(warehouseId: 1,
        referenceType: "SalesInvoice", referenceId: 100, notes: "فاتورة بيع");
    tx.WarehouseId.Should().Be(1);
    tx.ReferenceType.Should().Be("SalesInvoice");
    tx.ReferenceId.Should().Be(100);
}

[Fact]
public void AddLine_ValidItem_AddsTransactionLine()
{
    var tx = InventoryTransaction.Create(1, "PurchaseInvoice", 50);
    tx.AddLine(productUnitId: 1, quantity: 20m, unitCost: 15m, batchNo: "B001");
    tx.Lines.Should().HaveCount(1);
    tx.Lines[0].Quantity.Should().Be(20m);
    tx.Lines[0].UnitCost.Should().Be(15m);
}

[Fact]
public void AddLine_NegativeQuantity_ThrowsDomainException()
{
    var tx = InventoryTransaction.Create(1, "Adjustment", null);
    Action action = () => tx.AddLine(1, -5m, 10m, null);
    action.Should().Throw<DomainException>().WithMessage("*أكبر من الصفر*");
}
```

### Party Entity Tests (shared contact data for Customer/Supplier)

```csharp
[Fact]
public void Create_ValidData_SetsProperties()
{
    var party = Party.Create("محمد أحمد", "mohamed@example.com", "0512345678",
        "صنعاء", "1234567890", categoryId: null);
    party.Name.Should().Be("محمد أحمد");
    party.Email.Should().Be("mohamed@example.com");
}

[Fact]
public void Create_EmptyName_ThrowsDomainException()
{
    Action action = () => Party.Create("", null, null, null, null, null);
    action.Should().Throw<DomainException>().WithMessage("*الاسم مطلوب*");
}

[Fact]
public void Update_ValidData_UpdatesFields()
{
    var party = Party.Create("محمد", "old@email.com", "0512345678", null, null, null);
    party.Update("محمد أحمد", "new@email.com", "0598765432", "عدن", "9876543210", null);
    party.Name.Should().Be("محمد أحمد");
    party.Phone.Should().Be("0598765432");
}
```

### Customer Entity Tests (with PartyId, no balance/currency/CustomerGroup)

```csharp
[Fact]
public void Create_ValidData_SetsProperties()
{
    var customer = Customer.Create(partyId: 1, accountId: 10, creditLimit: 50000m,
        createdByUserId: 1);
    customer.PartyId.Should().Be(1);
    customer.AccountId.Should().Be(10);
    customer.CreditLimit.Should().Be(50000m);
    customer.IsActive.Should().BeTrue();
    // NO OpeningBalance, CurrentBalance, CurrencyId, CustomerGroupId assertions
}

[Fact]
public void Create_ZeroCreditLimit_SetsZero()
{
    var customer = Customer.Create(1, 10, creditLimit: 0m, createdByUserId: 1);
    customer.CreditLimit.Should().Be(0m);
}

[Fact]
public void CheckCreditLimit_WithinLimit_ReturnsTrue()
{
    var customer = Customer.Create(1, 10, creditLimit: 50000m, createdByUserId: 1);
    // Account balance is 30000, additionalAmount is 10000 → total would be 40000 < 50000
    customer.CheckCreditLimit(additionalAmount: 10000m, currentBalance: 30000m).Should().BeTrue();
}

[Fact]
public void CheckCreditLimit_ExceedsLimit_ReturnsFalse()
{
    var customer = Customer.Create(1, 10, creditLimit: 50000m, createdByUserId: 1);
    customer.CheckCreditLimit(additionalAmount: 30000m, currentBalance: 40000m).Should().BeFalse();
}
```

### Supplier Entity Tests (with PartyId, no balance/currency/SupplierType)

```csharp
[Fact]
public void Create_ValidData_SetsProperties()
{
    var supplier = Supplier.Create(partyId: 1, accountId: 20, creditLimit: 100000m,
        createdByUserId: 1);
    supplier.PartyId.Should().Be(1);
    supplier.AccountId.Should().Be(20);
    // NO OpeningBalance, CurrentBalance, CurrencyId, SupplierType assertions
}

[Fact]
public void CheckCreditLimit_WithinLimit_ReturnsTrue()
{
    var supplier = Supplier.Create(1, 20, creditLimit: 100000m, createdByUserId: 1);
    supplier.CheckCreditLimit(additionalAmount: 50000m, currentBalance: 30000m).Should().BeTrue();
}

[Fact]
public void CheckCreditLimit_ExceedsLimit_ReturnsFalse()
{
    var supplier = Supplier.Create(1, 20, creditLimit: 100000m, createdByUserId: 1);
    supplier.CheckCreditLimit(additionalAmount: 100000m, currentBalance: 50000m).Should().BeFalse();
}
```

### CustomerService Account Auto-Creation Tests

```csharp
[Fact]
public async Task CreateAsync_GivenValidRequest_ShouldAutoCreateAccount()
{
    // Arrange: parent account "1210 — العملاء" exists in mock
    var request = new CreateCustomerRequest { Name = "عميل جديد", Phone = "0512345678" };
    
    // Act
    var result = await _customerService.CreateAsync(request, userId: 1, ct);
    
    // Assert: account was created under parent "1210"
    _mockAccountRepo.Verify(r => r.AddAsync(It.Is<Account>(a =>
        a.ParentAccountId == expectedParentId), ct), Times.Once);
    result.IsSuccess.Should().BeTrue();
}

[Fact]
public async Task CreateAsync_GivenValidRequest_ShouldNotAcceptAccountId()
{
    // AccountId is NOT in CreateCustomerRequest — auto-created by service
    var requestType = typeof(CreateCustomerRequest);
    requestType.GetProperty("AccountId").Should().BeNull();
}
```

### Smallint PK Test Pattern (Lookup Tables)

```csharp
[Fact]
public void EntityConfiguration_SmallintPk_IsConfiguredCorrectly()
{
    // Verify using EF Core model snapshot or reflection
    var entityType = typeof(Unit);
    var keyProperty = entityType.GetProperty("Id");
    keyProperty?.PropertyType.Should().Be(typeof(short)); // smallint = short in C#
}

// In EF Core configuration tests:
[Fact]
public void UnitConfiguration_IdColumn_HasSmallintType()
{
    var builder = new EntityTypeBuilder<Unit>(new EntityTypeBuilder<Unit>());
    builder.Property(x => x.Id).HasColumnType("smallint");
    // Verify migration SQL uses smallint, not int
}
```

### Bigint PK Test Pattern (AuditLog)

```csharp
[Fact]
public void AuditLog_Create_SetsLongId()
{
    var log = AuditLog.Create(userId: 1, AuditAction.LoginSuccess,
        entityType: "User", entityId: "5");
    // Id is 0 until saved to DB, but property type is long
    log.Id.Should().Be(0);
    // Property should be long (bigint)
    typeof(AuditLog).GetProperty("Id")?.PropertyType.Should().Be(typeof(long));
}

[Fact]
public void EntityConfiguration_BigintPk_IsConfiguredCorrectly()
{
    var entityType = typeof(AuditLog);
    var keyProperty = entityType.GetProperty("Id");
    keyProperty?.PropertyType.Should().Be(typeof(long));
}
```

### Currency Immutability Tests

```csharp
[Fact]
public void Currency_Create_IsSystemFalseByDefault()
{
    var currency = Currency.Create("ريال يمني", "YER", "﷼", 1.0m);
    currency.IsSystem.Should().BeFalse();
}

[Fact]
public void Currency_Create_IsSystemTrue_ForSeedData()
{
    var currency = Currency.Create("ريال يمني", "YER", "﷼", 1.0m, isSystem: true);
    currency.IsSystem.Should().BeTrue();
}

[Fact]
public void Currency_Create_IsBaseCurrencyInitialFalse()
{
    var currency = Currency.Create("ريال يمني", "YER", "﷼", 1.0m);
    currency.IsBaseCurrency.Should().BeFalse();
}

[Fact]
public void SetAsBaseCurrency_SystemOnly_SetsIsBaseCurrency()
{
    var currency = Currency.Create("ريال يمني", "YER", "﷼", 1.0m);
    currency.SetAsBaseCurrency();  // System-only at setup
    currency.IsBaseCurrency.Should().BeTrue();
}

// NO test for UnsetBaseCurrency — IsBaseCurrency is immutable after creation
// Filtered unique index ensures only one active base currency
```

### Filtered Unique Index Tests

```csharp
[Fact]
public async Task CurrencyRepository_SoftDeletedBaseCurrency_AllowsNewBaseCurrency()
{
    // Arrange: existing base currency (Id=1) is soft-deleted
    var currency1 = Currency.Create("قديم", "OLD", "ⵣ", 0.5m, isSystem: false);
    currency1.SetAsBaseCurrency();
    await _repo.AddAsync(currency1, ct);
    await _uow.SaveChangesAsync(ct);
    
    // Soft-delete it
    currency1.MarkAsDeleted();
    await _uow.SaveChangesAsync(ct);
    
    // Act: create new base currency
    var currency2 = Currency.Create("جديد", "NEW", "N", 2.0m, isSystem: false);
    currency2.SetAsBaseCurrency();
    
    // Assert: no unique constraint violation because filter excludes deleted
    var act = async () =>
    {
        await _repo.AddAsync(currency2, ct);
        await _uow.SaveChangesAsync(ct);
    };
    await act.Should().NotThrowAsync();  // Filtered unique index allows it
}
```

### Perpetual Inventory Test Pattern

```csharp
[Fact]
public async Task PurchaseInvoicePost_NoPurchasesAccount_DrInventoryDirectly()
{
    // Purchase invoice posting must use Dr Inventory / Cr AP
    // NOT Dr Purchases / Cr AP (prevents reconciliation issues)
    var invoice = await CreateAndPostPurchaseInvoiceAsync();
    var entries = await _journalEntryRepo.GetByReferenceAsync("PurchaseInvoice", invoice.Id);
    
    // Verify debit goes to Inventory account, not Purchases account
    entries.Lines.Should().Contain(l =>
        l.AccountId == inventoryAccountId && l.Debit > 0);
    entries.Lines.Should().NotContain(l =>
        l.AccountId == purchasesAccountId);  // No Purchases account in Perpetual
}

[Fact]
public async Task PurchaseReturnPost_CreditsPurchaseReturnAccount()
{
    // Purchase return must credit PurchaseReturnAccountId, NOT Inventory account
    var returnInvoice = await CreateAndPostPurchaseReturnAsync();
    var entries = await _journalEntryRepo.GetByReferenceAsync("PurchaseReturn", returnInvoice.Id);
    
    entries.Lines.Should().Contain(l =>
        l.AccountId == purchaseReturnAccountId && l.Credit > 0);
    entries.Lines.Should().NotContain(l =>
        l.AccountId == inventoryAssetAccountId && l.Credit > 0);
}
```

### Key Test Count Targets for Phases 25-31
- **45+ domain entity tests**: ProductPrices (6), InventoryBatches (5), Unit (3), ProductUnit (5), WarehouseTransfer (5), InventoryTransaction (3), Party (3), Customer/Supplier w/Parties (8), Currency immutable+IsSystem (5)
- **30+ service tests**: ProductPriceService (4), ProductUnitService (3), WarehouseTransferService (5), InventoryTransactionService (4), CustomerService with Party+Account (6), SupplierService with Party+Account (4), CurrencyService immutability (2), CostAllocationService (3)
- **15+ integration tests**: API endpoints for all new entities, controller auth enforcement, 404 vs 400 differentiation
- **All tests verify `Result<T>.IsSuccess` / `IsFailure`** — never exception-based testing for service layer
- Smallint FK constraints verified via `DbUpdateException` on FK violation
- Bigint PK verified via `long` type assertion and migration SQL inspection
- Filtered unique indexes verified via soft-delete-create cycle (no constraint violation)
```
