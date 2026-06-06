# Research: Accounting Foundation (Phase 18)

**Feature**: `018-accounting-foundation`
**Date**: 2026-05-25

---

## 1. Zero Clarifications Required
The requirements provided in the Phase 18 document are exceptionally detailed and prescriptive. There are no technical unknowns. The user has provided the exact Domain Entities, SQL Schema, EF Core Configurations, and MediatR Commands. 

## 2. Best Practices & Decisions

### Decision 1: Decimal Precision
- **Decision**: Use `decimal(18,2)` for all Journal Entry lines, matching the standard money precision.
- **Rationale**: Per RULE-211, ALL money fields MUST use `decimal(18,2)`. The previous `decimal(18,2)` was overridden by the project constitution to eliminate rounding inconsistency across financial columns. Double-entry validation uses `Math.Abs(totalDebit - totalCredit) < 0.001m` tolerance to handle any micro-fractional rounding.
- **Alternatives Considered**: `decimal(18,2)` - rejected per RULE-211 (project-wide money precision standard).

### Decision 2: Snapshotting Account Data
- **Decision**: Store `AccountCode` and `AccountNameAr` directly in the `JournalEntryLine` table.
- **Rationale**: An account's name might change in the future, but the journal entry must preserve the exact state of the account name at the time the transaction occurred for audit purposes.
- **Alternatives Considered**: Only storing `AccountId` - rejected due to audit vulnerability.

### Decision 3: Double-Entry Validation Location
- **Decision**: Validate `Total Debit == Total Credit` inside the Domain Entity (`JournalEntry.ValidateAndPost()`) and inside the Application Validator (`CreateJournalEntryCommandValidator`).
- **Rationale**: Defense in depth. The Validator catches errors before DB access, while the Domain Entity ensures that even programmatic bypassing of the validator cannot result in unbalanced books being saved.

