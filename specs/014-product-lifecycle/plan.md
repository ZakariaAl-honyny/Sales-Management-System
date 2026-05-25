# Implementation Plan: Product Lifecycle & Media Management

**Branch**: `014-product-lifecycle` | **Date**: 2026-05-25 | **Spec**: [spec.md](./spec.md)

---

## Summary

This phase introduces product lifecycle management by tracking expiration dates and handling stock write-offs (الإتلاف). It accurately reduces warehouse stock and records the loss in history. Additionally, it implements an optimized product media management system that stores images locally (`%AppData%`) rather than bloating the SQL database, ensuring smooth WPF UI rendering via asynchronous lazy loading.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS (EF Core, WPF)
**Architecture Scope**: Full Stack (Domain → Infrastructure → Application → WPF Desktop)
**Constraints**:
- Images MUST NOT be stored as byte arrays in the database.
- ExpirationDate must be optional (`DateTime?`).
- Main UI must query for expired goods immediately on startup.

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal Precision | ✅ PASS | Write-off quantities use `decimal(18,3)` |
| II | Domain Formulas | ✅ N/A | No changes to core invoice formulas |
| III | Transactional Integrity | ✅ PASS | Write-off operation executes inside a database transaction |
| IV | Invoice Lifecycle | ✅ N/A | Document state unchanged |
| V | Stock Integrity | ✅ PASS | Write-offs decrease stock and write to `InventoryMovements` |
| VI | Result Pattern | ✅ PASS | All new endpoints return `Result<T>` |
| VII | Architecture Boundaries | ✅ PASS | Image I/O handled in infrastructure/desktop; Domain is pure |
| X | Logging | ✅ PASS | Stock write-offs and image failures will be logged via Serilog |
| XI | EF Core Conventions | ✅ PASS | `StockWriteOff` entity will use Fluent API configuration |
| XII | Audit Trail | ✅ PASS | Write-offs will track `CreatedByUserId` and `CreatedAt` |

**Gate Result**: ✅ ALL CLEAR — Perfectly aligns with existing architectural rules.

---

## Project Structure

### Source Code (affected paths)

```text
SalesSystem/
├── SalesSystem.Domain/
│   └── Entities/
│       ├── Products/Product.cs       ← UPDATE (Add ExpirationDate, ImagePath)
│       └── Inventory/StockWriteOff.cs← CREATE (New entity for damaged goods)
├── SalesSystem.Infrastructure/
│   ├── Persistence/                  ← UPDATE (Migrations)
│   └── Configurations/               ← UPDATE (StockWriteOffConfiguration)
├── SalesSystem.Application/
│   ├── Services/Products/            ← UPDATE (Product media upload logic)
│   └── Services/Inventory/           ← UPDATE (Write-off logic)
└── SalesSystem.DesktopPWF/
    ├── ViewModels/                   
    │   ├── Products/ProductEditorViewModel.cs ← UPDATE (Image upload/preview)
    │   └── MainViewModel.cs          ← UPDATE (Boot notification check)
    └── Views/
        └── Products/ProductEditor.xaml ← UPDATE (Image control, DatePicker)
```
