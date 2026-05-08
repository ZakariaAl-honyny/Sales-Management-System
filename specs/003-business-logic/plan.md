# Implementation Plan: Business Logic Implementation

**Branch**: `003-business-logic` | **Date**: 2026-05-07 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/003-business-logic/spec.md`

---

## Summary

Implement the full transactional business logic layer for the Sales Management System, covering seven services: `InventoryService`, `PurchaseService`, `SalesService`, `SalesReturnService`, `PurchaseReturnService`, `StockTransferService`, and `PaymentService`. Each service orchestrates multi-table database transactions that maintain stock integrity, customer/supplier balance accuracy, and a complete `InventoryMovement` audit trail. Six new API controllers expose this logic via role-protected endpoints. All logic follows the established `Result<T>` pattern, 7-step transaction protocol, and Clean Architecture boundaries defined in the project constitution.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS
**Primary Dependencies**: ASP.NET Core 10, Entity Framework Core 10, FluentValidation 11.x, Serilog 8.x, Microsoft.AspNetCore.Authentication.JwtBearer 10.x
**Storage**: SQL Server 2019+ via EF Core — all invoice, stock, movement, and payment tables are already migrated
**Testing**: Manual API testing via Swagger/Scalar; transaction rollback verified by intentional failure injection
**Target Platform**: Windows Server / Windows 10+ (local deployment, self-hosted API)
**Project Type**: Clean Architecture — 6-project solution (Contracts, Domain, Application, Infrastructure, Api, Desktop)
**Performance Goals**: Purchase/Sales post operations complete in < 2 seconds for invoices with up to 20 line items
**Constraints**: All money in `decimal(18,2)`, all quantities in `decimal(18,3)`; no `float`/`double` anywhere; `Console.WriteLine` forbidden; all endpoints `[Authorize]`
**Scale/Scope**: Single-store local deployment; estimated < 500 invoices/day; multi-warehouse stock management

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal-only financials | ✅ PASS | All DTOs, request models, and domain entities use `decimal(18,2)` / `decimal(18,3)` — confirmed in `AllDtos.cs` and existing domain entities |
| II | Domain-computed financial formulas | ✅ PASS | `LineTotal`, `SubTotal`, `TotalAmount`, `DueAmount` computed in domain entity methods only; services never compute these |
| III | Transactional integrity (7-step protocol) | ✅ PASS | `IUnitOfWork.BeginTransactionAsync` available; all services will follow: validate → begin → save invoice → stock → movements → balance → commit/rollback |
| IV | Invoice lifecycle state machine | ✅ PASS | `InvoiceStatus` enum (Draft=1, Posted=2, Cancelled=3) enforced; no hard-delete; no Posted→Draft; services enforce transitions |
| V | Stock integrity | ✅ PASS | Pre-transaction stock validation; stock deducted AFTER invoice saved (reference ID available); every change creates `InventoryMovement`; DB `CHECK (Quantity >= 0)` in place |
| VI | Result pattern | ✅ PASS | All services return `Result<T>` or `Result`; controllers translate to HTTP; no raw exceptions |
| VII | Clean Architecture boundaries | ✅ PASS | Application services use `IUnitOfWork`; domain has zero EF references; controllers delegate to services only |
| VIII | Security | ✅ PASS | All new controllers will have `[Authorize]` with role policies matching the permissions matrix |
| IX | Four-layer validation | ✅ PASS | Domain: entity method guards; Application: pre-transaction stock checks; API: FluentValidation validators; DB: CHECK constraints |
| X | Logging (Serilog) | ✅ PASS | `ILogger<T>` injected in all services; critical operations (post, cancel, stock change, payment) logged |
| XI | EF Core conventions | ✅ PASS | Fluent API only; no DataAnnotations on entities; `DeleteBehavior.Restrict` on all FKs; all already configured |
| XII | Audit trail | ✅ PASS | `CreatedByUserId` on all invoice/payment entities; Users soft-delete only |

**Gate result**: ✅ All 12 principles satisfied — proceeding to Phase 1 design.

**Complexity tracking**: No constitution violations. No additional projects needed. No pattern deviations.

---

## Project Structure

### Documentation (this feature)

```text
specs/003-business-logic/
├── plan.md              ← This file
├── research.md          ← Phase 0 output (below)
├── data-model.md        ← Phase 1 output (below)
├── contracts/           ← Phase 1 output (API contracts)
│   ├── purchase-invoices.md
│   ├── sales-invoices.md
│   ├── returns.md
│   ├── stock-transfers.md
│   └── payments.md
└── tasks.md             ← Phase 2 output (/speckit-tasks command)
```

### Source Code Layout (additions for this feature)

```text
SalesSystem/
├── SalesSystem.Contracts/
│   ├── DTOs/
│   │   └── AllDtos.cs                         ← EXISTING (all DTOs already defined)
│   └── Requests/
│       ├── Purchases/
│       │   ├── CreatePurchaseInvoiceRequest.cs  ← NEW
│       │   └── PostInvoiceRequest.cs            ← NEW (shared for post/cancel)
│       ├── Sales/
│       │   └── CreateSalesInvoiceRequest.cs     ← NEW
│       ├── Returns/
│       │   ├── CreateSalesReturnRequest.cs      ← NEW
│       │   └── CreatePurchaseReturnRequest.cs   ← NEW
│       ├── Transfers/
│       │   └── CreateStockTransferRequest.cs    ← NEW
│       └── Payments/
│           ├── CreateCustomerPaymentRequest.cs  ← NEW
│           └── CreateSupplierPaymentRequest.cs  ← NEW
│
├── SalesSystem.Application/
│   ├── Interfaces/
│   │   ├── IUnitOfWork.cs                      ← EXTEND (add invoice/return/transfer/payment repos)
│   │   └── Services/
│   │       ├── IInventoryService.cs             ← NEW
│   │       ├── IPurchaseService.cs              ← NEW
│   │       ├── ISalesService.cs                 ← NEW
│   │       ├── ISalesReturnService.cs           ← NEW
│   │       ├── IPurchaseReturnService.cs        ← NEW
│   │       ├── IStockTransferService.cs         ← NEW
│   │       └── IPaymentService.cs               ← NEW
│   └── Services/
│       ├── InventoryService.cs                  ← NEW (the single stock authority)
│       ├── PurchaseService.cs                   ← NEW
│       ├── SalesService.cs                      ← NEW
│       ├── SalesReturnService.cs                ← NEW
│       ├── PurchaseReturnService.cs             ← NEW
│       ├── StockTransferService.cs              ← NEW
│       └── PaymentService.cs                    ← NEW
│
├── SalesSystem.Infrastructure/
│   ├── Data/
│   │   └── UnitOfWork.cs                       ← EXTEND (wire new repos)
│   └── (no new migrations needed — schema complete)
│
└── SalesSystem.Api/
    ├── Controllers/
    │   ├── PurchaseInvoicesController.cs        ← NEW
    │   ├── SalesInvoicesController.cs           ← NEW
    │   ├── SalesReturnsController.cs            ← NEW
    │   ├── PurchaseReturnsController.cs         ← NEW
    │   ├── StockTransfersController.cs          ← NEW
    │   └── PaymentsController.cs                ← NEW
    └── Validators/
        ├── Purchases/
        │   └── CreatePurchaseInvoiceValidator.cs ← NEW
        ├── Sales/
        │   └── CreateSalesInvoiceValidator.cs    ← NEW
        ├── Returns/
        │   ├── CreateSalesReturnValidator.cs     ← NEW
        │   └── CreatePurchaseReturnValidator.cs  ← NEW
        ├── Transfers/
        │   └── CreateStockTransferValidator.cs   ← NEW
        └── Payments/
            ├── CreateCustomerPaymentValidator.cs ← NEW
            └── CreateSupplierPaymentValidator.cs ← NEW
```

**Structure Decision**: Extends the existing 6-project Clean Architecture solution. No new projects. All new code slots into established namespaces and follows the `ProductService` pattern as the template.

---

## Complexity Tracking

> No constitution violations. No entries required.
