# Feature Specification: Accounting Foundation

## 1. Feature Overview
**Feature Name:** Accounting Foundation
**Description:** Implement the core financial backbone of the system, including the Chart of Accounts, Journal Entries (with double-entry bookkeeping validation), and System Account Mappings to link business operations to appropriate accounting ledger accounts.
**Target Audience:** Accountants, System Administrators, Financial Managers.
**Business Value:** Serves as the central repository for all financial transactions, ensuring accurate reporting, balanced books, and a single source of truth for the company's financial health.

## 2. Scope & Boundaries
**In Scope:**
- Database tables for Accounts, Journal Entries, Journal Entry Lines, and System Account Mappings.
- Seeding default Account Types and a standard Chart of Accounts.
- Domain models and EF Core configurations for accounting entities.
- Enforcing double-entry bookkeeping rules (`Total Debit = Total Credit`) at the domain level.
- Basic financial queries (Account Balance, Account Statement).
- Protection of system accounts from modification or deletion.

**Out of Scope:**
- Full UI/Frontend screens for Chart of Accounts management (to be handled in separate phases).
- UI for manual Journal Entry creation (to be handled in separate phases).
- Advanced financial reporting (e.g., Balance Sheet, Income Statement) beyond basic account statements.

## 3. User Scenarios & Use Cases

### Scenario 1: Automatic System Mapping
**Actor:** System (Automated)
**Action:** When a sales invoice is created, the system must automatically fetch the `SystemAccountMappings` to know which accounts to debit (e.g., Cash/AR) and credit (e.g., Sales Revenue, VAT Output).
**Outcome:** The system accurately generates the background journal entry without user intervention.

### Scenario 2: Manual Journal Entry Creation
**Actor:** Accountant
**Action:** The accountant creates a manual journal entry to adjust balances or record an expense.
**Outcome:** The system validates that the total debits equal total credits before allowing the entry to be posted.

### Scenario 3: Viewing Account Statement
**Actor:** Financial Manager
**Action:** The manager requests an account statement for the "Cash Account" between specific dates.
**Outcome:** The system calculates the opening balance, lists all transactions in the period, and calculates the running and closing balances accurately.

## 4. Functional Requirements

**REQ-1: Chart of Accounts Management**
- The system MUST support a hierarchical Chart of Accounts (Parent/Child accounts).
- Each account MUST have a unique Account Code.
- The system MUST support 5 primary account types: Assets, Liabilities, Equity, Revenues, Expenses.
- The system MUST prevent editing or deleting accounts marked as `IsSystemAccount`.

**REQ-2: Double-Entry Bookkeeping Validation**
- Every Journal Entry MUST contain at least two lines (one debit, one credit).
- The total sum of Debit amounts MUST exactly equal the total sum of Credit amounts.
- If an entry is unbalanced, the system MUST throw an error and prevent saving.
- Negative amounts are NOT allowed; an account must be either debited or credited with a positive value.

**REQ-3: Journal Entry Posting**
- Journal Entries MUST be posted immediately upon successful validation.
- Posted entries CANNOT be modified or deleted. Errors must be corrected via a reversed entry.
- All amounts MUST be stored with standard precision (`decimal(18,2)`).

**REQ-4: System Account Mappings**
- The system MUST maintain a mapping table linking default business operations (e.g., Default Cash, Sales Revenue, COGS, VAT) to specific Account IDs.
- If no mappings exist, the system MUST prevent financial transactions from occurring.

**REQ-5: Financial Queries**
- The system MUST calculate account balances by summarizing posted journal entry lines.
- The balance direction (positive/negative) MUST respect the account's normal balance type (`IsDebitNormal`).

## 5. Non-Functional Requirements
**Performance:** Financial queries (e.g., statements) must execute efficiently using appropriate database indexes. Queries must use `AsNoTracking` for read-only operations.
**Data Integrity:** EF Core MUST utilize `DeleteBehavior.Restrict` to prevent deletion of accounts that have associated journal entry lines.
**Auditability:** Every journal entry MUST track who created it, who posted it, and the exact timestamps.

## 6. Success Criteria
1. **Migration Success:** All 6 accounting tables are successfully created with correct constraints and foreign keys.
2. **Seed Data:** Over 20 default accounts and 1 global system mapping are correctly seeded into the database.
3. **Data Integrity:** The system successfully rejects any journal entry where `Debit != Credit`.
4. **Test Coverage:** All 9 unit tests defined for the accounting domain pass successfully.

## 7. Assumptions & Dependencies
- **Assumptions:** The current currency system does not require multi-currency support for basic journal entries.
- **Dependencies:** This phase must be completed before any subsequent phases (e.g., Sales, Purchases) that generate automatic journal entries.

## 8. Open Questions (Needs Clarification)
*(No outstanding questions. The PRD/Phase 18 documentation provides exhaustive technical instructions).*

