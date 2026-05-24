# Research: Production Hardening (v4.4)

**Feature**: `011-production-hardening`
**Date**: 2026-05-25
**Status**: Complete — All unknowns resolved

---

## Decision Log

### D-001: Windows Service Hosting

**Decision**: Use `Microsoft.Extensions.Hosting.WindowsServices` via `builder.Host.UseWindowsService()` in `Program.cs`. Installation is done via `sc.exe` or PowerShell `New-Service`. Auto-recovery configured via `sc.exe failure` commands.

**Rationale**: This is the native .NET 10 pattern — no third-party wrapper needed. The service lifecycle integrates with `IHostApplicationLifetime` for graceful shutdown.

**Alternatives considered**: *NSSM (Non-Sucking Service Manager)*: Rejected — external dependency. *Topshelf*: Rejected — not maintained for .NET 10.

---

### D-002: Database Backup Strategy

**Decision**: Execute raw `BACKUP DATABASE [{dbName}] TO DISK = N'{filePath}'` via `SqlConnection` (ADO.NET) using the same connection string as the application. No SMO (SQL Server Management Objects) dependency.

**Rationale**: SMO requires SSMS installation on the server. Raw SQL backup works with SQL Express and full SQL Server without any additional installation. Explicitly required by PRD.

**Backup file naming**: `SalesSystemDb_{yyyy-MM-dd_HH-mm}.bak` stored in the configurable `BackupPath` directory.

**Retention cleanup**: After each backup run, enumerate all `.bak` files in the directory and delete those with `LastWriteTime < DateTime.Now.AddDays(-RetentionDays)`.

---

### D-003: DPAPI Connection String Encryption

**Decision**: Use `System.Security.Cryptography.ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine)` to encrypt the connection string. Store as Base64 with prefix `"DPAPI:"`. On startup, `IConnectionStringProtector.Decrypt(value)` checks for the prefix — if present, decrypts; if absent (first run), encrypts and rewrites the config file.

**Rationale**: DPAPI with `LocalMachine` scope is machine-bound but accessible by all processes on the machine (not user-scope). This is the correct scope for a Windows Service running under a LocalSystem account.

**Config file rewrite**: Uses `System.Text.Json` to read/modify/write `appsettings.json` atomically (write to temp file → `File.Move` with overwrite). NEVER log the decrypted value — RULE-037.

---

### D-004: ScheduledBackupWorker Timing

**Decision**: `ScheduledBackupWorker` extends `BackgroundService`. On each loop iteration, calculate the time until the next scheduled run (default: 02:00 AM local time, configurable via `SystemSettings`). Use `Task.Delay(timeUntilNextRun, stoppingToken)` to sleep. On wake, run backup and re-calculate next run time.

**Rationale**: Simple and reliable. No external scheduler (Hangfire, Quartz) required. The time calculation handles midnight crossing correctly by always calculating `nextRun - DateTime.Now`.

---

### D-005: Auto-Update Protocol

**Decision**: `UpdateService` flow:
1. Read `UpdateServerUrl` from `SystemSettings`.
2. `GET {url}/update-manifest.json` with `CancellationTokenSource(TimeSpan.FromSeconds(8))`.
3. Deserialize `UpdateManifest { Version, DownloadUrl, Sha256Hash }`.
4. Compare with current `Assembly.GetEntryAssembly().GetName().Version`.
5. If newer: `GET {DownloadUrl}` → save to temp file.
6. Compute `SHA256` of downloaded file → compare with manifest hash.
7. If match: notify user via `IToastNotificationService` "تحديث جاهز — أعد تشغيل البرنامج". If mismatch: delete temp file, log Serilog warning.
8. All steps wrapped in try/catch — any failure is silent (logged only).

**Rationale**: Simple pull-based model. No push notifications required. The 8-second CancellationToken enforces the timeout at the HTTP client level.

---

### D-006: Health Check Endpoint

**Decision**: Implement `IHealthCheck` as `DatabaseHealthCheck` that attempts `SELECT 1` via ADO.NET. Register with `builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database")`. Map to `/api/health` (unauthenticated — no `[Authorize]`). Returns `{ Status: "Healthy"/"Unhealthy", DatabaseStatus: "Reachable"/"Unreachable", Timestamp }`.

**Rationale**: The Desktop must call this before authentication is possible (startup check), so `[Authorize]` cannot be applied. The check is a simple TCP+query check — fast and reliable.

---

### D-007: Desktop Startup Connectivity Check

**Decision**: In `App.xaml.cs`, before loading `MainWindow`, call `IHealthApiService.CheckHealthAsync(timeout: 5 seconds)`. If success → proceed. If failure or timeout → show `DatabaseErrorDialog` (modal, styled, Arabic). Dialog has two buttons: "إعادة المحاولة" (loops) and "إغلاق" (calls `Application.Current.Shutdown()`).

**`DatabaseErrorDialog`**: Styled like other system dialogs (orange/red theme, warning icon). Implemented as a WPF Window registered via `IDialogService` — NOT `MessageBox.Show` (RULE-055).
