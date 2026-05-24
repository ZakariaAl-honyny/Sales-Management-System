# Feature Specification: Production Hardening (v4.4)

**Feature Branch**: `011-production-hardening`
**Created**: 2026-05-25
**Status**: Draft
**Input**: User description: "Phase 11 — Production Hardening (v4.4)"

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Automatic Database Backups (Priority: P1)

The system administrator needs to be confident that database backups happen automatically every night without any manual intervention. Each morning, a fresh backup file should exist on the configured backup path. Old backups beyond the configured retention window are deleted automatically to conserve disk space.

**Why this priority**: Data loss is the highest-risk failure in a retail system. Automated daily backups are a non-negotiable baseline for any production deployment.

**Independent Test**: Wait for scheduled backup window (or trigger manually) → Backup `.bak` file appears in the configured path with today's date in the filename → A backup older than the retention limit is no longer present.

**Acceptance Scenarios**:

1. **Given** the system is running at the scheduled time, **When** the backup window arrives, **Then** a new backup file is created in the configured directory with a timestamped filename.
2. **Given** a backup already exists from 31 days ago and retention is set to 30 days, **When** today's backup runs, **Then** the 31-day-old backup file is automatically deleted.
3. **Given** the backup directory does not exist, **When** the backup runs, **Then** the system creates the directory automatically and proceeds.
4. **Given** the backup fails (e.g., disk full), **When** the error occurs, **Then** a failure is recorded in the application log and the system continues running — no crash.

---

### User Story 2 — API as Windows Service (Priority: P1)

The store's IT setup person installs the API to run as a Windows Service so it starts automatically when the server boots and recovers automatically from failures. A separate installer or setup script configures the service. The service runs under a dedicated local account.

**Why this priority**: If the API does not start automatically, the entire system (Desktop + API) is offline after every reboot. This is critical for unattended production environments.

**Independent Test**: Restart the server → API Windows Service starts automatically within 60 seconds → Desktop application connects and functions normally → Kill the service process manually → Service auto-restarts within 30 seconds (Windows auto-recovery).

**Acceptance Scenarios**:

1. **Given** the server reboots, **When** Windows starts, **Then** the API service starts automatically without manual intervention.
2. **Given** the service process crashes unexpectedly, **When** Windows detects the failure, **Then** the service restarts automatically.
3. **Given** the service is running, **When** checked in Windows Services, **Then** the service is listed with the name "SalesSystem API" and status "Running".
4. **Given** the service starts, **When** the health check endpoint is called, **Then** it returns a success status confirming the database is reachable.

---

### User Story 3 — Secure Connection String Encryption (Priority: P1)

The IT administrator secures the database connection string so that it is not stored in plain text in any configuration file. The system encrypts the connection string using machine-bound encryption — meaning the encrypted value can only be decrypted on the same machine it was encrypted on, preventing trivial credential theft if config files are exfiltrated.

**Why this priority**: Plaintext connection strings in config files are a critical security risk, especially in retail environments where configuration files may be accessible to non-technical staff.

**Independent Test**: Inspect `appsettings.json` → Connection string value starts with `"DPAPI:"` prefix → System connects to the database successfully using the encrypted value → Copy the config file to another machine → The system on the second machine CANNOT decrypt and CANNOT connect.

**Acceptance Scenarios**:

1. **Given** the system is configured with a plaintext connection string, **When** the encryption setup runs for the first time, **Then** the connection string in the config file is replaced with a `"DPAPI:"`-prefixed encrypted value.
2. **Given** the config file has a `"DPAPI:"`-prefixed connection string, **When** the API starts, **Then** it decrypts the value in-memory and connects to the database successfully.
3. **Given** the encrypted config file is copied to a different machine, **When** the API on that machine tries to start, **Then** it fails to decrypt and logs an error — it does NOT connect to the database with invalid credentials.
4. **Given** the connection string is already encrypted (starts with `"DPAPI:"`), **When** setup runs again, **Then** it does NOT re-encrypt an already-encrypted value (idempotent).

---

### User Story 4 — Desktop Connectivity Error Handling (Priority: P2)

A cashier opens the desktop application when the server (API) is offline or unreachable. Instead of a crash or blank screen, the application shows a friendly Arabic dialog explaining the connection problem. The dialog offers options: "إعادة المحاولة" (Retry) and "إغلاق" (Exit). If the user clicks Retry and the server is now reachable, the application loads normally.

**Why this priority**: Without this, a server reboot or network hiccup causes the application to freeze or crash at the cashier's station, requiring technical intervention to restart it.

**Independent Test**: Stop the API service → Launch the Desktop → A friendly error dialog appears in Arabic with "إعادة المحاولة" and "إغلاق" buttons → Click "إعادة المحاولة" while API is still down → dialog remains → Start the API → Click "إعادة المحاولة" → Application loads normally.

**Acceptance Scenarios**:

1. **Given** the API is offline, **When** the Desktop starts, **Then** a styled Arabic error dialog appears — no unhandled exception or blank window.
2. **Given** the error dialog is showing, **When** the user clicks "إعادة المحاولة" and the API is still down, **Then** the dialog remains, showing the same options.
3. **Given** the error dialog is showing, **When** the user clicks "إغلاق", **Then** the application exits gracefully.
4. **Given** the error dialog is showing, **When** the user clicks "إعادة المحاولة" and the API is now reachable, **Then** the application loads the main window normally.

---

### User Story 5 — Silent Auto-Update (Priority: P2)

When a new version of the software is available, the Desktop application downloads it in the background and installs it silently without interrupting the cashier's workflow during business hours. The update is verified with a SHA256 hash before installation to ensure integrity. The update process has a timeout so it cannot hang the system.

**Why this priority**: Manual software distribution to retail shops is error-prone and time-consuming. Silent updates reduce maintenance overhead significantly.

**Independent Test**: Publish a new version to the update server → Run the Desktop → Update downloads in the background within 8 seconds → SHA256 hash is verified → Application prompts user to restart for the update to apply → If no update is available, no UI change occurs.

**Acceptance Scenarios**:

1. **Given** a new version is available on the update server, **When** the Desktop checks for updates, **Then** it downloads the update silently in the background without blocking the UI.
2. **Given** the download completes, **When** the SHA256 hash is verified successfully, **Then** the user is prompted to restart the application to apply the update.
3. **Given** the downloaded file's SHA256 does not match the expected hash, **When** verification runs, **Then** the corrupted file is deleted and the update is aborted — the application continues running the current version.
4. **Given** the update server is unreachable, **When** the update check runs, **Then** the check fails silently within 8 seconds and the application continues normally without any user-visible error.
5. **Given** no new version is available, **When** the update check runs, **Then** no UI notification is shown.

---

### Edge Cases

- Backup disk full → Backup fails gracefully; existing backups NOT deleted; error logged.
- API service fails to start (DB unreachable) → Service enters failed state; Windows auto-recovery retries; health check endpoint reports failure clearly.
- DPAPI decryption fails on startup (wrong machine) → API logs a clear error and exits rather than connecting with wrong credentials.
- Update download interrupted → Partial file is discarded; no corrupted installer left behind.
- First run with no encryption yet → System detects plaintext (no `DPAPI:` prefix) and encrypts automatically on first start.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST perform an automated database backup daily at a configurable time (default: 02:00 AM local time), writing a timestamped backup file to a configurable local directory.
- **FR-002**: The system MUST automatically delete backup files older than a configurable retention period (default: 30 days) after each successful backup run.
- **FR-003**: The API service MUST be installable as a Windows Service that starts automatically with Windows and auto-recovers from failures.
- **FR-004**: A database health check endpoint MUST be available that returns success/failure status indicating whether the database is reachable.
- **FR-005**: All database connection strings MUST be stored encrypted in configuration files using machine-bound encryption (DPAPI prefix). Plaintext connection strings MUST NOT exist in any deployed configuration file.
- **FR-006**: On first run (if connection string is plaintext), the system MUST automatically encrypt the connection string and rewrite the configuration file before establishing any database connection.
- **FR-007**: The Desktop application MUST detect API unreachability at startup and display a styled Arabic error dialog with "إعادة المحاولة" and "إغلاق" options — no crash or silent failure.
- **FR-008**: The Desktop application MUST check for software updates in the background on startup, download and SHA256-verify them silently, and prompt the user to restart when an update is ready.
- **FR-009**: The auto-update check MUST complete or timeout within 8 seconds — it MUST NOT block the application from loading.
- **FR-010**: A corrupted or hash-mismatched update download MUST be discarded — the system MUST NOT install an unverified update.

### Key Entities

- **BackupRecord** (log only, not persisted in DB): `FileName`, `CreatedAt`, `SizeBytes`, `Status (Success/Failure)`, `ErrorMessage`.
- **UpdateManifest**: Describes an available update. Attributes: `Version`, `DownloadUrl`, `Sha256Hash`, `ReleaseNotes`. Retrieved from the update server.

---

## Success Criteria *(mandatory)*

- **SC-001**: Backup file is created within 5 minutes of the scheduled time every day — measurable by backup file timestamp vs. scheduled time.
- **SC-002**: 0 backup files older than the configured retention limit remain after each backup run.
- **SC-003**: API service starts within 60 seconds of a server reboot — measurable by Windows Event Log timestamps.
- **SC-004**: API service auto-restarts within 30 seconds of an unexpected process termination.
- **SC-005**: 0 plaintext connection string values exist in any deployed configuration file — verifiable by inspecting the config file content.
- **SC-006**: Desktop shows the connectivity error dialog within 5 seconds of detecting an unreachable API.
- **SC-007**: Auto-update check and download completes in under 8 seconds OR times out and fails gracefully — never blocks the UI.
- **SC-008**: 100% of update installations are preceded by a successful SHA256 hash verification — 0 unverified installs.

---

## Assumptions

- The Windows Service will be installed using .NET's built-in Windows Service support (no third-party installers required).
- DPAPI encryption is machine-bound (uses the current machine's key) — this is a one-way binding; migrating the server requires re-encrypting with the new machine's key.
- The update server is a simple HTTPS file server hosting an `update-manifest.json` file and installer binary. The URL is configurable in `SystemSettings`.
- Auto-update applies to the Desktop application only — the API (Windows Service) is updated separately by the IT administrator.
- The health check endpoint is unauthenticated (publicly accessible on the local network) since it is used by the Desktop before authentication is possible.
- Backup is performed as raw SQL backup (no SMO/SSMS dependency) — the backup command runs against the configured SQL Server instance.
- The scheduled backup time defaults to 02:00 AM local time but is configurable via `SystemSettings`.
- The Desktop startup connectivity check targets the health check endpoint — if it returns success, the app proceeds; if it fails or times out (within 5 seconds), the error dialog is shown.
