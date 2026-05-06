# Data Model: Backend Core (Phase 2)

**Feature**: Backend Core | **Date**: 2026-05-06

## Overview

Phase 2 operates on entities defined in Phase 1. This maps each entity to its service and validation rules.

## Entities → Services

| Entity | Service | CRUD | Auth Policy | DTO |
|--------|---------|------|-------------|-----|
| User | AuthService | Login only | Public (login) | LoginResponse |
| Product | ProductService | Full + Search | ManagerAndAbove | ProductDto |
| Category | CategoryService | Full | ManagerAndAbove | CategoryDto |
| Unit | UnitService | Full | ManagerAndAbove | UnitDto |
| Customer | CustomerService | Full (Cashier: view) | AllStaff | CustomerDto |
| Supplier | SupplierService | Full | ManagerAndAbove | SupplierDto |
| Warehouse | WarehouseService | Full | AdminOnly | WarehouseDto |
| DocumentSequence | DocumentSequenceService | GetNext only | Internal | N/A |

## Key Validation Rules

| Entity | Field | Rule |
|--------|-------|------|
| Product | Code/Barcode | Unique (if provided) |
| Product | PurchasePrice/SalePrice | >= 0, decimal(18,2) |
| Product | MinStock | >= 0, decimal(18,3) |
| Customer/Supplier | CurrentBalance | = OpeningBalance on create |
| Customer/Supplier | Balance direction | Customer +ve = owes us; Supplier +ve = we owe |
| Warehouse | IsDefault | Only one true at a time |
| User | PasswordHash | BCrypt factor 12 |
| DocumentSequence | LastNumber | Thread-safe increment via SemaphoreSlim |

## Relationships (Phase 2 scope)

```text
Category 1──* Product
Unit     1──* Product
Product  1──* WarehouseStock
Warehouse 1──* WarehouseStock
```

## State Transitions

No state machines in Phase 2. All entities use simple active/inactive (soft-delete). Invoice state machines are Phase 3 scope.
