# Implementation Plan: Phase 7 — Production Readiness

**Branch**: `007-production-readiness` | **Date**: 2026-06-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/007-production-readiness/spec.md`

---

## Summary

Phase 7 transforms the API from a development-convenient console application into a production-grade Windows service that starts automatically with the operating system, survives crashes with automatic recovery, encrypts its own database credentials at rest, keeps its data safe with nightly backups, and stays up-to-date without manual intervention. Every capability in this phase is designed for unattended operation: the API must run headlessly with no console window, the installer must be a single self-contained executable that a non-technical store owner can run, and all failures must degrade gracefully rather than crash outright.

The phase is organized into five workstreams: Windows Service hosting with event-log integration, DPAPI-secured connection string management, a full backup and restore subsystem with a scheduled background worker, a background auto-update checker with SHA256 integrity verification, and a startup database-health verification that prevents the Desktop from attempting to log into a non-functional API.

---

## Windows Service Hosting

The API is hosted as a Windows Service named `SalesSystemService` with an Arabic display name. This is achieved by calling `UseWindowsService()` on the host builder in `Program.cs`, which configures the ASP.NET Core application to run as a Windows Service with no console window, no user interaction, and automatic startup when the machine boots. The service is registered with the Windows Service Control Manager (SCM) during installation, and the installer configures the recovery policy: on first failure, restart after one minute; on second failure, restart after five minutes; on third failure, restart after fifteen minutes; on subsequent failures within the same day, continue restarting at fifteen-minute intervals.

Logging is configured with two sinks: a file sink (Serilog rolling file in `%ProgramData%\SalesSystem\Logs`) for detailed diagnostic traces, and an EventLog sink (`Serilog.Sinks.EventLog`) for Windows Event Viewer integration. The EventLog sink writes to the `Application` log with source name `SalesSystemService`. Critical errors (database connection failures, unhandled exceptions, backup failures) write at Error level to both sinks; informational events (service start/stop, successful backup, update check) write at Information level; validation failures write at Warning level per the logging separation policy.

On startup, the service retries database connectivity up to three times with a five-second delay between attempts. This retry loop handles the common scenario where SQL Server is still starting up when the API service begins. If all three retries fail, the service logs a critical error and continues running in a degraded state — the health check endpoint returns `Database=Disconnected`, and all database-dependent API calls return 503 Service Unavailable. This prevents a hard crash while giving SQL Server time to recover. Database migrations run automatically on each startup via `DbContext.Database.Migrate()`, wrapped in the same retry loop, so that new deployments apply schema changes without manual SQL execution.

---

## Connection String Security with DPAPI

Connection strings are the single most sensitive configuration value in the system — a compromised connection string grants full database access. Phase 7 eliminates all plaintext connection strings from configuration files. The production and development configuration files store an empty string with a `_comment` property that documents the expected environment variable name (`SALESSYSTEM_DB_CONNECTION`). The actual connection string is never stored in plaintext on disk.

The `FirstRunSetupService` runs once during initial application deployment. It reads the plaintext connection string from the `SALESSYSTEM_DB_CONNECTION` environment variable, encrypts it using DPAPI (Data Protection API via `System.Security.Cryptography.ProtectedData`), prepends the `"DPAPI:"` prefix to identify the encrypted payload, and writes the result to `appsettings.json` using an atomic write pattern: the encrypted content is written to a `.tmp` file, then `File.Replace()` swaps it into place atomically, with a `.bak` backup created automatically by `File.Replace()`. On subsequent startups, the service detects the `"DPAPI:"` prefix and decrypts the payload using `ProtectedData.Unprotect()`.

The `SecureDbContextFactory` performs the decryption. It is used by the `Program.cs` DbContext registration and by the `ScheduledBackupWorker` (which needs a raw `SqlConnection` for `BACKUP DATABASE`). It reads the connection string from the `appsettings.json` configuration (which may contain the DPAPI-encrypted value or a fallback environment variable reference), detects the `"DPAPI:"` prefix, decrypts the payload, and returns the plaintext connection string. If the environment variable `SALESSYSTEM_DB_CONNECTION` is defined, it takes priority over all other sources — this allows a server administrator to override the config file entirely for emergency recovery scenarios.

Data Protection keys are stored in `%ProgramData%\SalesSystem\DataProtectionKeys` via `Microsoft.AspNetCore.DataProtection`. These keys are machine-scoped and are created automatically on first application startup. The key ring is persisted to the file system and encrypted at rest using DPAPI. In a production deployment, backup operators should include this directory in their file-level backups alongside the database backups.

A `SecurityAudit` class runs only in DEBUG builds and scans configuration files for unencrypted connection strings and hardcoded password-like values. It writes warnings to the debug output and the Serilog log. This is a development safeguard only — production builds strip this class entirely via conditional compilation.

---

## Backup and Restore

The backup subsystem uses raw T-SQL commands over a dedicated `SqlConnection` — it does not use Entity Framework or SQL Server Management Objects (SMO). The `IBackupService` interface in the Application layer defines four operations: `CreateBackupAsync(string backupPath)` executes `BACKUP DATABASE [SalesSystemDb] TO DISK = @path`; `RestoreBackupAsync(string backupPath)` executes a multi-step restore that first sets the database to `SINGLE_USER` mode with `ROLLBACK AFTER 30 SECONDS`, performs the `RESTORE DATABASE` command, and then sets the database back to `MULTI_USER` mode; `GetBackupHistoryAsync()` queries the `msdb.dbo.backupset` table for listing available backups; and `DeleteOldBackupsAsync(int retentionDays)` removes backup files older than the specified retention period from the filesystem and cleans up the backup directory.

The restore flow is designed for safety. Setting `SINGLE_USER WITH ROLLBACK AFTER 30` gives active transactions thirty seconds to complete before the restore proceeds, preventing abrupt disconnection of users while still guaranteeing the database will eventually be available for restore. After the restore completes (or if the restore fails at any point), the system attempts to set the database back to `MULTI_USER` mode. A dedicated recovery helper method `TrySetMultiUserAsync()` is called in a `finally` block to ensure the database is never left in `SINGLE_USER` mode after a failed restore.

The `ScheduledBackupWorker` is a `BackgroundService` that runs inside the API process. It triggers daily at 2:00 AM local time. It reads the backup path and retention days from `SystemSettings` (see `docs/database-schema.md` section 2.7 for the `SystemSettings` table definition — backup-related settings have `Category = "Backup"`). The worker creates a backup file named `SalesSystemDb_yyyyMMdd_HHmmss.bak` in the configured directory, logs success or failure via Serilog, and then calls `DeleteOldBackupsAsync` to remove files outside the retention window. If the backup fails, it logs at Error level but does not retry — the next nightly attempt will handle it. Configuration parsing for `BackupPath`, `RetentionDays`, and `BackupSchedule` uses `int.TryParse` and `string.IsNullOrWhiteSpace` guards to prevent unhandled format exceptions from crash the worker.

---

## Auto-Update System

The auto-update system is designed for silent, non-blocking operation. The `UpdaterService` runs as a fire-and-forget task that starts three seconds after the Desktop application launches (allowing the main UI to render first). It calls a version-check API endpoint on the running instance, which returns the latest available version number, download URL, and SHA256 checksum. The Desktop compares the latest version against its current version (read from the application assembly) using `System.Version` comparison. If a newer version is found and has not been skipped by the user, the service downloads the installer to a temporary location, verifies the SHA256 checksum of the downloaded file against the expected checksum, and displays a notification to the user.

The entire check-and-download operation is bounded by an 8-second total timeout enforced by `CancellationTokenSource`. If the version-check API is unreachable, the download exceeds the timeout, the checksum verification fails, or any other error occurs, the update check is silently abandoned — the system continues running with the current version and tries again on the next application launch.

If verification succeeds, the user sees a non-modal notification (via `IToastNotificationService`) with an "Install Now" button. Clicking the button triggers the `LaunchInstallerAndExitAsync` method, which writes a small batch file that waits two seconds (for the Desktop application to exit cleanly), launches the downloaded installer with silent-mode arguments, and then terminates. The Desktop calls `Application.Current.Shutdown()` after writing the batch file. The installer handles the update silently — no user interaction required — and relaunches the application after installation completes.

Skipped versions are persisted to `%AppData%\SalesSystem\settings.json` (a simple JSON file, not `appsettings.json`). If the user clicks "Skip This Version" on the update notification, the version string is written to this file and the check is suppressed until the next major version is detected. This prevents nagging the user with the same update repeatedly.

---

## Database Health Check on Startup

Before the WPF Desktop application shows the login screen, it performs a connectivity check against the API's health endpoint. This prevents the confusing user experience of typing credentials into a login form only to see a database connection error after authentication. The startup flow in `App.xaml.cs` runs an async initialization method that: resolves `IDatabaseHealthCheckService` from the DI container, calls `CheckAsync()` which sends an HTTP GET to `/api/v1/health/database`, and inspects the returned JSON for the database connection status.

If the database is connected, the application proceeds to show the `LoginWindow`. If the database is disconnected, the application shows a `DatabaseErrorDialog` with a clear Arabic message explaining that the database server is not reachable. The dialog has two buttons: "إعادة المحاولة" (Retry) and "خروج" (Exit). The Retry button re-runs the health check after a 1-second delay, repeating the loop indefinitely until the user either succeeds or gives up. This polling loop ensures that the application can recover from temporary network interruptions or SQL Server restarts without requiring a full application restart.

The `DatabaseHealthCheckService` is resilient to network errors. It catches `HttpRequestException` (API unreachable), `TaskCanceledException` (timeout), and `JsonException` (malformed response). Each failure type maps to a specific Arabic error message. The service is registered in the Desktop DI container alongside `ISettingsApiService`, `IAuthApiService`, and other service-layer interfaces.

On the API side, the health endpoint (`GET /api/v1/health`) is intentionally left unauthenticated — monitoring tools and load balancers need to check health without a JWT token. It returns a JSON object with `Status` ("OK" or "Degraded"), `Database` ("Connected" or "Disconnected"), and a `Timestamp`. The `/api/v1/health/database` endpoint specifically checks database connectivity via `DbContext.Database.CanConnectAsync()` and returns HTTP 200 with `{"status": "connected"}` or HTTP 503 with `{"status": "disconnected", "message": "..."}`.

---

## Installer

The installer is built with Inno Setup 6. The `setup.iss` script performs the following sequence: checks that .NET 10 Desktop Runtime is installed (shows a download prompt if missing), installs the API as a Windows Service using `sc create` with auto-start and the configured recovery policy, copies the Desktop application files to `%ProgramFiles%\SalesSystem\Desktop`, copies the API binaries to `%ProgramFiles%\SalesSystem\Api`, creates the `%ProgramData%\SalesSystem` directory structure (DataProtectionKeys, Logs, Backups), sets appropriate NTFS permissions (Administrators full control, Network Service read/execute for the API), registers the uninstaller, and optionally creates a desktop shortcut. The installer runs with Administrator privileges (requested via `PrivilegesRequired=admin`).

The installer output is a single self-contained `.exe` file (approximately 80 MB including the bundled .NET 10 runtime). It is built by Inno Setup's compiler using a post-build script in the solution. The installer is digitally signed with a code-signing certificate in production builds.

---

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Background backup worker inside API process | No extra deployment artifacts | A separate Windows Service would double the installation and monitoring surface area |
| DPAPI encryption instead of certificate-based | No certificate management burden | Certificate-based encryption requires certificate provisioning, renewal, and permission management beyond the target audience's capability |
| 8-second update timeout is generous | Ensures slow networks don't block main thread | Shorter timeout risks false negatives on metered or cellular connections |
