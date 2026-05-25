# Implementation Plan: Critical Business Rules Reference (Phase 16)

**Branch**: `016-critical-business-rules` | **Date**: 2026-05-25 | **Spec**: [spec.md](./spec.md)

---

## Summary

This phase centralizes and enforces the core business rules across all transaction flows (Sales, Purchase, Return, Transfer, Payment). The primary focus is ensuring absolute atomic database integrity using `IUnitOfWork.BeginTransactionAsync`, implementing thread-safe invoice generation, and enforcing the rule that inventory mutations only occur when an invoice transitions to the "Posted" state. 

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS (EF Core, MediatR, FluentValidation)
**Architecture Scope**: Domain & Application Layers
**Constraints**:
- Every multi-entity mutation must occur within an explicit `BEGIN TRANSACTION`.
- Any Domain exception must cause a full rollback.
- Stock validation must occur prior to transaction execution to avoid locking the database unnecessarily.

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal Precision | ✅ PASS | All financial/quantity checks use `decimal` natively |
| II | Domain Formulas | ✅ PASS | Validation of `PaidAmount <= TotalAmount` remains in Domain |
| III | Transactional Integrity | ✅ PASS | **CORE FOCUS** - Explicit enforcement of `BeginTransactionAsync` |
| IV | Invoice Lifecycle | ✅ PASS | Status explicitly managed (Draft -> Posted) |
| V | Stock Integrity | ✅ PASS | Stock deduction occurs ONLY on "Posted" status |
| VI | Result Pattern | ✅ PASS | All validation blocks return `Result.Failure` |
| VII | Architecture Boundaries | ✅ PASS | Application layer coordinates DB transactions; Domain is pure |
| X | Logging | ✅ PASS | Rollbacks and rule violations must be logged via Serilog |
| XI | EF Core Conventions | ✅ PASS | UnitOfWork handles the EF Core explicit transactions |

**Gate Result**: ✅ ALL CLEAR — This phase strictly enforces the existing architectural constitution.

---

## Project Structure

### Source Code (affected paths)

```text
SalesSystem/
├── SalesSystem.Domain/
│   ├── Entities/
│   │   ├── Sales/SalesInvoice.cs         ← UPDATE (Enforce PaidAmount rule in method)
│   │   ├── Purchases/PurchaseInvoice.cs  ← UPDATE (Enforce logic)
│   │   └── Inventory/StockTransfer.cs    ← UPDATE (Ensure symmetry)
│   └── Exceptions/
│       └── DomainException.cs            ← VERIFY (Standardized usage)
├── SalesSystem.Application/
│   ├── Core/
│   │   └── IUnitOfWork.cs                ← VERIFY (BeginTransactionAsync exists)
│   ├── Services/
│   │   ├── DocumentSequenceService.cs    ← UPDATE (Enforce thread-safety with SemaphoreSlim)
│   │   ├── Sales/SalesInvoiceService.cs  ← REFACTOR (Wrap in transactions, check stock first)
│   │   ├── Purchases/PurchaseInvoiceService.cs ← REFACTOR
│   │   └── Inventory/StockTransferService.cs   ← REFACTOR (Atomic source decrease / dest increase)
└── SalesSystem.Api/
    └── Middlewares/
        └── ExceptionHandlingMiddleware.cs ← VERIFY (Properly catches and formats DomainExceptions)
```
