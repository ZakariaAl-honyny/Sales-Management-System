---
description: "Task list for Production Hardening (v4.4)"
---

# Tasks: Production Hardening (v4.4)

**Input**: `specs/011-production-hardening/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅

> **Implementation Note**: Each task includes exact file paths and class names so it can be executed by any model without additional context. Tasks marked [P] can run in parallel with other [P]-marked tasks in the same phase.

---

## Phase 1: Setup

- [X] T001 Verify solution builds with 0 errors: `dotnet build SalesSystem/SalesSystem.slnx` — FILE: `SalesSystem/SalesSystem.slnx`

---

## Phase 2: Foundational (BLOCKS all user stories)

- [X] T002 [P] Add `HealthCheckDto` record to Contracts: `{ string Status, string DatabaseStatus, DateTime Timestamp }` — FILE: `SalesSystem/SalesSystem.Contracts/Responses/HealthCheckDto.cs`

- [X] T003 [P] Add `UpdateManifest` class to Contracts: `{ string Version, string DownloadUrl, string Sha256Hash, string? ReleaseNotes }` — FILE: `SalesSystem/SalesSystem.Contracts/Responses/UpdateManifest.cs`

- [X] T004 [P] Add `IConnectionStringProtector` interface — ALREADY EXISTS at `SalesSystem/SalesSystem.Application/Interfaces/Services/IConnectionStringProtector.cs`

- [X] T005 [P] Add `IBackupService` interface — ALREADY EXISTS at `SalesSystem/SalesSystem.Application/Interfaces/Services/IBackupService.cs`

**Checkpoint**: `dotnet build` passes with all new interfaces added.

---

## Phase 3: US3 — DPAPI Connection String Encryption (Priority: P1)

**Goal**: Plaintext connection strings are encrypted with machine-bound DPAPI on first run. All deployed configs must have the `"DPAPI:"` prefix.

**Independent Test**: Set a plaintext connection string in `appsettings.json` → Run the API once → Inspect the file → Value starts with `"DPAPI:"` → Restart the API → Connects to the database normally.

- [X] T006 [US3] Implement `DpapiConnectionStringProtector` — ALREADY IMPLEMENTED as `ConnectionStringProtector` at `SalesSystem/SalesSystem.Infrastructure/Services/ConnectionStringProtector.cs`. Uses `ProtectedData.Protect`/`Unprotect` with `DataProtectionScope.LocalMachine`, `"DPAPI:"` prefix, Base64 encoding. Full DPAPI implementation with `IsEncrypted`, `Protect`, `Unprotect` matching the interface.

- [X] T007 [US3] DPAPI startup encryption — ALREADY IMPLEMENTED via `FirstRunSetupService.EnsureConnectionStringEncrypted()` at `SalesSystem/SalesSystem.Infrastructure/Security/FirstRunSetupService.cs`. Uses atomic write pattern (`.tmp` → `File.Replace()`). `SecureDbContextFactory.GetDecryptedConnectionString()` at `Infrastructure/Persistence/SecureDbContextFactory.cs` handles decryption. DI registered at `Program.cs:110`. NEVER logs plaintext values (RULE-037).

**Checkpoint**: Inspect `appsettings.json` — connection string starts with `"DPAPI:"`. API starts and connects to DB successfully.

---

## Phase 4: US2 — API as Windows Service + Health Check (Priority: P1)

**Goal**: API installs as a Windows Service that starts with Windows, auto-recovers from crashes, and exposes an unauthenticated `/api/health` endpoint.

**Independent Test**: Call `GET http://localhost:5221/api/health` → Returns `{ Status: "Healthy", DatabaseStatus: "Reachable" }`. Stop SQL Server → Call again → Returns `{ Status: "Unhealthy", DatabaseStatus: "Unreachable" }` with HTTP 503.

- [X] T008 [US2] `UseWindowsService()` + `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` — ALREADY IMPLEMENTED in `Program.cs` (lines 41-44, 56)

- [X] T009 [P] [US2] `DatabaseHealthCheck` created at `SalesSystem/SalesSystem.Infrastructure/Health/DatabaseHealthCheck.cs`. Uses raw ADO.NET with `SqlConnection`, 3-second timeout, `SELECT 1` heartbeat. Returns `Healthy("Reachable")` / `Unhealthy("Unreachable")`.

- [X] T010 [P] [US2] `HealthController` updated at `SalesSystem/SalesSystem.Api/Controllers/HealthController.cs`. Now uses `HealthCheckService` (not raw DbContext). Route `api/v1/health`. `[AllowAnonymous]`. Returns `HealthCheckDto`. `GET /` → 200/503. `GET /database` → 200/503.

- [X] T011 [US2] Health checks registered in `Program.cs` (lines 157-158). `builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database")`. `[AllowAnonymous]` on controller handles auth exclusion.

**Checkpoint**: `GET /api/health` returns 200 when DB is reachable, 503 when unreachable.

---

## Phase 5: US1 — Automated Database Backups (Priority: P1)

**Goal**: A background worker performs nightly SQL backup at configured time, writes `.bak` file, and deletes old backups beyond the retention period.

**Independent Test**: Set `Backup.ScheduleTime = "00:01"` (1 minute from now) → Wait → `.bak` file appears in `Backup.Path` → Create a `.bak` file with `LastWriteTime` older than retention → After the next backup run, the old file is gone.

- [X] T012 [US1] `BackupService` — ALREADY IMPLEMENTED at `SalesSystem/SalesSystem.Infrastructure/Services/BackupService.cs`. Full implementation: raw SQL `BACKUP DATABASE` with COMPRESSION, `RestoreBackupAsync` with SINGLE_USER/ROLLBACK AFTER 30/RESTORE/MULTI_USER recovery, `GetBackupListAsync`, `DeleteOldBackupsAsync`. All return `Result<T>`/`Result`. Tries `TrySetMultiUserAsync` on restore failure.

- [X] T013 [US1] `ScheduledBackupWorker` — ALREADY IMPLEMENTED at `SalesSystem/SalesSystem.Infrastructure/Backup/ScheduledBackupWorker.cs`. `BackgroundService` with 2:00 AM schedule loop, `IServiceScopeFactory` for scoped service, calls `CreateBackupAsync` then `DeleteOldBackupsAsync`. Handles `OperationCanceledException`. Serilog logging.

- [X] T014 [US1] DI Registration — ALREADY REGISTERED in `Program.cs` (lines 113-116): `Configure<BackupSettings>`, `AddScoped<IBackupService, BackupService>()`, `AddHostedService<ScheduledBackupWorker>()`, `AddUpdateServices()`.

- [X] T015 [P] [US1] Backup API endpoint — ALREADY IMPLEMENTED as `BackupController` at `SalesSystem/SalesSystem.Api/Controllers/BackupController.cs`. Route `api/v1/backup`, `[Authorize(Policy = "AdminOnly")]`. Endpoints: `POST /create` (create backup), `GET /list` (list backups), `POST /restore` (restore with path traversal guard).

**Checkpoint**: Background worker starts on API launch. Manual `POST /api/v1/admin/backup` → `.bak` file created. Old `.bak` files beyond retention window are cleaned up.

---

## Phase 6: US4 — Desktop Connectivity Error Handling (Priority: P2)

**Goal**: Desktop shows a styled Arabic error dialog on startup when the API is unreachable. User can retry or exit gracefully.

**Independent Test**: Stop the API → Launch the Desktop → `DatabaseErrorDialog` appears within 5 seconds with "إعادة المحاولة" and "إغلاق" buttons → Start the API → Click "إعادة المحاولة" → Main window loads normally.

- [X] T016 [P] [US4] Health check service — ALREADY IMPLEMENTED as `IDatabaseHealthCheckService` / `DatabaseHealthCheckService` at `Services/Api/DatabaseHealthCheckService.cs`. Uses `GET api/v1/health/database`, handles `HttpRequestException`/`TaskCanceledException`, returns `HealthCheckResult` with `IsDatabaseConnected`/`IsApiReachable`/`ErrorMessage`. Arabic error messages.

- [X] T017 [P] [US4] `DatabaseErrorDialog` — ALREADY IMPLEMENTED at `Views/Dialogs/DatabaseErrorDialog.xaml` + `.xaml.cs`. Orange/red theme, ⚠ warning icon, Arabic text, retry/exit buttons, retrying state with ProgressBar, proper `PositionOverOwner()` guard against self-ownership (RULE-224).

- [X] T018 [US4] `App.xaml.cs` startup health check — ALREADY IMPLEMENTED at `App.xaml.cs:106-126`. `CheckDatabaseConnectionAsync()` calls `IDatabaseHealthCheckService.CheckAsync()`, shows `DatabaseErrorDialog` on failure, returns `true`/`false`. On failure: `Log.Fatal` + `Environment.Exit(1)`.

- [X] T019 [US4] DI Registration — ALREADY REGISTERED at `App.xaml.cs:200`: `services.AddSingleton<IDatabaseHealthCheckService, DatabaseHealthCheckService>()`.

**Checkpoint**: Stop API → Launch Desktop → Arabic error dialog appears → Start API → Retry → Main window loads.

---

## Phase 7: US5 — Silent Auto-Update (Priority: P2)

**Goal**: Desktop checks for updates in background on startup, downloads and SHA256-verifies silently, notifies user to restart. Entire check must complete or timeout within 8 seconds.

**Independent Test**: Set `Update.ServerUrl` → Host `update-manifest.json` with a higher version number → Launch Desktop → Toast appears: `"تحديث جاهز — أعد تشغيل البرنامج"`. Host manifest with wrong SHA256 → Launch Desktop → No toast, partial download file deleted.

- [X] T020 [US5] `UpdateService` — ALREADY IMPLEMENTED as `UpdaterService` at `SalesSystem/SalesSystem.DesktopPWF/Services/App/UpdaterService.cs`. Full implementation: 8-second timeout via `CancellationTokenSource`, `CheckForUpdatesAsync` fetches version.json, SHA256 checksum verification, download with progress reporting, skipped version persisted to `%AppData%\SalesSystem\settings.json`. All return `Result<T>`. Arabic error messages. Serilog logging.

- [X] T021 [US5] Background update check — ALREADY IMPLEMENTED at `App.xaml.cs:88` (`_ = ScheduleBackgroundUpdateCheckAsync()`). Fires after MainWindow shown, 3-second initial delay, silent failure handling. `IUpdaterService` → `UpdaterService` registered as singleton at line 165.

**Checkpoint**: Update URL configured → Desktop loads normally (update runs in background) → Toast appears if update available → Wrong SHA256 → No toast, temp file deleted.

---

## Phase 8: Polish

- [X] T022 [P] Add backup settings fields to `SettingsView.xaml` and `SettingsViewModel.cs`: `BackupPath` (TextBox + folder browse button), `BackupScheduleTime` (TextBox HH:mm), `BackupRetentionDays` (TextBox numeric), `UpdateServerUrl` (TextBox). SaveCommand calls existing SystemSettings update endpoint — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Settings/SettingsView.xaml` + `ViewModels/SettingsViewModel.cs`

- [X] T023 [P] Verify all Serilog logging: checked `ConnectionStringProtector`, `BackupService`, `ScheduledBackupWorker`, `UpdaterService`, `DatabaseHealthCheck`, `HealthController`, `FirstRunSetupService`. 3 files fixed — `ConnectionStringProtector` (added ILogger + try-catch), `UpdaterService` (migrated from Serilog.Log statics to ILogger DI), `DatabaseHealthCheck` (added ILogger + logging catch). All 7 now pass. No secrets logged anywhere (RULE-035, RULE-037).

- [X] T024 [P] Create `sc.exe` installation script: `install-service.ps1` — FILE: `SalesSystem/scripts/install-service.ps1`

- [X] T025 Update `docs/CHANGELOG.md` with v4.4 entry: Production Hardening (DPAPI encryption, Windows Service, daily backup, Desktop health check dialog, silent auto-update) — FILE: `docs/CHANGELOG.md`

---

## Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS Phases 3–7
- **Phase 3 (US3 - DPAPI)**: Depends on Phase 2 — implement FIRST (secures API startup)
- **Phase 4 (US2 - Windows Service)**: Depends on Phase 2 — can run in parallel with Phase 3
- **Phase 5 (US1 - Backup)**: Depends on Phase 2
- **Phase 6 (US4 - Desktop Dialog)**: Depends on Phase 4 (needs health endpoint)
- **Phase 7 (US5 - Auto-Update)**: Depends on Phase 2
- **Phase 8 (Polish)**: Depends on all previous phases

---

## Implementation Strategy

### MVP First (US3 + US2 + US1)
1. Implement DPAPI encryption first (Phase 3) — secures the deployment
2. Implement Windows Service + Health Check (Phase 4) — makes the API production-ready
3. Implement Backup Worker (Phase 5) — protects data
4. **STOP AND VALIDATE** — run `dotnet publish`, install service, verify backup runs
5. Then Desktop dialog (Phase 6) and auto-update (Phase 7)
