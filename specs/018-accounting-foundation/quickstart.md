# Quickstart: Accounting Foundation (Phase 18)

**Feature**: `018-accounting-foundation`
**Date**: 2026-05-25

---

## 1. Migration Order
It is critical that the SQL migration is applied before writing any code. Run the `Task 0.1` SQL script provided in the Phase 18 document to:
1. Create `AccountTypes`.
2. Create `Accounts` and seed standard chart of accounts.
3. Create `JournalEntries` and `JournalEntryLines`.
4. Create `SystemAccountMappings` and seed the global mapping.

## 2. Entity Constraints
When building the Domain Entities, adhere to the strict access modifiers:
- `IsSystemAccount = true` means the account CANNOT be edited or deleted by the user. Throws `DomainException` in Arabic.
- Journal Entry Lines can **ONLY** be created via `JournalEntry.AddDebitLine()` and `JournalEntry.AddCreditLine()`. The line factory methods (`CreateDebit` / `CreateCredit`) must be `internal`.
- `JournalEntry.ValidateAndPost()` is the single most important method in the entire system. It MUST ensure `Total Debit == Total Credit` down to `0.001m` tolerance.

## 3. Decimal Precision
You MUST configure EF Core precision for the accounting lines to `HasPrecision(18,2)`. This matches the project-wide money precision standard (RULE-211). Double-entry validation uses a `0.001m` tolerance to handle any micro-fractional rounding.

## 4. Query Performance
For `GetAccountBalanceHandler` and `GetAccountStatementHandler`:
- ALWAYS use `.AsNoTracking()`.
- Only sum lines where `l.JournalEntry.IsPosted == true`. Draft or unposted entries (if they ever exist) should never affect ledger balances.

