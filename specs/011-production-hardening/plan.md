# Implementation Plan: Production Hardening (v4.4)

**Branch**: `011-production-hardening` | **Date**: 2026-05-25 | **Spec**: [spec.md](./spec.md)

---

## Summary

Harden the system for unattended production deployment on a Windows server. Five workstreams: (1) automated nightly SQL backup with retention cleanup via a background Worker, (2) API installable as a Windows Service with auto-recovery, (3) machine-bound DPAPI connection string encryption, (4) Desktop startup connectivity check with Arabic error dialog, and (5) silent background auto-update with SHA256 verification and 8-second timeout.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS
**Platform**: Windows x64 Server + Windows Desktop
**Primary Dependencies**:
- Backup: `Microsoft.SqlServer.Management.Smo` is excluded; raw SQL `BACKUP DATABASE` via `SqlConnection`
- Windows Service: `Microsoft.Extensions.Hosting.WindowsServices` (`UseWindowsService()`)
- DPAPI: `System.Security.Cryptography.ProtectedData` (built-in)
- Auto-Update: `System.Net.Http.HttpClient` + `System.Security.Cryptography.SHA256`
- Background Jobs: `IHostedService` / `BackgroundService`
- Health Check: `Microsoft.AspNetCore.Diagnostics.HealthChecks`
**Storage**: `SystemSettings` table for configurable values (BackupPath, BackupSchedule, RetentionDays, UpdateServerUrl, ThermalPrinterName)
**Constraints**:
- RULE-035/036/037: All events logged via Serilog; NEVER log passwords or connection strings
- RULE-039: Passwords BCrypt work factor 12 — no change to auth
- RULE-040: Connection strings via environment variables OR DPAPI-encrypted config — NEVER plaintext
- RULE-006: All service methods return `Result<T>` — never throw
- Auto-update timeout: 8 seconds hard limit (CancellationTokenSource)
- Backup: raw `BACKUP DATABASE TO DISK` SQL — no SMO/SSMS dependency
- Health check endpoint MUST be unauthenticated (no `[Authorize]`)

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal Precision | ✅ N/A | No financial data |
| II | Domain Formulas | ✅ N/A | No financial formulas |
| III | Transactional Integrity | ✅ PASS | Backup is outside EF Core transaction — uses raw ADO.NET |
| IV | Invoice Lifecycle | ✅ N/A | No invoice changes |
| V | Stock Integrity | ✅ N/A | No stock changes |
| VI | Result Pattern | ✅ PASS | All services return `Result<T>` — RULE-006 |
| VII | Architecture Boundaries | ✅ PASS | Desktop calls API health check — no direct DB access |
| VIII | Security | ✅ PASS | DPAPI encryption; health check is intentionally unauthenticated |
| IX | Four-Layer Validation | ✅ PASS | BackupPath/RetentionDays validated at service layer |
| X | Logging | ✅ PASS | Backup runs, update events, encryption events logged via Serilog |
| XI | EF Core Conventions | ✅ N/A | No new EF entities |
| XII | Audit Trail | ✅ N/A | No financial audit trail required for infra events |
| XIII | Delete Strategy | ✅ N/A | File cleanup (old backups) is filesystem, not DB |
| XIV | Defensive Programming | ✅ PASS | Guard clauses for null/empty paths; try/catch around all IO and network ops |
| XV | WPF Dialogs | ✅ PASS | `DatabaseErrorDialog` via `IDialogService` — no raw `MessageBox.Show` |
| XVI | Toast Notifications | ✅ PASS | Update-ready toast shown via `IToastNotificationService` |
| XVII | Real-Time UI Validation | ✅ N/A | No complex form input in this feature |

**Gate Result**: ✅ ALL CLEAR — no violations.

---

## Project Structure

### Source Code (affected paths)

```text
SalesSystem/
├── SalesSystem.Contracts/
│   └── Responses/
│       └── HealthCheckDto.cs            ← NEW { Status, DatabaseStatus, Timestamp }
│
├── SalesSystem.Application/
│   ├── Interfaces/Services/
│   │   ├── IBackupService.cs            ← NEW
│   │   └── IConnectionStringProtector.cs ← NEW
│   └── Services/
│       └── BackupService.cs             ← NEW
│
├── SalesSystem.Infrastructure/
│   ├── Security/
│   │   └── DpapiConnectionStringProtector.cs ← NEW
│   ├── BackgroundWorkers/
│   │   └── ScheduledBackupWorker.cs     ← NEW (IHostedService)
│   └── Health/
│       └── DatabaseHealthCheck.cs       ← NEW (IHealthCheck)
│
├── SalesSystem.Api/
│   ├── Program.cs                       ← EXTEND (UseWindowsService, HealthChecks, Protector)
│   └── Controllers/
│       └── HealthController.cs          ← NEW (unauthenticated /api/health)
│
└── SalesSystem.DesktopPWF/
    ├── Services/
    │   ├── App/
    │   │   └── UpdateService.cs         ← NEW (check + download + SHA256 + timeout)
    │   └── Api/
    │       └── HealthApiService.cs      ← NEW (calls /api/health)
    └── Views/Dialogs/
        └── DatabaseErrorDialog.xaml     ← NEW (Retry/Exit dialog)
```
