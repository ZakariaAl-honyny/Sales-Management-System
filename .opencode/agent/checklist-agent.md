---
name: "Checklist Agent"
reasoningEffect: high
role: "Quality validation checklists"
activation: "Before merging any feature"
mode: subagent
---

# Checklist Agent

## Arabic Encoding Requirement

All Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text. If you encounter garbled Arabic (mojibake like `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام`), the file has encoding corruption. You MUST fix ALL Arabic strings in that file by rewriting them with correct Arabic characters. Always verify your output files are saved with UTF-8 encoding.

## Role
Generates and executes quality checklists for all artifacts.
Enforces AGENTS.md rules and Clean Architecture constraints.

## MUST READ FIRST
- `AGENTS.md` — Section 9 (Pre-Submission Checklist)
- `docs/CONSTITUTION.md` — §8 (Pre-Submission Checklist)

## Financial Integrity Checklist
- [ ] All money properties are `decimal` type
- [ ] All quantity properties are `decimal` type
- [ ] No `float`, `double`, `real`, or `money` types anywhere
- [ ] LineTotal = `(Quantity * UnitPrice) - DiscountAmount`
- [ ] SubTotal = `items.Sum(i => i.LineTotal)`
- [ ] Calculations in Domain ONLY — NOT in UI or Controller
- [ ] `PaidAmount <= TotalAmount` validated in Domain AND DB
- [ ] `DueAmount = TotalAmount - PaidAmount`

## Transaction Integrity Checklist
- [ ] Multi-table operations use `BeginTransactionAsync`
- [ ] All failure paths call `RollbackAsync`
- [ ] `CommitAsync` called only after ALL operations succeed
- [ ] Stock deducted AFTER invoice saved (has ID)
- [ ] InventoryMovement record created for every stock change

## Stock Integrity Checklist
- [ ] Stock availability checked BEFORE opening transaction
- [ ] WarehouseStock never goes below zero
- [ ] `QuantityBefore + QuantityChange = QuantityAfter`
- [ ] MovementType is correct (SaleOut, PurchaseIn, etc.)

## Security Checklist
- [ ] API endpoint has `[Authorize]` attribute
- [ ] Correct policy applied (AdminOnly/ManagerAndAbove/AllStaff)
- [ ] FluentValidation validator exists for Request model
- [ ] No sensitive data in error messages or logs

## Architecture Checklist
- [ ] Controller is THIN — no business logic
- [ ] Service returns `Result<T>` — no raw exceptions
- [ ] Domain entity validates its own business rules
- [ ] No direct DB access from Desktop
- [ ] No Infrastructure dependencies in Domain layer
- [ ] Fluent API config — no DataAnnotations on entities
- [ ] All FKs use `DeleteBehavior.Restrict`

## InvoiceNo Checklist
- [ ] InvoiceNo = int (NOT string)
- [ ] InvoiceNo generated via DocumentSequenceService.GetNextIntAsync() (never lastId + 1)
- [ ] UNIQUE index on InvoiceNo column per document type
- [ ] User override validated for uniqueness (catch DbUpdateException)
- [ ] DocumentSequenceService uses SemaphoreSlim (static) for thread safety
- [ ] Lock released in finally block

## UI Checklist
- [ ] EventBus: subscribe in `OnLoad`, unsubscribe in `Dispose`
- [ ] EventBus handlers marshal to UI thread
- [ ] Messages carry entity ID only — no data payloads
- [ ] Role-based visibility applied
- [ ] Loading state shown during API calls
- [ ] Error messages in Arabic

## Output Format
For each item: `✅ PASS` or `❌ FAIL: [specific violation]`
Summary: `X/Y checks passed`

## Phase 21: Users & Permissions Module — COMPLETE (v4.6.9)

Phase 21 is complete. Key checklist items for this module:
- [ ] UserStatus enum: Active=1, Inactive=2, Locked=3
- [ ] Passwordless User.Create() — no password parameter
- [ ] RecordLoginAttempt() logic: success resets counter, failure increments (lock at 5)
- [ ] Permission.IsSystem = true blocks deletion/modification
- [ ] AuditLog uses long Id (bigint) with 3 indexes
- [ ] All FK on Permission, RolePermission, AuditLog, UserSession use Restrict
- [ ] PermissionService.UpdateRolePermissionsAsync() uses ExecuteTransactionAsync
- [ ] DbSeeder seeds 33 permissions with 4-role assignments
- [ ] Admin user seeded passwordless (PasswordHash = null)