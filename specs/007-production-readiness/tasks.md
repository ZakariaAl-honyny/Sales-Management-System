# Tasks: Phase 7 — Production Readiness

**Spec**: `specs/007-production-readiness/spec.md`  
**Plan**: `specs/007-production-readiness/plan.md`  
**Contracts**: `specs/007-production-readiness/contracts/`  
**AGENTS.md**: Read BEFORE writing any code — rules are LAW.

> **Smaller-model note**: Every task below includes the exact file path, what to implement, and which AGENTS.md rule applies. Follow AGENTS.md §2 (Constitution) strictly. Return `Result<T>` from all services. Use `IDialogService` — never `MessageBox.Show`.

---

## Format: `- [ ] [ID] [P?] [Story?] Description — FILE: path — RULE: X`

---

## Phase 1: Setup

- [ ] T001 Verify solution builds with 0 errors: run `dotnet build SalesSystem/SalesSystem.sln` and fix any existing compilation errors before starting — FILE: `SalesSystem/SalesSystem.sln`
- [ ] T002 [P] Add `Microsoft.Extensions.Hosting.WindowsServices` NuGet to Api project — FILE: `SalesSystem/SalesSystem.Api/SalesSystem.Api.csproj` — needed by T007
- [ ] T003 [P] Add `Serilog.Sinks.EventLog` NuGet to Api project — FILE: `SalesSystem/SalesSystem.Api/SalesSystem.Api.csproj` — RULE-035

---

## Phase 2: Foundational (BLOCKS all user stories)

**⚠️ Complete and verify before any Phase 3+ work.**

- [ ] T004 Add `IConnectionStringProtector` interface with methods `bool IsEncrypted(string)`, `string Protect(string)`, `string Unprotect(string)` — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Services/IConnectionStringProtector.cs`

- [ ] T005 [P] Implement `ConnectionStringProtector` using `System.Security.Cryptography.ProtectedData` with `DataProtectionScope.LocalMachine`. Prefix = `"DPAPI:"`. `Protect()` → base64 of encrypted bytes. `Unprotect()` → strip prefix, decrypt — FILE: `SalesSystem/SalesSystem.Infrastructure/Services/ConnectionStringProtector.cs` — RULE-040

- [ ] T006 [P] Add `GET /api/v1/health` and `GET /api/v1/health/database` endpoints (no `[Authorize]`). Health checks `DbContext.Database.CanConnectAsync()`. Returns `200 {"status":"healthy","database":"connected"}` or `503` — FILE: `SalesSystem/SalesSystem.Api/Controllers/HealthController.cs` — RULE-025

- [ ] T007 In `Program.cs`: call `builder.Host.UseWindowsService()`. Add Serilog EventLog sink when `WindowsServiceHelpers.IsWindowsService()` is true. Register `IConnectionStringProtector` as singleton. Register `AdminOnly` authorization policy (`Role == "1"`) — FILE: `SalesSystem/SalesSystem.Api/Program.cs` — RULE-038

**Checkpoint**: `dotnet build` passes. Health endpoint returns 200.

---

## Phase 3: User Story 1 — Clean Windows Installer (P1) 🎯

**Goal**: App installs on clean Windows machine; service starts automatically; DB seeded.  
**Test**: Install `.exe` on clean VM → reboot → desktop reaches login screen within 3 min.

- [ ] T008 [US1] Create `DbSeeder.cs` with `SeedAsync(SalesDbContext db)`. Check if admin user exists before inserting. Seed: 1 Admin user (`admin`/BCrypt hash of `Admin@123`, work factor 12), 1 default Warehouse (`IsDefault=true`), 1 Cash Customer (`Name="نقدي"`, `CurrentBalance=0`), base units (Piece with `ConversionFactor=1`) — FILE: `SalesSystem/SalesSystem.Infrastructure/Data/DbSeeder.cs` — RULE-039

- [ ] T009 [US1] In `Program.cs` startup, after `app.Build()`: call `db.Database.MigrateAsync()` then `DbSeeder.SeedAsync(db)` inside a try/catch that logs failures with Serilog but does NOT crash the service — FILE: `SalesSystem/SalesSystem.Api/Program.cs` — RULE-035, RULE-036

- [ ] T010 [US1] Set `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`, `<SelfContained>true</SelfContained>`, `<PublishSingleFile>true</PublishSingleFile>` in Api `.csproj` for release publish — FILE: `SalesSystem/SalesSystem.Api/SalesSystem.Api.csproj`

- [ ] T011 [US1] Write `Installer/setup.iss` Inno Setup script. `[Run]` section must: (1) `sc create SalesSystemService binPath="..." start=auto`, (2) `sc failure SalesSystemService reset=86400 actions=restart/60000/restart/300000/restart/900000`. Include desktop client shortcut in `[Icons]` — FILE: `Installer/setup.iss`

**Checkpoint**: Service registers and starts automatically after install.

---

## Phase 4: User Story 2 — Database Backup & Restore (P1)

**Goal**: Manual backup, scheduled 2 AM backup, restore with single-user mode.  
**Test**: Backup → drop DB → restore → all data accessible after re-login.

- [ ] T012 [US2] Add interface with: `Task<Result<string>> BackupAsync(CancellationToken ct)`, `Task<Result> RestoreAsync(string fileName, CancellationToken ct)`, `Task<Result<List<BackupFileDto>>> ListBackupsAsync()` — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Services/IBackupService.cs` — RULE-006

- [ ] T013 [US2] Implement `BackupService`. Backup SQL: `BACKUP DATABASE [{db}] TO DISK=N'{path}' WITH FORMAT,INIT,STATS=10`. Restore SQL: first `ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK AFTER 30`, then `RESTORE DATABASE ... WITH REPLACE,STATS=5`, then `ALTER DATABASE [{db}] SET MULTI_USER`. On restore failure catch: run `TrySetMultiUserAsync()`. Backup filename: `SalesSystem_{yyyyMMdd}_{HHmmss}.bak`. Retention cleanup: delete `.bak` files beyond configured `BackupRetentionDays` — FILE: `SalesSystem/SalesSystem.Infrastructure/Services/BackupService.cs` — RULE-003

- [ ] T014 [US2] Implement `ScheduledBackupWorker : BackgroundService`. In `ExecuteAsync`: calculate delay to next 02:00 AM, `await Task.Delay(delay, ct)`, then call `IBackupService.BackupAsync()`. Loop. Log result with Serilog — FILE: `SalesSystem/SalesSystem.Api/BackgroundServices/ScheduledBackupWorker.cs` — RULE-035

- [ ] T015 [US2] Add `BackupController` with `[Authorize(Policy="AdminOnly")]`. Endpoints: `GET /api/v1/backup/list`, `POST /api/v1/backup`, `POST /api/v1/backup/restore` (body: `{ "fileName": "..." }`). FluentValidation: fileName NotEmpty, ends with `.bak` — FILE: `SalesSystem/SalesSystem.Api/Controllers/BackupController.cs` — RULE-025, RULE-038

- [ ] T016 [US2] Add `IBackupApiService` interface and `BackupApiService` implementation using `IHttpClientFactory`. Methods: `ListBackupsAsync()`, `BackupNowAsync()`, `RestoreAsync(string fileName)`. All return `Result<T>` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/Api/BackupApiService.cs` — RULE-007

- [ ] T017 [US2] Build `BackupView.xaml` (DataGrid of backup files + two buttons: "إنشاء نسخة احتياطية الآن" / "استعادة من النسخة المحددة") and `BackupViewModel.cs`. On restore: call `await _dialogService.ShowConfirmationAsync(...)` first. On success: call `_dialogService.ShowSuccessAsync(...)` then force logout. ViewModel calls `SetDialogService()` in constructor — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Settings/BackupView.xaml` + `ViewModels/Settings/BackupViewModel.cs` — RULE-054, RULE-055

**Checkpoint**: Manual backup creates `.bak` file. Restore brings back all data.

---

## Phase 5: User Story 3 — Settings & User Management (P2)

**Goal**: Admin can edit store settings, manage users (soft-delete only, last-admin guard).  
**Test**: Create Cashier user → login as Cashier → Settings screen not visible → 403 from API.

- [ ] T018 [US3] Add `UpdateSettingsRequest` DTO and `UpdateSettingsRequestValidator` (StoreName NotEmpty MaxLength 200, DefaultTaxRate `>=0 <=100`, CostingMethod InclusiveBetween 1–3) — FILE: `SalesSystem/SalesSystem.Api/Validators/UpdateSettingsRequestValidator.cs` — RULE-044

- [ ] T019 [US3] Update `SettingsController`: `GET /api/v1/settings` and `PUT /api/v1/settings`, both `[Authorize(Policy="AdminOnly")]`. Response includes `costingMethod` field. Service call returns `Result<T>` — FILE: `SalesSystem/SalesSystem.Api/Controllers/SettingsController.cs` — RULE-025

- [ ] T020 [US3] Add to `UsersController`: `GET /api/v1/users?includeInactive=bool`, `POST /api/v1/users`, `PUT /api/v1/users/{id}`, `DELETE /api/v1/users/{id}` (soft delete only — `IsActive=false`), `PUT /api/v1/users/{id}/restore`. All `[Authorize(Policy="AdminOnly")]` — FILE: `SalesSystem/SalesSystem.Api/Controllers/UsersController.cs` — RULE-038

- [ ] T021 [US3] In `UserService.DeactivateAsync()`: count active Admins first. If count == 1 and user is Admin → `return Result.Failure("لا يمكن إلغاء تفعيل آخر مسؤول في النظام")`. Never hard-delete — FILE: `SalesSystem/SalesSystem.Application/Services/UserService.cs` — RULE-006, RULE-012 (constitution XII)

- [ ] T022 [P] [US3] Verify/update `SettingsApiService` has `GetSettingsAsync()` and `UpdateSettingsAsync(UpdateSettingsRequest)` returning `Result<T>` via `IHttpClientFactory` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/Api/SettingsApiService.cs` — RULE-007

- [ ] T023 [P] [US3] Verify/update `UserApiService` has `GetAllAsync(bool includeInactive)`, `CreateAsync(CreateUserRequest)`, `UpdateAsync(int id, UpdateUserRequest)`, `DeactivateAsync(int id)`, `RestoreAsync(int id)` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/Api/UserApiService.cs`

- [ ] T024 [US3] Build `UsersListView.xaml` + `UsersListViewModel.cs`. DataGrid columns: Id, FullName, UserName, RoleName, IsActive badge, CreatedAt. Toolbar: "مستخدم جديد" button opens `UserEditorView` via `ScreenWindowService.OpenScreen<UserEditorView, UserEditorViewModel>()`. Subscribe to `UserChangedMessage` on EventBus, unsubscribe in `Dispose()` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Users/UsersListView.xaml` + `ViewModels/Users/UsersListViewModel.cs` — RULE-012, RULE-013

- [ ] T025 [US3] Build `UserEditorView.xaml` + `UserEditorViewModel.cs`. Fields: FullName*, UserName*, Password* (PasswordBox), Role ComboBox. Call `SetDialogService()` in constructor. `SaveCommand` always enabled. `Validate()` method shows `_dialogService.ShowWarningAsync(...)` listing all missing fields. On success: `_toastService.ShowSuccess("تم حفظ المستخدم")` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Users/UserEditorView.xaml` + `ViewModels/Users/UserEditorViewModel.cs` — RULE-054, RULE-059

- [ ] T026 [US3] Update `SettingsView.xaml` + `SettingsViewModel.cs` to add CostingMethod radio buttons (3 options per `ui-contracts.md`) and bind to API. Load on navigate, save via `PUT /api/v1/settings` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Settings/SettingsView.xaml` + `ViewModels/SettingsViewModel.cs`

- [ ] T027 [US3] Hide Settings and User Management sidebar items when `SessionService.CurrentUser.Role != Admin`. Sidebar XAML uses `Visibility` converter bound to role — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/MainWindow.xaml` — RULE-038

**Checkpoint**: Non-admin users cannot see or access Settings/Users screens.

---

## Phase 6: User Story 4 — DPAPI First-Run Encryption (P2)

**Goal**: Connection string auto-encrypted on first run; transparent decryption after.  
**Test**: Plain-text string in `appsettings.json` → start API → string becomes `DPAPI:...` → app connects.

- [ ] T028 [US4] Implement `FirstRunSetupService`. In `RunAsync()`: read current connection string from `appsettings.json`. If `!IsEncrypted(value)`: call `Protect(value)`, write back using atomic pattern (write to `.tmp` file → `File.Replace(tmp, target, backup)`). Use `System.Text.Json` to read/write — FILE: `SalesSystem/SalesSystem.Infrastructure/Services/FirstRunSetupService.cs` — RULE-040

- [ ] T029 [US4] Register and call `FirstRunSetupService.RunAsync()` in `Program.cs` BEFORE `builder.Build()`. Then configure `DbContext` connection string: if encrypted, call `Unprotect()` first — FILE: `SalesSystem/SalesSystem.Api/Program.cs`

- [ ] T030 [US4] In `App.xaml.cs` `OnStartup`: call `IDatabaseHealthCheckService.CheckAsync()` (up to 3 retries with 2s delay). On failure: show `DatabaseErrorDialog` (Retry/Exit). Only show `MainWindow` after health check passes — FILE: `SalesSystem/SalesSystem.DesktopPWF/App.xaml.cs` + `Views/Common/DatabaseErrorDialog.xaml` — RULE-054

**Checkpoint**: `appsettings.json` shows `DPAPI:` prefix after first run. App connects normally.

---

## Phase 7: User Story 5 — Silent Auto-Update (P3)

**Goal**: Background update check on startup; SHA256 verified; never blocks login.  
**Test**: Mock manifest with newer version → login appears immediately → prompt appears after login.

- [ ] T031 [US5] Implement `AutoUpdateService`. `CheckAndOfferUpdateAsync()`: use `CancellationTokenSource(TimeSpan.FromSeconds(8))`. Fetch manifest JSON from configured URL. Compare `System.Version.Parse(manifest.Version) > currentVersion`. Download file. Compute SHA256 and compare to manifest hash. If mismatch: `Log.Warning("SHA256 mismatch")` and return. Read/write `skippedVersion` from `%AppData%\SalesSystem\settings.json` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/App/AutoUpdateService.cs` — RULE-035, RULE-037

- [ ] T032 [US5] Build `UpdateAvailableDialog.xaml` + `UpdateAvailableViewModel.cs`. Three buttons: "تثبيت الآن" (launches installer via `Process.Start()`), "تذكيري لاحقاً" (dismiss), "تخطي هذا الإصدار" (writes `skippedVersion` to settings.json) — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Common/UpdateAvailableDialog.xaml`

- [ ] T033 [US5] In `App.xaml.cs` `OnStartup`: launch `_ = Task.Run(() => _autoUpdateService.CheckAndOfferUpdateAsync())` — fire and forget, never awaited on startup thread. Show update dialog on `Application.Current.Dispatcher` after check completes — FILE: `SalesSystem/SalesSystem.DesktopPWF/App.xaml.cs`

**Checkpoint**: Login screen appears in < 3s regardless of update server reachability.

---

## Phase 8: Polish & Cross-Cutting

- [ ] T034 [P] Register all new services in DI: `BackupApiService`, `AutoUpdateService`, `BackupViewModel`, `UsersListViewModel`, `UserEditorViewModel` in `App.xaml.cs`. Register `IBackupService`, `ScheduledBackupWorker`, `IConnectionStringProtector` in `Program.cs` — FILE: `SalesSystem/SalesSystem.DesktopPWF/App.xaml.cs` + `SalesSystem/SalesSystem.Api/Program.cs`

- [ ] T035 [P] Add `CreateUserRequestValidator` (FullName NotEmpty MaxLength 150, UserName NotEmpty MaxLength 100 no spaces, Password MinLength 8, Role 1–3) — FILE: `SalesSystem/SalesSystem.Api/Validators/CreateUserRequestValidator.cs` — RULE-044

- [ ] T036 Verify all new endpoints have `[Authorize]`. Verify all new service methods return `Result<T>`. Verify no `MessageBox.Show` anywhere in Desktop. Search with: `grep -r "MessageBox.Show" SalesSystem/SalesSystem.DesktopPWF/` — RULE-038, RULE-006, RULE-055

- [ ] T037 [P] Update `docs/CHANGELOG.md` and `docs/MASTER-PLAN.md` version history with v4.6.4 entry covering: Windows Service, DPAPI, Backup/Restore, User Management, Auto-Update, Inno Setup installer

---

## Dependencies & Execution Order

| Phase | Depends On | Can Parallel With |
|-------|-----------|-------------------|
| Phase 1 (Setup) | — | All T002, T003 in parallel |
| Phase 2 (Foundational) | Phase 1 | T005, T006 in parallel |
| Phase 3 (US1 Installer) | Phase 2 | Phase 4 (US2) |
| Phase 4 (US2 Backup) | Phase 2 | Phase 3 (US1) |
| Phase 5 (US3 Settings) | Phase 2 | Phase 6 (US4) |
| Phase 6 (US4 DPAPI) | Phase 2 | Phase 5 (US3) |
| Phase 7 (US5 Update) | Phase 2 | After P1 stories |
| Phase 8 (Polish) | All phases | T034, T035 in parallel |

---

## Implementation Strategy

### MVP (P1 only — deliver first)
1. Phase 1 + Phase 2 (foundation)
2. Phase 4 (US2 — Backup/Restore) — highest business value
3. Phase 3 (US1 — Installer)

### Full Release
4. Phase 6 (US4 — DPAPI security)
5. Phase 5 (US3 — Settings/Users)
6. Phase 7 (US5 — Auto-update)
7. Phase 8 (Polish + DI wiring)

---

## Key Patterns (copy into implementation)

```csharp
// Every service method — RULE-006
public async Task<Result<T>> DoSomethingAsync(CancellationToken ct)
{
    try { ... return Result<T>.Success(value); }
    catch (Exception ex) { _logger.LogError(ex, "..."); return Result<T>.Failure("حدث خطأ"); }
}

// Every Editor ViewModel constructor — RULE-054, RULE-059
public MyEditorViewModel(IDialogService dialogService, ...)
{
    SetDialogService(dialogService);
    SaveCommand = new AsyncRelayCommand(SaveAsync);  // NO CanExecute predicate
}

// EventBus subscription — RULE-012
_subscription = _eventBus.Subscribe<EntityChangedMessage>(_ => _ = LoadAsync());
public void Dispose() { _subscription?.Dispose(); }
```
