# Implementation Plan: Desktop Shell

**Branch**: `004-desktop-shell` | **Date**: 2026-05-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/004-desktop-shell/spec.md`

---

## Summary

Build the WinForms desktop shell for the Sales Management System. This phase transitions the existing placeholder `Form1` into a production-ready shell that handles JWT authentication, role-based sidebar navigation (13 items, 3 roles), in-process event communication via an `IEventBus`, user feedback via toast notifications, and a set of 4 reusable common controls. The shell uses the .NET Generic Host for DI composition and reads the API base URL from `appsettings.json`.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10-windows
**Primary Dependencies**: `Microsoft.Extensions.Hosting` 10.x (new); `System.Text.Json` 10.x, `Microsoft.Extensions.Http` 10.x (existing via Hosting)
**Storage**: In-memory only (JWT token, UserSession). Config from `appsettings.json`.
**Testing**: Manual integration tests against running API (Phase 4). Unit tests deferred to Phase 7.
**Target Platform**: Windows 10/11 (WinForms, .NET 10-windows)
**Project Type**: Desktop application (WinForms)
**Performance Goals**: Login < 3s | Navigation < 500ms | EventBus dispatch < 1s
**Constraints**: Token NEVER persisted (Constitution Rule VIII) | Desktop NEVER connects to DB (Rule VII) | EventBus messages ID-only (Rule EventBus)
**Scale/Scope**: 1 main form + 13 placeholder screens + 6 service implementations + 4 common controls

---

## Constitution Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Decimal-Only Financial Precision | ✅ N/A | Shell has no financial calculations |
| II. Domain-Computed Financial Formulas | ✅ N/A | No formulas in UI shell |
| III. Transactional Integrity | ✅ N/A | No DB writes from Desktop |
| IV. Invoice Lifecycle State Machine | ✅ N/A | Not applicable in shell phase |
| V. Stock Integrity | ✅ N/A | Not applicable in shell phase |
| VI. Result Pattern | ✅ REQUIRED | `IAuthApiService.LoginAsync` returns `Result<UserSession>` |
| VII. Clean Architecture | ✅ REQUIRED | Desktop → HttpClient → API only. No direct DB access. |
| VIII. Security | ✅ REQUIRED | Token in-memory only. Never persisted. 401 handler triggers sign-out. |
| IX. Four-Layer Validation | ✅ Partial | Login request validated at API layer (FluentValidation already exists) |
| X. Logging | ⚠️ Deferred | Desktop-side Serilog deferred to Phase 7 (production hardening) |
| XI. EF Core Conventions | ✅ N/A | No EF Core in Desktop |
| XII. Audit Trail | ✅ N/A | Audit is an API-layer concern |
| EventBus Rules | ✅ REQUIRED | Subscribe OnLoad, Unsubscribe Dispose, ID-only messages |

**Gate Result: ✅ PASSED** — No violations. Phase 0 research complete.

---

## Project Structure

### Documentation (this feature)

```text
specs/004-desktop-shell/
├── plan.md              ← This file
├── spec.md              ← Feature specification
├── research.md          ← Phase 0: Design decisions
├── data-model.md        ← Phase 1: Entities + service interfaces
├── quickstart.md        ← Phase 1: Developer guide
├── contracts/
│   └── ui-contracts.md  ← Phase 1: Service + control contracts
└── tasks.md             ← Phase 2 output (speckit-tasks command)
```

### Source Code Layout

```text
SalesSystem/SalesSystem.Desktop/
├── Program.cs                         ← Generic Host + DI root
├── appsettings.json                   ← API base URL config
│
├── Configuration/
│   └── ApiSettings.cs
│
├── Models/
│   ├── UserSession.cs
│   ├── NavigationItem.cs
│   └── Notification.cs
│
├── Messages/
│   └── Messages.cs                    ← EntityChangedMessage + concrete types
│
├── Services/
│   ├── Interfaces/
│   │   ├── ISessionService.cs
│   │   ├── IAuthApiService.cs
│   │   ├── INavigationService.cs
│   │   ├── IEventBus.cs
│   │   ├── INotificationService.cs
│   │   └── IDialogService.cs
│   ├── SessionService.cs
│   ├── AuthApiService.cs
│   ├── NavigationService.cs
│   ├── EventBus.cs
│   ├── NotificationService.cs
│   ├── DialogService.cs
│   └── Http/
│       └── AuthTokenHandler.cs        ← 401 DelegatingHandler
│
├── Forms/
│   ├── LoginForm.cs / .Designer.cs
│   └── MainForm.cs  / .Designer.cs
│
└── Controls/
    ├── Common/
    │   ├── SearchBarControl.cs
    │   ├── LoadingOverlayControl.cs
    │   ├── SummaryCardControl.cs
    │   └── MoneyTextBox.cs
    └── Placeholders/
        ├── DashboardControl.cs
        ├── ProductsControl.cs
        ├── CustomersControl.cs
        ├── SuppliersControl.cs
        ├── WarehousesControl.cs
        ├── PurchasesControl.cs
        ├── SalesControl.cs
        ├── ReturnsControl.cs
        ├── TransfersControl.cs
        ├── PaymentsControl.cs
        ├── ReportsControl.cs
        ├── SettingsControl.cs
        └── UsersControl.cs
```

**Structure Decision**: Single project extension of the existing `SalesSystem.Desktop` WinForms project. No new projects added (Desktop is already project #6 in the 6-project solution). Clean layering via namespace-based folder organization.

---

## Complexity Tracking

No constitution violations to justify. Structure stays within the existing 6-project solution.
