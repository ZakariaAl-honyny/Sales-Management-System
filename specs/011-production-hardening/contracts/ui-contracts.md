# UI Contracts: Production Hardening (v4.4)

**Feature**: `011-production-hardening`
**Date**: 2026-05-25

---

## New Views

### `DatabaseErrorDialog.xaml` + `DatabaseErrorDialogViewModel.cs`

Shown on Desktop startup when the API is unreachable.

- **Theme**: Orange/red, warning icon ⚠
- **Message**: "تعذر الاتصال بالخادم. يرجى التحقق من تشغيل الخدمة والمحاولة مرة أخرى."
- **Buttons**: "إعادة المحاولة" (calls `IHealthApiService.CheckHealthAsync` again) | "إغلاق" (calls `Application.Current.Shutdown()`)
- **Location**: `SalesSystem/SalesSystem.DesktopPWF/Views/Dialogs/DatabaseErrorDialog.xaml`
- **Registered via**: `IDialogService` (RULE-054, RULE-055)

---

## Modified Views

### `App.xaml.cs`

- On `OnStartup`, call `IHealthApiService.CheckHealthAsync(timeout: 5s)` BEFORE creating `MainWindow`.
- If failure → show `DatabaseErrorDialog` (modal, loops on Retry, exits on Close).
- If success → proceed to show `MainWindow` as normal.
- Register `UpdateService` as a startup background task (fire-and-forget) AFTER `MainWindow` is shown.

### Backup Settings UI (extend existing SettingsView)

- Add a "النسخ الاحتياطي" (Backup) section to `SettingsView.xaml` with:
  - `BackupPath` — TextBox with folder-browse button
  - `BackupScheduleTime` — TextBox (HH:mm format)
  - `BackupRetentionDays` — TextBox (numeric)
  - "نسخ احتياطي الآن" — manual trigger button (calls backup API endpoint or fires `BackupService` via a dedicated admin endpoint)
- Add an "التحديثات" (Updates) section with:
  - `UpdateServerUrl` — TextBox

---

## New Services (Desktop)

### `IHealthApiService` / `HealthApiService`

- Method: `Task<bool> CheckHealthAsync(CancellationToken ct)` — returns `true` if `/api/health` returns 200, `false` on any error or timeout.
- **File**: `SalesSystem/SalesSystem.DesktopPWF/Services/Api/HealthApiService.cs`

### `UpdateService`

- Method: `Task CheckAndUpdateAsync(CancellationToken ct)` — full update check/download/verify flow from research D-005.
- Called once from `App.xaml.cs` after `MainWindow` loads, via `Task.Run`.
- **File**: `SalesSystem/SalesSystem.DesktopPWF/Services/App/UpdateService.cs`
