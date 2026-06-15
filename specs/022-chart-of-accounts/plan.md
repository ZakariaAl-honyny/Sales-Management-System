# Phase 22 — Chart of Accounts Module: Implementation Plan

> **Version**: 2.0 — Aligned with V1 database-schema.md (AccountCategories + Accounts)
> **Scope**: Full Chart of Accounts CRUD with 4-level hierarchy, ~65 seeded accounts, Desktop dual-mode UI (TreeView + DataGrid), integration points for Journal Entries, CashBoxes, Customers, Suppliers.

---

## 1. Summary

The Chart of Accounts (دليل الحسابات) is the backbone of the accounting subsystem. It defines a self-referencing hierarchical tree of accounts organized by Nature (Asset, Liability, Equity, Revenue, Expense). Every financial transaction posts to leaf-level detail accounts. This module provides CRUD for accounts, hierarchical tree building, system account protection, and Desktop UI with both TreeView and flat DataGrid modes.

---

## 2. Key Entities

### 2.1 AccountCategories (فئات الحسابات)

| Field | Type | Notes |
|-------|------|-------|
| Id | smallint PK | Small lookup table |
| Name | nvarchar(100) not null | Arabic name |
| Description | nvarchar(300) nullable | Optional description |

Used to group accounts by functional category (e.g., "نقدية", "بنوك", "عملاء"). Lightweight categorization separate from the hierarchy. Seeded with 5–10 categories covering cash, banks, customers, suppliers, inventory, fixed assets, taxes, capital, revenues, expenses.

### 2.2 Accounts (الحسابات)

| Field | Type | Notes |
|-------|------|-------|
| Id | int PK | Auto-increment |
| ParentId | int? FK → self | Self-referencing hierarchy, Restrict delete |
| AccountCode | nvarchar(20) not null | Unique filtered `[IsActive]=1` |
| Name | nvarchar(200) not null | Arabic name |
| Nature | tinyint not null | 1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense |
| IsLeaf | bit not null default 1 | True for detail accounts that allow transactions |
| IsSystem | bit not null default 0 | System accounts protected from modification/deletion |
| CategoryId | smallint? FK → AccountCategories | Optional categorization |
| IsActive | bit not null default 1 | Soft delete flag |

**Inherited fields**: CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId.

**Design rationale**:
- **No Level field**: Depth is derived dynamically from the ParentId chain. The DB enforces no strict level numbering — any depth is allowed. UI calculates level by traversing up the parent chain (max practical depth is ~10).
- **IsLeaf replaces AllowTransactions**: When IsLeaf=true, the account accepts journal entry postings. Parent accounts (IsLeaf=false) are grouping nodes only.
- **Nature replaces AccountType enum**: Simple tinyint 1–5 matching standard accounting classification.
- **IsSystem scoped to L1–L2**: Only Group (Level 1) and Main (Level 2) accounts carry IsSystem=true in seed data. Sub and Detail accounts are user-modifiable.

---

## 3. Business Rules

### 3.1 AccountCode Rules

- Must be a string of 4–10 digits (`^\d{4,10}$`)
- Unique across active accounts (filtered unique index `WHERE [IsActive]=1`)
- Level 1 (Group) accounts: exactly 3 digits (e.g., `100`, `200`, `300`, `400`, `500`)
- Level 2+ (Main/Sub/Detail): 4–10 digits
- Hierarchical convention: child codes inherit parent's leading digits (e.g., parent `1100` → child `1110` → grandchild `1111`)
- Once created, AccountCode is read-only (no editing — breaks integrity of cross-references)

### 3.2 Hierarchy Rules

- A child account's depth must exceed its parent's depth (enforced in service, not in DB)
- Leaf accounts (IsLeaf=true) must have no children
- Parent accounts (IsLeaf=false) must have at least one child or be seed grouping nodes
- A parent account cannot be soft-deleted while it has active children — service checks `AnyAsync(a => a.ParentAccountId == id)` before allowing deletion

### 3.3 System Account Protection

- IsSystem=true accounts are **immutable**: Update() throws DomainException, MarkAsDeleted() throws DomainException
- Only Level 1 (Group) and Level 2 (Main) accounts seeded as IsSystem=true
- Level 3 (Sub) and Level 4 (Detail) accounts are NOT system accounts — users can modify descriptions, names, or color codes
- System accounts cannot be permanently deleted even with DbUpdateException fallback — service returns Result.Failure at the guard check

### 3.4 Delete Strategy

- **Soft delete** (default): Sets IsActive=false. Guarded: cannot soft-delete IsSystem accounts, cannot soft-delete accounts with active children.
- **Permanent delete**: Only available for non-system accounts with no children and no journal entry references. The service catches `DbUpdateException` and returns a friendly Arabic error if the account is referenced by JournalEntryLines, CashBoxes, Customers, Suppliers, Banks, or Expenses.

### 3.5 Unique AccountCode per Active Account

Filtered unique index: `CREATE UNIQUE INDEX IX_Accounts_AccountCode ON Accounts(AccountCode) WHERE IsActive = 1`. Soft-deleted accounts keep their code but don't block reuse.

---

## 4. Seed Data

### 4.1 AccountCategories

5–10 categories seeded: نقدية, بنوك, عملاء, موردون, مخزون, أصول ثابتة, ضرائب, رأس مال, إيرادات, مصروفات.

### 4.2 Account Hierarchy (~65 Accounts)

**5 Root Groups (Level 1, IsSystem=true, IsLeaf=false):**

| Code | Name (Ar) | Nature | Color |
|------|-----------|--------|-------|
| 100 | الأصول | Asset (1) | Blue `#2196F3` |
| 200 | الخصوم | Liability (2) | Red `#F44336` |
| 300 | حقوق الملكية | Equity (3) | Green `#4CAF50` |
| 400 | الإيرادات | Revenue (4) | Green `#4CAF50` |
| 500 | المصروفات | Expense (5) | Orange `#FF9800` |

**8 Level 2 Main groups (IsSystem=true, IsLeaf=false)** under each root — e.g., under 100: `110` أصول متداولة, `120` أصول ثابتة, `130` أصول غير ملموسة.

**~20 Level 3 Sub groups (IsSystem=false, IsLeaf=false)** — e.g., under 110: `111` الصناديق, `112` البنوك, `113` العملاء, `114` المخزون.

**~32 Level 4 Detail accounts (IsSystem=false, IsLeaf=true)** — leaf accounts where transactions post. Examples: `1111` الصندوق, `1121` البنك الأهلي, `1131` عميل نقدي, `1141` بضاعة أول المدة.

**Two-pass seed approach:**
1. Pass 1: Create all Level 1 accounts → `SaveChangesAsync()` → query back to get DB-generated IDs
2. Pass 2: Create Level 2 accounts with ParentId from Pass 1 → `SaveChangesAsync()` → query IDs
3. Repeat for Level 3 and Level 4

This avoids the chicken-and-egg problem of self-referencing FKs without temporary IDs.

### 4.3 SystemAccountMappings Update

After seeding accounts, update SystemAccountMappings to point to correct new account IDs for: Cash, Bank, AR, AP, VAT Output, VAT Input, Capital, Sales Revenue, COGS, Inventory, etc.

---

## 5. Design Decisions

### 5.1 No Strict Level Enforcement in DB

The database-schema.md defines no `Level` column — hierarchy depth is derived from the `ParentId` chain. The Domain entity may expose a computed `Level` property (walk up parents, count depth), but the DB stores only the parent reference. This gives flexibility for future restructuring. Service-level validation ensures child depth > parent depth.

### 5.2 IsLeaf Replaces AllowTransactions + Level Combination

The database schema uses a single `IsLeaf` bit to indicate transaction-allowed accounts. This is simpler than storing both `Level` (int) and `AllowTransactions` (bool). The Domain entity may add a `Level` computed property for UI convenience, but the persistence model follows the DB schema exactly.

### 5.3 Color Coding via Service, Not DB

Colors are assigned by Nature in the service layer when building DTOs: Asset=Blue, Liability=Red, Equity=Green, Revenue=Green, Expense=Orange. The DB schema has no `ColorCode` column — colors are a UI concern computed from Nature at runtime.

### 5.4 Tree Building from Flat List (No N+1)

`AccountService.GetTreeAsync()` loads all active accounts in a single query, builds an in-memory dictionary keyed by `ParentId`, then recursively assembles the tree. This avoids the N+1 problem of querying children per parent. Algorithm:
1. Query `_uow.Accounts.GetAllAsync()` — single round trip
2. Group by `ParentId` into `Dictionary<int?, List<Account>>`
3. Build root nodes (ParentId == null) and recursively attach children from the dictionary

### 5.5 Dual-Mode Desktop UI

AccountsListView supports two display modes toggleable by toolbar button:
- **TreeView mode** (default for accountants): `HierarchicalDataTemplate` with expand/collapse, indentation by depth, color-coded by Nature, bold for parent nodes
- **DataGrid mode** (default for casual users): Flat table with columns (Code, Name, Nature, Depth, IsLeaf, IsSystem), searchable and sortable

Both modes use the same underlying `ObservableCollection<AccountTreeNodeDto>`.

### 5.6 AccountCode Read-Only After Creation

AccountCode is set once at creation and is read-only on edit. This prevents integrity breaks when the code is used as a cross-reference by users, reports, or external systems. The Edit form displays AccountCode as a read-only TextBox.

### 5.7 Auth Policies on API Endpoints

| Endpoint | Policy | Rationale |
|----------|--------|-----------|
| GET /api/v1/accounts | AllStaff | Read access for all roles |
| GET /api/v1/accounts/{id} | AllStaff | Read individual account |
| GET /api/v1/accounts/tree | AllStaff | Read hierarchical tree |
| GET /api/v1/accounts/type/{type} | AllStaff | Filter by Nature |
| POST /api/v1/accounts | ManagerAndAbove | Create accounts |
| PUT /api/v1/accounts/{id} | ManagerAndAbove | Update accounts |
| DELETE /api/v1/accounts/{id} | ManagerAndAbove | Soft delete |
| DELETE /api/v1/accounts/permanent/{id} | AdminOnly | Permanent delete |

404 vs 400 differentiation: service returns `ErrorCodes.NotFound` for missing entities (→ 404), business validation errors return 400.

---

## 6. Implementation Tasks

### Task 1 — Domain Entity Updates
- Expand `Account.cs` (if already exists) or create from scratch
- Add `ParentId` (int? FK self), `AccountCode` (string), `Name` (string), `Nature` (byte), `IsLeaf` (bool), `IsSystem` (bool), `CategoryId` (smallint? FK)
- Add domain methods: `Create()`, `Update()`, `MarkAsDeleted()`, `HasChildren()`, `Activate()`
- Create `AccountCategory.cs` entity (smallint PK, Name, Description)
- Guard clauses: IsSystem protection in Update/MarkAsDeleted, children guard in delete

### Task 2 — EF Core Configurations
- `AccountConfiguration.cs`: Fluent API with all columns, self-referencing FK with Restrict, filtered unique index on AccountCode (`WHERE [IsActive]=1`), query filter `IsActive == true`, enum conversion on Nature
- `AccountCategoryConfiguration.cs`: smallint PK, max lengths, query filter
- Migration: CREATE TABLE + ALTER scripts

### Task 3 — AccountingSeeder Rewrite
- Two-pass seed of ~65 accounts across 4 levels
- 5 Level 1 roots (100/200/300/400/500) with IsSystem=true
- 8 Level 2 mains with IsSystem=true
- 20 Level 3 subs with IsSystem=false
- 32 Level 4 details with IsLeaf=true
- Color codes by Nature, Arabic explanations on all seeded accounts
- Seed 8–10 AccountCategories
- Update SystemAccountMappings with new IDs

### Task 4 — Contracts: DTOs + Requests + Validators
- `AccountDto`: 13+ fields including computed `NatureDisplay`, `Depth`
- `AccountTreeNodeDto`: recursive `Children` list for TreeView
- `CreateAccountRequest`: AccountCode, Name, Nature, ParentId, CategoryId, IsLeaf, IsSystem, Explanation
- `UpdateAccountRequest`: Name, Nature, ParentId, CategoryId, IsLeaf, Explanation (no AccountCode)
- FluentValidators: AccountCode regex `^\d{4,10}$`, Level-1 code max 3 chars, Nature 1–5, Explanation MaxLength(500)

### Task 5 — Application Service: IAccountService + AccountService
- `GetTreeAsync()`: single-query flat→tree builder with dictionary approach
- `GetAllAsync()`, `GetByIdAsync()`, `GetByTypeAsync()`: standard lookups
- `CreateAsync()`: validates parent exists/level deeper, unique code check, returns Result<AccountDto>
- `UpdateAsync()`: IsSystem guard
- `DeleteAsync()`: soft delete with parent children check via `AnyAsync`
- `PermanentDeleteAsync()`: DbUpdateException catch → Arabic error

### Task 6 — API Controller: AccountsController
- 7–8 endpoints matching auth policies from §5.7
- Pure controller: injects IAccountService only, no DbContext
- 404 vs 400 differentiation based on ErrorCodes
- Route constraints: `{id:int}`, `{type:int:min(1):max(5)}`

### Task 7 — Desktop API Service (IAccountApiService + AccountApiService)
- HTTP client with typed methods matching controller endpoints
- Error handling: content-type guard before JSON parse (RULE-184)
- Base URL from DI configuration

### Task 8 — Desktop ViewModels
- `AccountsListViewModel`: ObservableCollection, two display modes (tree/grid), search/filter, IDisposable with EventBus subscription
- `AccountEditorViewModel`: INotifyDataErrorInfo, ValidateAllAsync(), dual constructor (parameterless for add, parameterized for edit), AccountCode read-only in edit mode
- `AccountChangedMessage` EventBus message
- ToolTips on all buttons per RULE-185–190

### Task 9 — Desktop Views (XAML)
- `AccountsListView.xaml`: Dual-mode layout — TreeView with HierarchicalDataTemplate on left toggle, DataGrid on right toggle, toolbar with add/edit/delete/refresh
- `AccountEditorView.xaml`: Form with AccountCode (read-only on edit), Name, Nature dropdown, ParentId picker, IsLeaf checkbox, CategoryId picker, Explanation field
- No `ModernTextBox` on ComboBox elements — use `ModernComboBox` style
- No `DisplayMemberPath` + `ItemTemplate` on same ComboBox
- Compact styles per RULE-262–274

### Task 10 — Desktop DI Registration + Navigation
- Register in `ServiceCollectionExtensions`: IAccountApiService, AccountApiService, AccountsListViewModel, AccountEditorViewModel
- Add sidebar navigation entry in MainWindow: "دليل الحسابات" under المحاسبة section

---

## 7. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **AccountCode unique constraint on soft-delete** | Soft-deleted accounts block reuse of code | Filtered index `WHERE [IsActive]=1` solves this |
| **Self-referencing FK deadlock on seeding** | Cannot seed children before parents exist | Two-pass approach: parents first, SaveChanges, then children |
| **Deep hierarchy performance** | Recursive tree building on 1000+ accounts | Single-query + in-memory dictionary — O(n), no N+1 |
| **Permanent delete FK violation** | Unhandled DbUpdateException crashes API | Catch in PermanentDeleteAsync, return Arabic error |
| **System account modification** | User changes critical seed accounts | IsSystem guard in both Update() and MarkAsDeleted() |
| **Parent deletion with children** | Orphaned child accounts left in tree | Service-level `AnyAsync` check before soft delete |
| **Duplicate codes across soft-deletes** | Filtered unique index not covering IsActive | Verified: filter `WHERE IsActive=1` allows duplicates in inactive |
| **IsLeaf changes breaking journal entries** | User marks transaction-used account as non-leaf | Add service guard: if JournalEntryLines exist, reject IsLeaf=false |

---

## 8. Cross-Module Integration Points

| Module | Integration | Direction |
|--------|------------|-----------|
| Journal Entries (Phase 30) | JournalEntryLines.AccountId FK → Accounts.Id | Accounts must exist before journal entries post |
| CashBoxes (Phase 29) | CashBox.AccountId FK → Accounts.Id | CashBox auto-creates detail account under 1110 parent |
| Customers (Phase 23) | Customer.AccountId FK → Accounts.Id | Service auto-creates detail account under 1130 (AR) parent |
| Suppliers (Phase 32) | Supplier.AccountId FK → Accounts.Id | Service auto-creates detail account under 1320 (AP) parent |
| Banks (Phase 29) | Bank.AccountId FK → Accounts.Id | Bank links to account under 1120 parent |
| Expenses (Phase 29) | Expense.AccountId FK → Accounts.Id | Posts to expense detail accounts |
| Receipt/Payment Vouchers | Voucher.AccountId FK → Accounts.Id | Vouchers must select an account |
| SystemAccountMappings | Maps accounting system keys to specific account IDs | Updated during seed to point to correct IDs |
| Reports (Phase 31) | Account balances drive Trial Balance, Income Statement, Balance Sheet | Flat tree query groups by Nature for financial reports |
