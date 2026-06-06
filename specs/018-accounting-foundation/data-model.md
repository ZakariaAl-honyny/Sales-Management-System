# Data Model: Accounting Foundation (Phase 18)

**Feature**: `018-accounting-foundation`
**Date**: 2026-05-25

---

## 1. Enums

### `AccountType`
- `Asset = 1`
- `Liability = 2`
- `Equity = 3`
- `Revenue = 4`
- `Expense = 5`

### `JournalEntryType`
- `Sales = 1`, `SalesReturn = 2`, `Purchase = 3`, `PurchaseReturn = 4`
- `Expense = 5`, `StockWriteOff = 6`, `Transfer = 7`, `Manual = 8`, `OpeningBalance = 9`

## 2. Entities

### `Account`
- **Fields**: `AccountCode` (unique), `NameAr`, `NameEn`, `AccountType`, `ParentAccountId`, `IsSystemAccount`, `IsActive`, `Notes`.
- **Validation**: `IsSystemAccount = true` prevents editing and deletion.
- **Methods**: `IsDebitNormal()` returns true for Assets/Expenses, false for Liabilities/Equity/Revenues.

### `JournalEntry`
- **Fields**: `EntryNumber`, `TransactionDate`, `Description`, `EntryType`, Reference Fields (`Type`, `Id`, `Number`), `IsPosted`, `IsReversed`.
- **Validation**: MUST be balanced (`Total Debit == Total Credit`). Throw `DomainException` otherwise.
- **Methods**: `AddDebitLine()`, `AddCreditLine()`, `ValidateAndPost()`, `IsBalanced()`.

### `JournalEntryLine`
- **Fields**: `AccountId`, `AccountCode` (snapshot), `AccountNameAr` (snapshot), `Debit` (18,2), `Credit` (18,2).
- **Validation**: Cannot have both Debit and Credit > 0 on the same line. Cannot have negative values.

### `SystemAccountMappings`
- **Fields**: References to specific system accounts (Default Cash, Sales Revenue, COGS, VAT, etc.).
- **Methods**: `GetPaymentAccountId(paymentMethod)` to dynamically resolve Cash vs Bank.

## 3. Database Integrity
- `Accounts` table has `IsSystemAccount = 1` for default seeded accounts.
- `JournalEntryLines` has check constraints `CHK_DebitOrCredit` and `CHK_NoNegativeValues`.
- `JournalEntryLines` cascade deletes with `JournalEntry`, but restricts delete on `Account`.

