# Feature Specification: Phase 7 — Production Readiness

**Feature Branch**: `007-production-readiness`  
**Created**: 2026-05-23  
**Status**: Draft  
**Input**: User description: "Phase 7 — Production Readiness"

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - System Installs on a Clean Windows Machine (Priority: P1)

An administrator receives a fresh Windows machine with no prior software. They run the SalesSystem installer and the system sets itself up completely — the API service starts automatically, the database is created, seed data is inserted, and the desktop client connects to the running API without any manual configuration.

**Why this priority**: This is the entry point for every new customer. If installation fails, the product cannot be used at all.

**Independent Test**: Install the `.exe` or `.msi` on a clean Windows VM, reboot, and verify the desktop client opens and reaches the login screen within 2 minutes.

**Acceptance Scenarios**:

1. **Given** a clean Windows machine (no .NET, no SQL Server installed), **When** the administrator runs the installer, **Then** the application and all prerequisites are installed and the API service starts within 3 minutes.
2. **Given** the installer completes, **When** the machine reboots, **Then** the API Windows Service (`SalesSystemService`) starts automatically without manual intervention.
3. **Given** first run after installation, **When** the desktop client opens, **Then** the system detects the database, creates it if absent, inserts seed data (admin user, default warehouse, cash customer, default units), and presents the login screen.

---

### User Story 2 - Admin Performs and Restores a Database Backup (Priority: P1)

An administrator initiates a manual backup from within the desktop Settings screen. A `.bak` file is created in the configured backup folder. Later, they restore the database from a backup file and, after re-logging in, verify that all historical data (invoices, stock, balances) is intact.

**Why this priority**: Data loss is catastrophic for a retail shop. Backup and restore are the primary safety net.

**Independent Test**: Create a backup via the Settings screen, drop the database manually, then restore via the restore function, and confirm all data is present after login.

**Acceptance Scenarios**:

1. **Given** the user is logged in as Admin, **When** they trigger "Backup Database" from the Settings screen, **Then** a timestamped `.bak` file is created in the configured backup directory within 30 seconds and a success confirmation is shown.
2. **Given** a valid `.bak` file exists, **When** the Admin triggers "Restore Database" and confirms the warning prompt, **Then** the database is restored, the application prompts for re-login, and all previously entered data is accessible after login.
3. **Given** the scheduled daily backup is configured, **When** the system clock reaches 2:00 AM, **Then** a backup is automatically created without user intervention and old backups beyond the retention period are deleted.
4. **Given** a corrupt or incompatible `.bak` file is selected for restore, **When** the restore process begins, **Then** the system reports a clear Arabic error message, rolls back to a safe multi-user state, and leaves the original database intact.

---

### User Story 3 - Admin Manages System Settings and User Accounts (Priority: P2)

An administrator accesses the Settings screen to configure the store name, contact details, default tax rate, and default warehouse. They also create, edit, and deactivate user accounts (Cashier, Manager roles) through the User Management screen. Role-based restrictions prevent Cashiers and Managers from accessing the Settings and User Management screens.

**Why this priority**: Without correct settings and user management, the system cannot be tailored for the specific store and access control cannot be enforced.

**Independent Test**: Log in as Admin, create a new Manager user, log in as that Manager, and verify Settings and User Management screens are not accessible.

**Acceptance Scenarios**:

1. **Given** the user is logged in as Admin, **When** they open Settings and update the store name and phone number, **Then** the changes are saved and reflected in invoice print headers immediately.
2. **Given** the user is logged in as Admin, **When** they create a new user with role Cashier and a password, **Then** the user can log in with those credentials and cannot access Admin-only screens.
3. **Given** the user is logged in as Manager or Cashier, **When** they attempt to navigate to Settings or User Management, **Then** those screens are not visible in the sidebar and direct navigation is blocked with a 403 response from the API.
4. **Given** an existing user account, **When** the Admin deactivates it, **Then** that user can no longer log in and their historical invoice records remain linked (no data loss).

---

### User Story 4 - Connection String is Secured at First Run (Priority: P2)

On the very first launch after installation, the system automatically detects that the database connection string is unencrypted, encrypts it using the machine's built-in data protection (DPAPI), and writes it back to the configuration file. Subsequent launches use the encrypted string transparently.

**Why this priority**: Storing plain-text credentials in a configuration file is a security risk for a local retail environment, especially on shared workstations.

**Independent Test**: Inspect `appsettings.json` before and after first launch; verify the connection string value starts with `DPAPI:` after first run, and that the application still connects successfully.

**Acceptance Scenarios**:

1. **Given** a fresh installation with a plain-text connection string in the config file, **When** the application starts for the first time, **Then** the connection string is encrypted and stored with a `DPAPI:` prefix without any user action.
2. **Given** the connection string is already encrypted (starts with `DPAPI:`), **When** the application starts again, **Then** it decrypts the string transparently and connects to the database without re-encrypting.
3. **Given** the encrypted connection string is copied to a different machine, **When** that machine attempts to start the API, **Then** decryption fails and a clear error message guides the administrator to re-configure the connection string.

---

### User Story 5 - Auto-Update Notifies and Applies Updates Silently (Priority: P3)

When the desktop client starts, it checks in the background for a newer version. If a new version is available and passes integrity verification, it prompts the user with the option to install now or skip this version. The update never blocks the login flow.

**Why this priority**: Keeping installations up-to-date is important but must never interrupt daily operations.

**Independent Test**: Simulate a newer version available in the update source, start the desktop client, confirm login proceeds immediately, and verify the update prompt appears in the background.

**Acceptance Scenarios**:

1. **Given** a newer version is available, **When** the desktop client starts, **Then** the login screen appears immediately (update check does not block it) and the update prompt appears after login within 8 seconds.
2. **Given** an update file is available, **When** the SHA256 checksum of the downloaded file does not match the manifest, **Then** the update is silently discarded with a logged warning and no installation proceeds.
3. **Given** the user selects "Skip this version", **When** the application restarts next time, **Then** the skipped version is not prompted again, but a subsequent newer version will be prompted.
4. **Given** the update server is unreachable, **When** the desktop starts, **Then** the update check times out silently within 8 seconds, no error is shown to the user, and login proceeds normally.

---

### Edge Cases

- What happens when the backup destination folder does not exist or is read-only? → The system reports a clear Arabic error and does not crash.
- What happens when a restore is attempted while another user is connected to the database? → The system forces single-user mode with a 30-second grace period, then proceeds.
- What happens when the Windows Service fails to start after installation? → The installer logs the failure and the desktop client shows a "Service not running" dialog with a retry option.
- What happens when a user account password is changed while that user is logged in? → The existing session remains valid until the JWT expires (8 hours); subsequent logins require the new password.
- What happens if the daily scheduled backup fails at 2 AM? → The failure is logged to the system event log and the next scheduled attempt runs the following day.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide an installer that sets up the API Windows Service, database, and desktop client on a clean Windows machine without requiring manual configuration steps.
- **FR-002**: The API MUST run as a Windows Service named `SalesSystemService` that starts automatically on machine boot and restarts automatically on failure (3 retry attempts with escalating delays: 1 min, 5 min, 15 min).
- **FR-003**: On first run, the database MUST be automatically created and seeded with default data (admin user, default warehouse, default cash customer, standard product units).
- **FR-004**: The Settings screen MUST allow Admin users to configure: store name, phone number, address, store logo (file path), default tax rate, tax toggle, default warehouse, and costing method.
- **FR-005**: The User Management screen MUST allow Admin users to create, edit, and deactivate user accounts with roles (Admin/Manager/Cashier). Users MUST be soft-deleted only — historical invoice records must remain intact.
- **FR-006**: All Admin-only screens (Settings, User Management, Backup, Warehouses) MUST be invisible in the sidebar and return 403 from the API when accessed by Manager or Cashier roles.
- **FR-007**: The system MUST allow manual database backup via the Settings screen, producing a timestamped `.bak` file in the configured backup directory.
- **FR-008**: The system MUST support automated daily backup at 2:00 AM, with configurable retention period (default 30 days). Backups older than the retention period MUST be automatically deleted.
- **FR-009**: The system MUST allow database restore from a `.bak` file, forcing single-user mode with a 30-second grace period. After restore, the system MUST prompt for re-login.
- **FR-010**: The connection string stored in the configuration file MUST be encrypted using machine-bound DPAPI on first run, transparently decrypted on all subsequent startups.
- **FR-011**: The system MUST check for application updates in the background on desktop startup, without blocking or delaying the login screen.
- **FR-012**: Downloaded update files MUST be verified using SHA256 checksum before installation. Failed verification MUST silently discard the file and log a warning.
- **FR-013**: Users MUST be able to skip a specific version; that version MUST NOT be prompted again on subsequent startups.
- **FR-014**: The update check MUST time out within 8 seconds if the update server is unreachable, with no error shown to the user.
- **FR-015**: The API MUST expose health check endpoints that allow the desktop client to verify database connectivity before showing the login screen.
- **FR-016**: On database connection failure at startup, the desktop MUST show a styled "Database Unavailable" dialog with Retry and Exit options — never a raw error message.
- **FR-017**: All backup and restore operations MUST only be accessible to users with the Admin role.

### Key Entities

- **StoreSettings**: Store name, address, phone, logo path, default tax rate, tax toggle, default warehouse ID, costing method — single-row configuration table.
- **SystemSettings**: Key-value pairs for application-level settings (costing method, print settings, backup retention days).
- **User**: Authentication account with role, active status, and audit trail. Soft-delete only.
- **Backup Record** (implicit): Timestamped `.bak` files managed on disk; metadata logged to system event log.
- **UpdateManifest**: Remote JSON file describing the latest version, download URL, and SHA256 hash.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new installation completes successfully on a clean Windows machine in under 5 minutes, with the desktop login screen reachable at the end.
- **SC-002**: The API Windows Service starts automatically within 30 seconds of machine boot and remains running without manual intervention for 30 consecutive days.
- **SC-003**: Manual database backup completes within 30 seconds for a database containing 1 year of operational data (invoices, stock movements, payments).
- **SC-004**: A database restore from a `.bak` file completes within 60 seconds and 100% of previously entered records are accessible after re-login.
- **SC-005**: The desktop login screen appears within 3 seconds of application launch, regardless of whether an update is available.
- **SC-006**: 100% of Admin-only operations (settings, user management, backup, restore) return 403 when attempted by non-Admin roles via the API.
- **SC-007**: The connection string is encrypted (starts with `DPAPI:`) on all production installations; no plain-text credentials appear in configuration files after first run.
- **SC-008**: The scheduled 2 AM backup runs successfully for 30 consecutive days without manual intervention on a Windows machine where the API runs as a service.
- **SC-009**: When the update server is unreachable, the desktop client proceeds to the login screen within 8 seconds with no visible error to the user.
- **SC-010**: Zero hard-deleted user accounts — all deactivated users retain linkage to their historical invoice records.

---

## Assumptions

- The target deployment environment is a Windows 10 or Windows 11 machine with internet access for update checks and sufficient local disk space for backups.
- SQL Server Express (free tier) is bundled with or pre-installed alongside the application; the installer either includes it or validates its presence.
- The installer is built using Inno Setup and distributed as a single `.exe` setup file.
- The backup directory defaults to a local path (e.g., `C:\SalesSystemBackups`) and the Admin is responsible for ensuring the path has write permissions.
- The auto-update source is a GitHub Releases page; the update manifest is a JSON file in a known public location.
- DPAPI encryption is machine-bound, meaning connection string configuration must be re-done when migrating to a new server machine.
- The `.NET 10 runtime` is bundled as a self-contained deployment — end users do not need to install .NET separately.
- Mobile version support is explicitly out of scope for this phase.
