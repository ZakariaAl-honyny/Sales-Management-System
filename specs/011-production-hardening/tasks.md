---
description: "Task list for Production Hardening (v4.4)"
---

# Tasks: Production Hardening (v4.4)

**Input**: `specs/011-production-hardening/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅

> **Implementation Note**: Each task includes exact file paths and class names so it can be executed by any model without additional context. Tasks marked [P] can run in parallel with other [P]-marked tasks in the same phase.

---

## Phase 1: Setup

- [ ] T001 Verify solution builds with 0 errors: `dotnet build SalesSystem/SalesSystem.slnx` — FILE: `SalesSystem/SalesSystem.slnx`

---

## Phase 2: Foundational (BLOCKS all user stories)

- [ ] T002 [P] Add `HealthCheckDto` record to Contracts: `{ string Status, string DatabaseStatus, DateTime Timestamp }` — FILE: `SalesSystem/SalesSystem.Contracts/Responses/HealthCheckDto.cs`

- [ ] T003 [P] Add `UpdateManifest` class to Contracts: `{ string Version, string DownloadUrl, string Sha256Hash, string? ReleaseNotes }` — FILE: `SalesSystem/SalesSystem.Contracts/Responses/UpdateManifest.cs`

- [ ] T004 [P] Add `IConnectionStringProtector` interface with `string Encrypt(string plaintext)` and `string Decrypt(string ciphertext)` — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Security/IConnectionStringProtector.cs`

- [ ] T005 [P] Add `IBackupService` interface: `Task<Result> BackupNowAsync(CancellationToken ct)` — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Services/IBackupService.cs`

**Checkpoint**: `dotnet build` passes with all new interfaces added.

---

## Phase 3: US3 — DPAPI Connection String Encryption (Priority: P1)

**Goal**: Plaintext connection strings are encrypted with machine-bound DPAPI on first run. All deployed configs must have the `"DPAPI:"` prefix.

**Independent Test**: Set a plaintext connection string in `appsettings.json` → Run the API once → Inspect the file → Value starts with `"DPAPI:"` → Restart the API → Connects to the database normally.

- [ ] T006 [US3] Implement `DpapiConnectionStringProtector` implementing `IConnectionStringProtector`. `Encrypt`: uses `ProtectedData.Protect(Encoding.UTF8.GetBytes(plaintext), null, DataProtectionScope.LocalMachine)` → returns `"DPAPI:" + Convert.ToBase64String(...)`. `Decrypt`: strips `"DPAPI:"` prefix → `ProtectedData.Unprotect(...)` → returns plaintext. Wrap both in try/catch returning `Result` — FILE: `SalesSystem/SalesSystem.Infrastructure/Security/DpapiConnectionStringProtector.cs`

- [ ] T007 [US3] Add startup encryption logic in `Program.cs`: Before `builder.Build()`, read the raw connection string value. If it does NOT start with `"DPAPI:"`, call `protector.Encrypt(value)`, then atomically rewrite `appsettings.json` using `System.Text.Json` (write to `appsettings.json.tmp` → `File.Move` with overwrite). NEVER log the plaintext value (RULE-037). Register `IConnectionStringProtector` → `DpapiConnectionStringProtector` as Singleton. If it DOES start with `"DPAPI:"`, call `protector.Decrypt(value)` and use the result as the connection string — FILE: `SalesSystem/SalesSystem.Api/Program.cs`

**Checkpoint**: Inspect `appsettings.json` — connection string starts with `"DPAPI:"`. API starts and connects to DB successfully.

---

## Phase 4: US2 — API as Windows Service + Health Check (Priority: P1)

**Goal**: API installs as a Windows Service that starts with Windows, auto-recovers from crashes, and exposes an unauthenticated `/api/health` endpoint.

**Independent Test**: Call `GET http://localhost:5000/api/health` → Returns `{ Status: "Healthy", DatabaseStatus: "Reachable" }`. Stop SQL Server → Call again → Returns `{ Status: "Unhealthy", DatabaseStatus: "Unreachable" }` with HTTP 503.

- [ ] T008 [US2] Add `Microsoft.Extensions.Hosting.WindowsServices` NuGet if not already referenced and add `builder.Host.UseWindowsService()` to `Program.cs`. Also register `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` at startup — FILE: `SalesSystem/SalesSystem.Api/Program.cs`, `SalesSystem/SalesSystem.Api/SalesSystem.Api.csproj`

- [ ] T009 [P] [US2] Implement `DatabaseHealthCheck` implementing `IHealthCheck`. In `CheckHealthAsync`: open a `SqlConnection` using the configured connection string, execute `SELECT 1`, return `HealthCheckResult.Healthy("Reachable")` on success or `HealthCheckResult.Unhealthy("Unreachable")` on exception. Connection timeout: 3 seconds — FILE: `SalesSystem/SalesSystem.Infrastructure/Health/DatabaseHealthCheck.cs`

- [ ] T010 [P] [US2] Create `HealthController` with route `[Route("api/health")]`. NO `[Authorize]`. `GET /` calls `IHealthCheckService` (injected `HealthCheckService` from `Microsoft.Extensions.Diagnostics.HealthChecks`). Maps `Healthy` → 200, `Unhealthy` → 503. Returns `HealthCheckDto` — FILE: `SalesSystem/SalesSystem.Api/Controllers/HealthController.cs`

- [ ] T011 [US2] Register health checks in `Program.cs`: `builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database")`. Register health check middleware. Ensure the `/api/health` route is excluded from `[Authorize]` global policies — FILE: `SalesSystem/SalesSystem.Api/Program.cs`

**Checkpoint**: `GET /api/health` returns 200 when DB is reachable, 503 when unreachable.

---

## Phase 5: US1 — Automated Database Backups (Priority: P1)

**Goal**: A background worker performs nightly SQL backup at configured time, writes `.bak` file, and deletes old backups beyond the retention period.

**Independent Test**: Set `Backup.ScheduleTime = "00:01"` (1 minute from now) → Wait → `.bak` file appears in `Backup.Path` → Create a `.bak` file with `LastWriteTime` older than retention → After the next backup run, the old file is gone.

- [ ] T012 [US1] Implement `BackupService` (implements `IBackupService`). Inject `IConfiguration`, `ILogger<BackupService>`. In `BackupNowAsync`: (1) read `Backup.Path` and `Backup.RetentionDays` from `ISystemSettingsRepository`. (2) Ensure directory exists (`Directory.CreateDirectory`). (3) Build filename `SalesSystemDb_{DateTime.Now:yyyy-MM-dd_HH-mm}.bak`. (4) Open `SqlConnection` and execute `BACKUP DATABASE [{dbName}] TO DISK = N'{fullPath}'`. (5) Delete `.bak` files in the directory where `LastWriteTime < DateTime.Now.AddDays(-retentionDays)`. (6) Log result via Serilog. Wrap all in try/catch returning `Result` — FILE: `SalesSystem/SalesSystem.Application/Services/BackupService.cs`

- [ ] T013 [US1] Implement `ScheduledBackupWorker` extending `BackgroundService`. Inject `IBackupService`, `ILogger`. In `ExecuteAsync`: (1) Read `Backup.ScheduleTime` (HH:mm) from settings. (2) Calculate milliseconds until next occurrence using `TimeSpan`. (3) `await Task.Delay(delay, stoppingToken)`. (4) Call `_backupService.BackupNowAsync(stoppingToken)`. (5) Loop. Log each run. Handle `OperationCanceledException` gracefully on shutdown — FILE: `SalesSystem/SalesSystem.Infrastructure/BackgroundWorkers/ScheduledBackupWorker.cs`

- [ ] T014 [US1] Register services in DI: `builder.Services.AddScoped<IBackupService, BackupService>()`. Register Worker: `builder.Services.AddHostedService<ScheduledBackupWorker>()`. Add `Backup.*` and `Update.ServerUrl` default rows to the database seeder if not already present — FILE: `SalesSystem/SalesSystem.Infrastructure/DependencyInjection.cs`, `SalesSystem/SalesSystem.Api/Program.cs`

- [ ] T015 [P] [US1] Add `POST /api/v1/admin/backup` endpoint to trigger backup immediately (admin-only convenience endpoint). Calls `IBackupService.BackupNowAsync`. Returns 200 OK with `Result` — FILE: `SalesSystem/SalesSystem.Api/Controllers/AdminController.cs` (create if not exists)

**Checkpoint**: Background worker starts on API launch. Manual `POST /api/v1/admin/backup` → `.bak` file created. Old `.bak` files beyond retention window are cleaned up.

---

## Phase 6: US4 — Desktop Connectivity Error Handling (Priority: P2)

**Goal**: Desktop shows a styled Arabic error dialog on startup when the API is unreachable. User can retry or exit gracefully.

**Independent Test**: Stop the API → Launch the Desktop → `DatabaseErrorDialog` appears within 5 seconds with "إعادة المحاولة" and "إغلاق" buttons → Start the API → Click "إعادة المحاولة" → Main window loads normally.

- [ ] T016 [P] [US4] Create `IHealthApiService` interface + `HealthApiService` using `IHttpClientFactory`. Method: `Task<bool> CheckHealthAsync(CancellationToken ct)`. Uses `GET http://localhost:{port}/api/health` with 5-second `CancellationToken`. Returns `true` on 200, `false` on any error or timeout — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/Api/HealthApiService.cs`

- [ ] T017 [P] [US4] Create `DatabaseErrorDialog.xaml` + `DatabaseErrorDialogViewModel.cs`. Orange/red theme, warning icon ⚠. Text: `"تعذر الاتصال بالخادم. يرجى التحقق من تشغيل الخدمة والمحاولة مرة أخرى."`. Buttons: `"إعادة المحاولة"` and `"إغلاق"`. ViewModel exposes `RetryCommand` and `CloseCommand`. CloseCommand calls `Application.Current.Shutdown()` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Dialogs/DatabaseErrorDialog.xaml` + `ViewModels/Dialogs/DatabaseErrorDialogViewModel.cs`

- [ ] T018 [US4] Modify `App.xaml.cs.OnStartup`: Before `MainWindow` is created, create a `CancellationTokenSource(TimeSpan.FromSeconds(5))`. Call `healthApiService.CheckHealthAsync(cts.Token)`. If `false`, show `DatabaseErrorDialog` as a retry loop (keep retrying until success or user closes). Only after a successful health check, proceed to create and show `MainWindow` — FILE: `SalesSystem/SalesSystem.DesktopPWF/App.xaml.cs`

- [ ] T019 [US4] Register `IHealthApiService` → `HealthApiService` in Desktop DI — FILE: `SalesSystem/SalesSystem.DesktopPWF/App.xaml.cs`

**Checkpoint**: Stop API → Launch Desktop → Arabic error dialog appears → Start API → Retry → Main window loads.

---

## Phase 7: US5 — Silent Auto-Update (Priority: P2)

**Goal**: Desktop checks for updates in background on startup, downloads and SHA256-verifies silently, notifies user to restart. Entire check must complete or timeout within 8 seconds.

**Independent Test**: Set `Update.ServerUrl` → Host `update-manifest.json` with a higher version number → Launch Desktop → Toast appears: `"تحديث جاهز — أعد تشغيل البرنامج"`. Host manifest with wrong SHA256 → Launch Desktop → No toast, partial download file deleted.

- [ ] T020 [US5] Implement `UpdateService`. Inject `IHttpClientFactory`, `IToastNotificationService`, `ISystemSettingsApiService`, `ILogger`. Method `Task CheckAndUpdateAsync(CancellationToken ct)`. Logic: (1) Read `Update.ServerUrl` from settings. If empty, return. (2) `using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8))`. (3) `GET {url}/update-manifest.json` with combined `CancellationToken`. (4) Deserialize to `UpdateManifest`. (5) Compare `manifest.Version` with `Assembly.GetEntryAssembly().GetName().Version.ToString()`. If same or older, return. (6) `GET {manifest.DownloadUrl}` → save bytes to `Path.GetTempFileName() + ".exe"`. (7) Compute `SHA256.HashData(fileBytes)` → compare with `manifest.Sha256Hash` (hex string). (8) If mismatch: `File.Delete(tempPath)`, log Serilog warning, return. (9) If match: `_toastService.ShowInfo("تحديث جاهز — أعد تشغيل البرنامج")`. Wrap entire method in try/catch — failure is always silent (log only) — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/App/UpdateService.cs`

- [ ] T021 [US5] After `MainWindow` is shown in `App.xaml.cs.OnStartup`, fire `Task.Run(() => _updateService.CheckAndUpdateAsync(CancellationToken.None))` — no await, no blocking. Register `UpdateService` as singleton in DI — FILE: `SalesSystem/SalesSystem.DesktopPWF/App.xaml.cs`

**Checkpoint**: Update URL configured → Desktop loads normally (update runs in background) → Toast appears if update available → Wrong SHA256 → No toast, temp file deleted.

---

## Phase 8: Polish

- [ ] T022 [P] Add backup settings fields to `SettingsView.xaml` and `SettingsViewModel.cs`: `BackupPath` (TextBox + folder browse button), `BackupScheduleTime` (TextBox HH:mm), `BackupRetentionDays` (TextBox numeric), `UpdateServerUrl` (TextBox). SaveCommand calls existing SystemSettings update endpoint — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Settings/SettingsView.xaml` + `ViewModels/SettingsViewModel.cs`

- [ ] T023 [P] Verify all Serilog logging: check that `DpapiConnectionStringProtector`, `BackupService`, `ScheduledBackupWorker`, and `UpdateService` all use Serilog `ILogger<T>` and that no decrypted connection string or password appears in any log message (RULE-035, RULE-037)

- [ ] T024 [P] Create `sc.exe` installation script: `install-service.ps1` that runs `sc.exe create`, `sc.exe failure` with the 3-restart recovery policy, and `sc.exe start` — FILE: `SalesSystem/scripts/install-service.ps1`

- [ ] T025 Update `docs/CHANGELOG.md` with v4.4 entry: Production Hardening (DPAPI encryption, Windows Service, daily backup, Desktop health check dialog, silent auto-update) — FILE: `docs/CHANGELOG.md`

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
