# Implementation Plan: Foundation Setup

**Branch**: `001-foundation-setup` | **Date**: 2026-05-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-foundation-setup/spec.md`

## Summary

Establish the complete .NET 10 LTS solution with 6 Clean Architecture projects,
22+ Domain entities with financial calculation logic, Contracts layer (DTOs,
Requests, Result<T>), EF Core Infrastructure with Fluent API configurations,
initial database migration, and seed data. This phase produces a buildable
solution and a fully migrated database — the foundation for all subsequent phases.

## Technical Context

**Language/Version**: C# / .NET 10 LTS
**Primary Dependencies**: Entity Framework Core 10, BCrypt.Net-Next 4.x
**Storage**: SQL Server 2019+ via EF Core (Code-First migrations)
**Testing**: Manual build verification + entity unit tests (xUnit)
**Target Platform**: Windows (Desktop + local API)
**Project Type**: Desktop application with Web API backend (Clean Architecture)
**Performance Goals**: Solution build < 30s, Migration < 30s
**Constraints**: Decimal-only financials, nvarchar-only text, Fluent API only
**Scale/Scope**: 22 database tables, 6 projects, ~80 C# files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal-Only Financial Precision | ✅ PASS | All money=decimal(18,2), quantity=decimal(18,3) |
| II | Domain-Computed Financial Formulas | ✅ PASS | LineTotal, SubTotal, TotalAmount, DueAmount in entities |
| III | Transactional Integrity | ⬜ N/A | No transactions in Phase 1 (no services yet) |
| IV | Invoice Lifecycle State Machine | ✅ PASS | InvoiceStatus enum defined; entity enforces transitions |
| V | Stock Integrity | ⬜ N/A | Stock logic in Phase 2; CHECK constraint in schema |
| VI | Result Pattern | ✅ PASS | Result<T> defined in Contracts |
| VII | Clean Architecture Boundaries | ✅ PASS | 6-project structure with correct dependency chain |
| VIII | Security | ⬜ N/A | Auth implemented in Phase 2; BCrypt used for seed pwd |
| IX | Four-Layer Validation | ✅ PASS | Domain validation in entities; DB CHECK constraints |
| X | Logging Standard | ⬜ N/A | Serilog configured in Phase 2 |
| XI | EF Core Conventions | ✅ PASS | Fluent API only, Restrict FKs, nvarchar, HasPrecision |
| XII | Audit Trail | ✅ PASS | CreatedByUserId FK on financial entities; Users soft delete |

**Gate Result**: ✅ ALL applicable principles satisfied.

## Project Structure

### Documentation (this feature)

```text
specs/001-foundation-setup/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── contracts.md
└── quickstart.md        # Phase 1 output
```

### Source Code (repository root)

```text
SalesSystem/
├── SalesSystem.sln
│
├── SalesSystem.Contracts/
│   ├── Common/
│   │   ├── Result.cs
│   │   ├── PagedResult.cs
│   │   └── ErrorCodes.cs
│   ├── DTOs/
│   │   ├── ProductDto.cs
│   │   ├── CustomerDto.cs
│   │   ├── SupplierDto.cs
│   │   ├── WarehouseDto.cs
│   │   ├── UnitDto.cs
│   │   ├── CategoryDto.cs
│   │   ├── SalesInvoiceDto.cs
│   │   ├── PurchaseInvoiceDto.cs
│   │   ├── SalesReturnDto.cs
│   │   ├── PurchaseReturnDto.cs
│   │   ├── StockTransferDto.cs
│   │   ├── CustomerPaymentDto.cs
│   │   ├── SupplierPaymentDto.cs
│   │   ├── WarehouseStockDto.cs
│   │   ├── InventoryMovementDto.cs
│   │   ├── StoreSettingsDto.cs
│   │   ├── UserDto.cs
│   │   └── DocumentSequenceDto.cs
│   ├── Requests/
│   │   ├── Products/
│   │   │   ├── CreateProductRequest.cs
│   │   │   └── UpdateProductRequest.cs
│   │   ├── Customers/
│   │   ├── Suppliers/
│   │   ├── Warehouses/
│   │   ├── Units/
│   │   ├── Categories/
│   │   └── Auth/
│   │       └── LoginRequest.cs
│   └── Responses/
│       └── LoginResponse.cs
│
├── SalesSystem.Domain/
│   ├── Common/
│   │   └── BaseEntity.cs
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Unit.cs
│   │   ├── Category.cs
│   │   ├── Product.cs
│   │   ├── Warehouse.cs
│   │   ├── WarehouseStock.cs
│   │   ├── Supplier.cs
│   │   ├── Customer.cs
│   │   ├── PurchaseInvoice.cs
│   │   ├── PurchaseInvoiceItem.cs
│   │   ├── SalesInvoice.cs
│   │   ├── SalesInvoiceItem.cs
│   │   ├── PurchaseReturn.cs
│   │   ├── PurchaseReturnItem.cs
│   │   ├── SalesReturn.cs
│   │   ├── SalesReturnItem.cs
│   │   ├── StockTransfer.cs
│   │   ├── StockTransferItem.cs
│   │   ├── CustomerPayment.cs
│   │   ├── SupplierPayment.cs
│   │   ├── InventoryMovement.cs
│   │   ├── StoreSettings.cs
│   │   └── DocumentSequence.cs
│   ├── Enums/
│   │   ├── UserRole.cs
│   │   ├── InvoiceStatus.cs
│   │   ├── PaymentType.cs
│   │   └── MovementType.cs
│   └── Exceptions/
│       ├── DomainException.cs
│       ├── NotFoundException.cs
│       └── ValidationException.cs
│
├── SalesSystem.Application/
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   │   └── IGenericRepository.cs
│   │   ├── Services/
│   │   │   └── (empty — populated in Phase 2)
│   │   └── IUnitOfWork.cs
│   └── Services/
│       └── (empty — populated in Phase 2)
│
├── SalesSystem.Infrastructure/
│   ├── Data/
│   │   ├── SalesDbContext.cs
│   │   └── Configurations/
│   │       ├── UserConfiguration.cs
│   │       ├── UnitConfiguration.cs
│   │       ├── CategoryConfiguration.cs
│   │       ├── ProductConfiguration.cs
│   │       ├── WarehouseConfiguration.cs
│   │       ├── WarehouseStockConfiguration.cs
│   │       ├── SupplierConfiguration.cs
│   │       ├── CustomerConfiguration.cs
│   │       ├── PurchaseInvoiceConfiguration.cs
│   │       ├── PurchaseInvoiceItemConfiguration.cs
│   │       ├── SalesInvoiceConfiguration.cs
│   │       ├── SalesInvoiceItemConfiguration.cs
│   │       ├── PurchaseReturnConfiguration.cs
│   │       ├── PurchaseReturnItemConfiguration.cs
│   │       ├── SalesReturnConfiguration.cs
│   │       ├── SalesReturnItemConfiguration.cs
│   │       ├── StockTransferConfiguration.cs
│   │       ├── StockTransferItemConfiguration.cs
│   │       ├── CustomerPaymentConfiguration.cs
│   │       ├── SupplierPaymentConfiguration.cs
│   │       ├── InventoryMovementConfiguration.cs
│   │       ├── StoreSettingsConfiguration.cs
│   │       └── DocumentSequenceConfiguration.cs
│   ├── Repositories/
│   │   └── (empty — populated in Phase 2)
│   └── Migrations/
│       └── (auto-generated by EF Core)
│
├── SalesSystem.Api/
│   ├── Program.cs (minimal — DI setup only, no controllers yet)
│   └── appsettings.json (placeholder — conn string from env var)
│
└── SalesSystem.Desktop/
    └── Program.cs (minimal — WinForms entry point stub)
```

**Structure Decision**: Clean Architecture with 6 projects as mandated by
the constitution. Domain at center with zero dependencies. All layers
follow strict dependency direction.

## Complexity Tracking

No violations — the 6-project structure is mandated by the constitution.
