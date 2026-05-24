# Implementation Plan: Phase 7 — Production Readiness

**Branch**: `007-production-readiness` | **Date**: 2026-05-23 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `specs/007-production-readiness/spec.md`

## Summary

Deliver the production-hardening layer for the Sales Management System: a self-contained Windows installer, the API hosted as an auto-recovering Windows Service, DPAPI-encrypted connection strings on first run, a manual + scheduled SQL Server backup/restore system, an Admin Settings screen, a User Management screen, and a background auto-update check with SHA256 verification. All Admin-restricted operations are gated at both the API (JWT policy) and UI (sidebar visibility) layers. The desktop client verifies database connectivity before showing the login screen and presents a styled dialog on failure.

---

## Technical Context

**Language/Version**: C# / .NET 10 LTS  
**Primary Dependencies**: ASP.NET Core 10 (API), WPF (Desktop), Entity Framework Core 10, Serilog, FluentValidation 11, BCrypt.Net-Next, QuestPDF (printing), Inno Setup 6 (installer)  
**Storage**: SQL Server 2019+ — `BACKUP DATABASE` / `RESTORE DATABASE` raw T-SQL (no SMO dependency)  
**Testing**: xUnit + Moq + FluentAssertions (unit); WebApplicationFactory (integration)  
**Target Platform**: Windows 10 / Windows 11 (64-bit), Windows Service for API  
**Project Type**: Desktop application (WPF/MVVM) + ASP.NET Core Windows Service  
**Performance Goals**: Backup completes < 30s for 1 year of data; restore completes < 60s; update check times out in ≤ 8s  
**Constraints**: API must run headless as Windows Service with no console window; DPAPI encryption is machine-bound; installer must be single `.exe`, self-contained (.NET 10 bundled)  
**Scale/Scope**: Single-machine deployment, 1 store, up to 3 concurrent desktop users sharing 1 API instance

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Decimal-Only Financial Precision** | ✅ PASS | Backup/Restore does not introduce new financial fields |
| **II. Domain-Computed Financial Formulas** | ✅ PASS | No new financial calculations in this phase |
| **III. Transactional Integrity** | ✅ PASS | Restore uses `SINGLE_USER WITH ROLLBACK AFTER 30` to ensure no partial state |
| **IV. Invoice Lifecycle** | ✅ PASS | No invoice state changes in this phase |
| **V. Stock Integrity** | ✅ PASS | No stock operations in this phase |
| **VI. Result Pattern** | ✅ PASS | `BackupService`, `RestoreService`, `UserService`, `SettingsService` all return `Result<T>` |
| **VII. Clean Architecture Boundaries** | ✅ PASS | Desktop calls API via `ISettingsApiService`, `IUserApiService`, `IBackupApiService` — no direct DB access |
| **VIII. Security** | ✅ PASS | All new endpoints have `[Authorize]`; Settings/User endpoints use `AdminOnly` policy; DPAPI encryption for connection strings |
| **IX. Four-Layer Validation** | ✅ PASS | FluentValidation on all new Request models; Guard Clauses on User/Settings entities |
| **X. Logging Standard** | ✅ PASS | Serilog for all backup/restore/login events; Windows EventLog sink for Service mode |
| **XI. EF Core Conventions** | ✅ PASS | Fluent API only; `DeleteBehavior.Restrict` on all new FKs |
| **XII. Audit Trail** | ✅ PASS | Users soft-deleted only; `CreatedByUserId` on all relevant tables |
| **XIII. Delete Strategy** | ✅ PASS | User deactivation uses `Deactivate` strategy; permanent delete validates last-admin rule |
| **XIV. Defensive Programming** | ✅ PASS | Guard Clauses in `User.Create()` factory; backup path validation before operation |
| **XV. WPF Interactive Dialogs** | ✅ PASS | `IDialogService` for all confirmations; `DatabaseErrorDialog` for health check failure |
| **XVI. Toast Notifications** | ✅ PASS | Toast on successful save in Settings and User editor |
| **XVII. Real-Time UI Validation** | ✅ PASS | Settings and User editor ViewModels use `INotifyDataErrorInfo` |

**Constitution Gate**: ✅ ALL PASS — no violations. Proceed to Phase 0.

---

## Project Structure

### Documentation (this feature)

```text
specs/007-production-readiness/
├── plan.md              ← This file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── contracts/
│   ├── api-endpoints.md ← Phase 1 output
│   └── ui-contracts.md  ← Phase 1 output
└── tasks.md             ← Phase 2 output (/speckit-tasks command)
```

### Source Code (affected paths)

```text
SalesSystem.Api/
├── Controllers/
│   ├── SettingsController.cs        ← UPDATE: add CostingMethod + Logo endpoints
│   ├── UsersController.cs           ← UPDATE: ensure soft-delete only; add list/create/update
│   └── BackupController.cs          ← NEW: backup + restore endpoints (AdminOnly)
├── Validators/
│   ├── UpdateSettingsRequestValidator.cs   ← UPDATE: validate new fields
│   ├── CreateUserRequestValidator.cs       ← UPDATE/VERIFY
│   └── RestoreRequestValidator.cs          ← NEW
└── Program.cs / Extensions/
    └── WindowsServiceExtensions.cs         ← UPDATE: UseWindowsService() + EventLog sink

SalesSystem.Application/
├── Interfaces/Services/
│   ├── IBackupService.cs              ← NEW
│   └── ISystemSettingsRepository.cs   ← VERIFY exists for CostingMethod
└── Services/
    ├── BackupService.cs               ← NEW: raw SQL BACKUP/RESTORE, ScheduledBackupWorker
    └── UserService.cs                 ← UPDATE: verify soft-delete, last-admin guard

SalesSystem.Infrastructure/
└── Services/
    ├── BackupService.cs               ← NEW (infra implementation)
    ├── ConnectionStringProtector.cs   ← NEW: DPAPI encrypt/decrypt
    └── FirstRunSetupService.cs        ← NEW: idempotent first-run encryption

SalesSystem.DesktopPWF/
├── Views/Settings/
│   ├── SettingsView.xaml              ← UPDATE: add CostingMethod + logo fields
│   └── CostingMethodSettingsView.xaml ← VERIFY/UPDATE: uses ISettingsApiService
├── Views/Users/
│   └── UserEditorView.xaml            ← UPDATE: role dropdown, active toggle
├── ViewModels/Settings/
│   ├── SettingsViewModel.cs           ← UPDATE: CostingMethod binding, logo browse
│   └── CostingMethodSettingsViewModel.cs ← UPDATE: use ISettingsApiService (HTTP)
├── ViewModels/Users/
│   └── UserEditorViewModel.cs         ← VERIFY: Guard Clauses, SetDialogService()
├── Services/Api/
│   ├── ISettingsApiService.cs / SettingsApiService.cs  ← VERIFY: GetSettingsAsync / UpdateSettingsAsync
│   ├── IUserApiService.cs / UserApiService.cs           ← VERIFY: CRUD + deactivate
│   └── IBackupApiService.cs / BackupApiService.cs       ← NEW: backup + restore HTTP calls
└── Services/App/
    ├── AutoUpdateService.cs           ← NEW: SHA256-verified background update
    ├── DatabaseHealthCheckService.cs  ← VERIFY: startup check with DatabaseErrorDialog
    └── ScheduledBackupWorker.cs       ← NEW (or in API — see research.md)

Installer/
└── setup.iss                          ← NEW: Inno Setup script (self-contained .NET 10)
```

**Structure Decision**: Clean Architecture with 6 projects maintained. New Windows Service configuration, DPAPI, and backup belong in `SalesSystem.Infrastructure`. The Desktop `AutoUpdateService` and `BackupApiService` are added to `SalesSystem.DesktopPWF/Services/`. The `ScheduledBackupWorker` runs inside the API as a `BackgroundService` (not a separate process).

---

## Complexity Tracking

*No Constitution violations requiring justification.*
