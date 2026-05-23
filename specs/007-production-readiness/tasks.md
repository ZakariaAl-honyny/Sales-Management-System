# Tasks: Phase 7 — Production Readiness

**Input**: Design documents from `specs/007-production-readiness/`
**Prerequisites**: plan.md (required), spec.md (required), data-model.md, contracts/api-endpoints.md, contracts/ui-contracts.md

This task list is organized by User Story to support incremental implementation, TDD workflows, and independent validation of each release increment.

---

## Format: `[ID] [P?] [Story?] Description with exact file paths`

- **[P]**: Parallelizable (touches different files, no dependencies)
- **[Story]**: Maps to user stories [US1] through [US5] from `spec.md`
- **Checklist format**: Starts with `- [ ]` and includes exact file targets

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initial project alignment and environment synchronization.

- [ ] T001 Create project folders and prepare shared setup folders in `c:\Users\ALlahabi\Desktop\Sales Management System`
- [ ] T002 [P] Verify the .NET 10 compilation configuration targets and solution files in `SalesSystem/SalesSystem.slnx`
- [ ] T003 [P] Setup linting profiles and ignore entries in `.specify/init-options.json` and `.gitignore`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core security, database health, and host infrastructure required by all subsequent features.

*⚠️ **CRITICAL CHECKPOINT**: No user story work can begin until this phase is complete and verified.*

- [ ] T004 Implement database connectivity health checks in `SalesSystem/SalesSystem.Api/Controllers/HealthController.cs`
- [ ] T005 [P] Create connection string security interface and DPAPI-based encryption service in `SalesSystem/SalesSystem.Infrastructure/Services/ConnectionStringProtector.cs`
- [ ] T006 [P] Implement `SecureDbContextFactory` with transparent connection decryption in `SalesSystem/SalesSystem.Infrastructure/Data/SecureDbContextFactory.cs`
- [ ] T007 Configure Windows Service hosting pipeline, Serilog EventLog sink, and SQL startup retry policy in `SalesSystem/SalesSystem.Api/Program.cs`

**Checkpoint**: Foundation ready - database verification and configuration security infrastructure are online.

---

## Phase 3: User Story 1 - System Installs on a Clean Windows Machine (Priority: P1) 🎯 MVP

**Goal**: Seamless installation experience on target machines, including service setup, automatic migrations, and full seed data injection.

**Independent Test**: Install using the `.exe` package on a clean VM. Confirm `SalesSystemService` boots automatically, performs database creation/migration, and enables immediate client connection.

### Implementation for User Story 1

- [ ] T008 [US1] Create `DbSeeder.cs` for idempotent seed data injection (Default admin, warehouse, cash customer, units) in `SalesSystem/SalesSystem.Infrastructure/Data/DbSeeder.cs`
- [ ] T009 [US1] Integrate startup migration execution and database seeding logic in `SalesSystem/SalesSystem.Api/Program.cs`
- [ ] T010 [US1] Configure API project compilation metadata for self-contained Windows service packaging in `SalesSystem/SalesSystem.Api/SalesSystem.Api.csproj`
- [ ] T011 [US1] Build the single-file Inno Setup installer script with automatic service registration in `Installer/setup.iss`

**Checkpoint**: User Story 1 complete - system is fully distributable and installs without manual DB steps.

---

## Phase 4: User Story 2 - Admin Performs and Restores a Database Backup (Priority: P1)

**Goal**: Full system database backup and restore capabilities (manual and daily scheduled) utilizing robust T-SQL with single-user rollback fallback recovery.

**Independent Test**: Execute manual backup, drop local database schemas, run restore from generated `.bak` file, and verify all transactional data is restored.

### Implementation for User Story 2

- [ ] T012 [US2] Declare the backup and restore interfaces in `SalesSystem/SalesSystem.Application/Interfaces/Services/IBackupService.cs`
- [ ] T013 [US2] Implement SQL Server T-SQL backup, single-user mode restore (`ROLLBACK AFTER 30`), and multi-user fallback recovery in `SalesSystem/SalesSystem.Infrastructure/Services/BackupService.cs`
- [ ] T014 [US2] Implement the scheduled daily 2:00 AM background backup job inside `SalesSystem/SalesSystem.Api/BackgroundServices/ScheduledBackupWorker.cs`
- [ ] T015 [US2] Create backup list, manual backup, and restore controller endpoints under `SalesSystem/SalesSystem.Api/Controllers/BackupController.cs`
- [ ] T016 [US2] Implement the desktop backup API client proxy in `SalesSystem/SalesSystem.DesktopPWF/Services/Api/BackupApiService.cs`
- [ ] T017 [US2] Build the Settings Backup/Restore UI view and binding ViewModel in `SalesSystem/SalesSystem.DesktopPWF/Views/Settings/BackupView.xaml` and `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Settings/BackupViewModel.cs`

**Checkpoint**: User Story 2 complete - database backups can be generated manually/automatically and restored safely.

---

## Phase 5: User Story 3 - Admin Manages System Settings and User Accounts (Priority: P2)

**Goal**: Advanced store parameter management and safe administrative account maintenance with soft-delete protections.

**Independent Test**: Log in as Admin, deactivate a cashier, verify login is blocked but cashier's invoice references remain intact, and verify non-admins are denied access.

### Implementation for User Story 3

- [ ] T018 [US3] Create dynamic system configurations data repository inside `SalesSystem/SalesSystem.Infrastructure/Data/Repositories/SystemSettingsRepository.cs`
- [ ] T019 [US3] Create Settings request validators and update controllers in `SalesSystem/SalesSystem.Api/Validators/UpdateSettingsRequestValidator.cs` and `SalesSystem/SalesSystem.Api/Controllers/SettingsController.cs`
- [ ] T020 [US3] Implement full user CRUD endpoints including soft-delete and restore routes in `SalesSystem/SalesSystem.Api/Controllers/UsersController.cs`
- [ ] T021 [US3] Add the last-admin active guard check to UserService in `SalesSystem/SalesSystem.Application/Services/UserService.cs`
- [ ] T022 [US3] Implement user management and settings client services in `SalesSystem/SalesSystem.DesktopPWF/Services/Api/UserApiService.cs` and `SalesSystem/SalesSystem.DesktopPWF/Services/Api/SettingsApiService.cs`
- [ ] T023 [US3] Build the User Management control and ViewModel in `SalesSystem/SalesSystem.DesktopPWF/Views/Users/UsersListView.xaml` and `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Users/UserListViewModel.cs`
- [ ] T024 [US3] Create the Store Settings view and ViewModel supporting costing methods and tax toggles in `SalesSystem/SalesSystem.DesktopPWF/Views/Settings/SettingsView.xaml` and `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Settings/SettingsViewModel.cs`

**Checkpoint**: User Story 3 complete - administrators can manage users and customize store settings securely.

---

## Phase 6: User Story 4 - Connection String is Secured at First Run (Priority: P2)

**Goal**: Automatic first-run connection security wrapping via machine-bound DPAPI.

**Independent Test**: Put cleartext string in `appsettings.json`, start service, confirm it is replaced with `DPAPI:` prefixed key, and app connects successfully.

### Implementation for User Story 4

- [ ] T025 [US4] Implement `FirstRunSetupService` connection string auto-detector and atomic configuration writer in `SalesSystem/SalesSystem.Infrastructure/Services/FirstRunSetupService.cs`
- [ ] T026 [US4] Integrate First-Run protector runner inside API Program pipeline in `SalesSystem/SalesSystem.Api/Program.cs`
- [ ] T027 [US4] Build desktop startup connection validator and `DatabaseErrorDialog` triggers in `SalesSystem/SalesSystem.DesktopPWF/App.xaml.cs` and `SalesSystem/SalesSystem.DesktopPWF/Views/Dialogs/DatabaseErrorDialog.xaml`

**Checkpoint**: User Story 4 complete - database configuration parameters are fully secured at runtime.

---

## Phase 7: User Story 5 - Auto-Update Notifies and Applies Updates Silently (Priority: P3)

**Goal**: Background auto-update detection on client startup with SHA256 checksum validation.

**Independent Test**: Mock a newer version manifest, launch desktop, check that login is immediate (non-blocked) and the background updater shows prompt after 3s.

### Implementation for User Story 5

- [ ] T028 [US5] Implement the `UpdaterService` for version comparisons, downloads, and checksum validations in `SalesSystem/SalesSystem.DesktopPWF/Services/App/UpdaterService.cs`
- [ ] T029 [US5] Create the mock/update manifest endpoints in `SalesSystem/SalesSystem.Api/Controllers/UpdatesController.cs`
- [ ] T030 [US5] Design the Update Dialog window and update prompt bindings in `SalesSystem/SalesSystem.DesktopPWF/Views/Updates/UpdateDialog.xaml` and `SalesSystem/SalesSystem.DesktopPWF/ViewModels/Updates/UpdateDialogViewModel.cs`
- [ ] T031 [US5] Set up background update checking worker registrations inside `SalesSystem/SalesSystem.DesktopPWF/App.xaml.cs`

**Checkpoint**: User Story 5 complete - desktop client supports silent checks and integrity-verified auto-updates.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: High-fidelity logging, code quality validations, and compilation warning resolution.

- [ ] T032 [P] Clean up leftover debug Console writes and integrate Serilog EventLog sink for service modes in `SalesSystem/SalesSystem.Api/Program.cs`
- [ ] T033 Verify decimal arithmetic and domain guard constraints across all new systems to enforce RULE-001, RULE-002, and RULE-009
- [ ] T034 [P] Update and run all unit tests inside `SalesSystem/Tests` ensuring 100% pass rates across Domain, Application, Infrastructure, Api, and Desktop projects
- [ ] T035 [P] Compile production release bundles and verify 0 compilation warnings across all project configurations

---

## Dependencies & Execution Order

### Phase Dependencies
1. **Setup (Phase 1)**: No dependencies - executes immediately.
2. **Foundational (Phase 2)**: Depends on Phase 1 - BLOCKS all User Story work.
3. **User Stories (Phases 3-7)**: Depend on Phase 2 completion. Can execute in priority order: P1 (US1, US2) → P2 (US3, US4) → P3 (US5).
4. **Polish (Phase 8)**: Executes after all target user stories are implemented.

### Parallel Opportunities [P]
* Shared setup (T002, T003) can run in parallel.
* Foundational DPAPI and Health check structures (T004, T005, T006) can execute in parallel.
* Once Phase 2 is complete, US1, US2, and US3 are largely decoupled and can be implemented in parallel if multiple resources are available.
* All polish tasks marked with `[P]` (T032, T034, T035) can be executed concurrently.

---

## Parallel Example: Foundational Phase
```powershell
# Implement independent foundaton modules together:
Task: "T004 Implement database connectivity health checks in HealthController.cs"
Task: "T005 Implement connection string security using DPAPI in ConnectionStringProtector.cs"
```

---

## Implementation Strategy

### MVP Release (P1 Scope)
1. Complete Setup and Foundational prerequisites.
2. Deliver **User Story 1** (Self-contained Installer) and **User Story 2** (Database Backups).
3. Validate clean installation and manual backup/restore behaviors.

### Continuous Security Integration
1. Layer on connection security (**User Story 4**) to lock configuration files.
2. Deploy User administration panels (**User Story 3**) to lock down endpoints.
3. Configure final updates (**User Story 5**) to distribute subsequent builds.
