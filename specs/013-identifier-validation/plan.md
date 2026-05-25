# Implementation Plan: Identifier Strategy & Validation (v4.5.3–v4.6.2)

**Branch**: `013-identifier-validation` | **Date**: 2026-05-25 | **Spec**: [spec.md](./spec.md)

---

## Summary

This feature removes the legacy `Code` identifier from master data entities (Products, Customers, Suppliers, Warehouses, Categories, Units, Users) across the entire stack (Database, Domain, Application, API, Desktop). Concurrently, it modernizes the WPF Desktop validation framework by implementing `INotifyDataErrorInfo` in `ViewModelBase`, applying a standard red-border UI error template, and replacing silent validation failures with an interactive, aggregated error dialog on save.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS (EF Core, ASP.NET Core, WPF)
**Architecture Scope**: Full Stack (Domain → Infrastructure → Application → API → Desktop)
**Constraints**:
- Must drop `Code` columns via EF Core migrations without data loss on `Id`.
- Must strip `Code` from all DTOs and API endpoints.
- `ViewModelBase` must implement `INotifyDataErrorInfo`.
- All 14 editor ViewModels must migrate from manual `HasXError` booleans to `INotifyDataErrorInfo` validation blocks.
- Action buttons must always remain enabled (Rule-059).

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal Precision | ✅ N/A | No financial math changes |
| II | Domain Formulas | ✅ N/A | No formula changes |
| III | Transactional Integrity | ✅ N/A | No transaction changes |
| IV | Invoice Lifecycle | ✅ N/A | No document changes |
| V | Stock Integrity | ✅ N/A | No stock logic changes |
| VI | Result Pattern | ✅ PASS | API error responses continue using Result<T> |
| VII | Architecture Boundaries | ✅ PASS | Domain remains pure, UI validation stays in WPF |
| VIII | Security | ✅ N/A | No security changes |
| IX | Four-Layer Validation | ✅ PASS | Aligns WPF UI validation with the 4-layer model |
| X | Logging | ✅ N/A | No logging changes |
| XI | EF Core Conventions | ✅ PASS | `Code` properties will be removed via standard Fluent API / migrations |
| XII | Audit Trail | ✅ N/A | No audit changes |
| XIII | Delete Strategy | ✅ N/A | No delete logic changes |
| XIV | Defensive Programming | ✅ PASS | Domain validation remains; UI validation prevents bad requests |
| XV | WPF Dialogs | ✅ PASS | Mandates `IDialogService.ShowWarningAsync` for save errors |
| XVI | Toast Notifications | ✅ N/A | No changes to toasts |
| XVII | Real-Time UI Validation | ✅ PASS | **Directly implements RULE-058 and RULE-059** |

**Gate Result**: ✅ ALL CLEAR — perfectly aligns with RULE-058 and RULE-059.

---

## Project Structure

### Source Code (affected paths)

```text
SalesSystem/
├── SalesSystem.Domain/
│   └── Entities/                 ← UPDATE (Remove Code properties)
├── SalesSystem.Infrastructure/
│   ├── Persistence/              ← UPDATE (Generate DropCode migration)
│   └── Configurations/           ← UPDATE (Remove Code constraints)
├── SalesSystem.Contracts/
│   └── Requests/Responses        ← UPDATE (Remove Code from DTOs)
├── SalesSystem.Application/
│   └── Services/                 ← UPDATE (Remove Code assignments)
└── SalesSystem.DesktopPWF/
    ├── ViewModels/
    │   ├── Base/ViewModelBase.cs ← UPDATE (Implement INotifyDataErrorInfo)
    │   └── Editors/              ← UPDATE (Refactor 14 VMs to new pattern)
    └── Views/
        └── Resources/            ← UPDATE (Add generic ErrorTemplate XAML)
```
