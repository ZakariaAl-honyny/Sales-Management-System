# Implementation Plan: Phase 11 — Production Hardening

**Branch**: `011-production-hardening` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/011-production-hardening/spec.md`

---

## Summary

Phase 11 hardens the Sales Management System for unattended production deployment. While Phase 7 established the Windows Service hosting, backup infrastructure, and auto-update framework, this phase focuses on the runtime resilience and security measures that protect the system during day-to-day operation: crash-safe error dialogs for unhandled exceptions, DPAPI-encrypted connection strings with a robust first-run setup, nightly database backups with retention cleanup, background infrastructure workers, environment-variable-based JWT secrets that fail securely in production, and API-level rate limiting that prevents brute-force attacks on the login endpoint.

Each workstream in this phase addresses a specific failure mode: the `FallbackErrorDialog` ensures that an unhandled exception never leaves the user staring at a blank white window; the DPAPI encryption chain ensures that the database credentials are never stored in plaintext anywhere on disk; the `ScheduledBackupWorker` ensures that data loss is bounded by at most one day; the `SecurityAudit` (DEBUG-only) catches developer mistakes before they reach production; the JWT secret validation ensures that a missing configuration does not default to an insecure fallback; and the rate limiting ensures that a compromised credential does not enable unlimited brute-force attempts.

---

## FallbackErrorDialog — Last-Resort Exception Handler

The `FallbackErrorDialog` is the system's outermost defense against unhandled exceptions. It is triggered by the `AppDomain.CurrentDomain.UnhandledException` event and the `Dispatcher.UnhandledException` event in the WPF Desktop application. When any code path throws an exception that is not caught by a `try-catch`, the `ExecuteAsync` wrapper, or the `HandleFailure` flow of a ViewModel, these events fire and invoke the fallback dialog handler.

The handler is designed for thread safety: it captures the exception details, the current timestamp, and a stack trace, logs everything to Serilog at Critical level, and then shows a modal dialog overlay. The overlay is a `FallbackErrorDialog` window with a red header, a warning icon, a primary message in Arabic ("حدث خطأ غير متوقع. تم تسجيل الخطأ."), a secondary message advising the user to contact the system administrator, a "نسخ التفاصيل" (Copy Details) button that copies the exception information to the clipboard (formatted as text for the administrator), a "إعادة تشغيل" (Restart) button that calls `System.Windows.Forms.Application.Restart()` (falling back to `Process.Start` with the current executable path if restart is unavailable), and a "خروج" (Exit) button that calls `Application.Current.Shutdown()`.

The dialog guards against self-ownership crashes: it detects whether the `MainWindow` property is null or equals the dialog instance itself, and if so, centers on screen instead of setting an `Owner`. This prevents the `InvalidOperationException` ("Cannot set Owner property to itself") that could occur if the unhandled exception fires before the MainWindow is fully initialized. The dialog is wrapped in a top-level `try-catch` as well — if even the dialog fails to render, the application calls `Environment.FailFast()` with the original exception details to generate a Windows Error Report.

The `FallbackErrorDialog` is implemented as a XAML window in `Views/Dialogs/` with a corresponding ViewModel that implements `INotifyPropertyChanged` for the exception details text. It is intentionally simple — no dependency injection, no service resolution — because it must function when the DI container may be in an inconsistent state.

---

## Connection String DPAPI Encryption Chain

The connection string encryption chain spans three components: the `ConnectionStringProtector` handles encrypt and decrypt operations; the `FirstRunSetupService` handles the initial encryption during application deployment; and the `SecureDbContextFactory` handles transparent decryption during DbContext resolution.

The `ConnectionStringProtector` (implementation of `IConnectionStringProtector` in the Infrastructure layer) uses `System.Security.Cryptography.ProtectedData` with `DataProtectionScope.CurrentUser`. This means the encrypted payload is bound to the Windows user account that performed the encryption — typically the `NETWORK SERVICE` account under which the API Windows Service runs. The protector prepends the literal prefix `"DPAPI:"` to all encrypted values, which enables the `SecureDbContextFactory` to distinguish between already-encrypted values and plaintext values (or environment variable references). The `IsEncrypted(string)` method checks for the prefix and prevents double-encryption. The `Encrypt(string plaintext)` method converts the input string to UTF-8 bytes, calls `ProtectedData.Protect(plaintextBytes, null, DataProtectionScope.CurrentUser)`, converts the result to a Base64 string, and prepends `"DPAPI:"`. The `Decrypt(string encrypted)` method validates the prefix, strips it, converts the remainder from Base64 to bytes, calls `ProtectedData.Unprotect()`, and converts the result back to a UTF-8 string.

The `FirstRunSetupService` is called once during the initial `Program.cs` startup sequence (before the `WebApplication` is built). It checks whether the configured connection string starts with `"DPAPI:"`. If it does not, the service reads the plaintext connection string (from the `SALESSYSTEM_DB_CONNECTION` environment variable, which is the recommended source), encrypts it via `ConnectionStringProtector.Encrypt()`, and writes the encrypted result to `appsettings.json` using an atomic write pattern: serialize the updated configuration to a `.tmp` file, call `File.Replace(tmpPath, appsettingsPath, backupPath)` to swap the files atomically while creating a `.bak` backup of the previous configuration. If the connection string already starts with `"DPAPI:"`, the service does nothing — the operation is idempotent. If the environment variable is not set and the configuration file does not contain a connection string, the service logs a warning and continues without encryption, relying on the `SecureDbContextFactory` fallback logic.

The `SecureDbContextFactory` resolves the connection string in `Program.cs` at the point of DbContext registration. It reads the connection string from the configuration (which may be a direct value, a `"DPAPI:"` encrypted value, or an empty string), calls `ConnectionStringProtector.Decrypt()` if the `"DPAPI:"` prefix is detected, and falls back to the `SALESSYSTEM_DB_CONNECTION` environment variable if the resolved string is empty or null. If all sources yield an empty connection string, the factory logs a Critical-level error and throws `InvalidOperationException("Connection string not configured")` — the application will not start without a valid database connection.

Data Protection keys (used internally by ASP.NET Core's Data Protection stack for anti-forgery, authentication cookies, and other cryptographic operations) are persisted to `%ProgramData%\SalesSystem\DataProtectionKeys`. This directory is created during the first application startup. Key files are XML documents encrypted at rest by the operating system using DPAPI. The `services.AddDataProtection()` call in `Program.cs` configures the key ring to persist to this directory and to protect keys with `ProtectKeysWithDpapi()`.

---

## SecurityAudit (DEBUG Only)

The `SecurityAudit` class is a conditional-compilation component that exists only in `DEBUG` builds. It is called once during application startup (after configuration is loaded but before the DI container is built) and performs four checks: it searches the `appsettings.json` file for any value that contains `"Server="` or `"Data Source="` (indicating a plaintext connection string) and logs a warning if one is found; it searches for any value that looks like a password (`"Password="` or `"pwd="`) that is not in an encrypted section; it verifies that the `JwtSettings:Secret` configuration section is populated from an environment variable (by checking that the value does not equal a known default like `"YourSuperSecretKeyHere123!"`); and it verifies that the configured JWT secret is at least 32 characters long. Any violation produces a debug-time assertion and a Serilog warning.

This component is stripped from RELEASE builds by the `[Conditional("DEBUG")]` attribute on each check method. This prevents any possibility of security-sensitive information being logged or asserted in production. The class is located in the Infrastructure layer alongside the `ConnectionStringProtector`.

---

## Backup System and ScheduledBackupWorker

The backup subsystem provides full database backup and restore capabilities using raw T-SQL on a dedicated `SqlConnection` (not Entity Framework). The `IBackupService` interface defines `CreateBackupAsync(string backupPath, CancellationToken ct)` which executes `BACKUP DATABASE [SalesSystemDb] TO DISK = @path WITH INIT, NAME = @name, COMPRESSION` — compression is enabled by default to minimize disk space usage. `RestoreBackupAsync(string backupPath, CancellationToken ct)` executes the restore sequence: `ALTER DATABASE [SalesSystemDb] SET SINGLE_USER WITH ROLLBACK AFTER 30` to give active transactions time to complete, `RESTORE DATABASE [SalesSystemDb] FROM DISK = @path WITH REPLACE` to overwrite the current database, and finally `ALTER DATABASE [SalesSystemDb] SET MULTI_USER` to restore access. A `finally` block always calls `TrySetMultiUserAsync()` to prevent the database from being left in single-user mode if the restore fails at any point.

`GetBackupHistoryAsync()` queries `RESTORE LABELONLY FROM DISK = @path` to read backup metadata from the backup file header — this provides the backup date, size, and database name without querying `msdb`. `DeleteOldBackupsAsync(int retentionDays)` enumerates `.bak` files in the backup directory and deletes those whose `LastWriteTime` is older than the retention period.

The `ScheduledBackupWorker` is an `IHostedService` (implemented as a `BackgroundService`) that runs inside the API process. It triggers daily at 2:00 AM using a `PeriodicTimer` with a computed initial delay (time until 2:00 AM from the current time, or 24 hours minus current time if it is already past 2:00 AM — this ensures the worker stays on a 24-hour cycle). When triggered, the worker: reads `BackupPath` and `RetentionDays` from `SystemSettings` (see `docs/database-schema.md` section 2.7 for the `SystemSettings` table — backup settings use `Category = "Backup"`), validates that the backup directory exists (creates it if it does not), constructs a backup file name using the format `SalesSystemDb_yyyyMMdd_HHmmss.bak`, calls `CreateBackupAsync`, and then calls `DeleteOldBackupsAsync`. Success is logged at Information level; failure is logged at Error level with full exception details. The worker does not retry on failure — the next nightly attempt will handle it. Configuration values (`BackupPath`, `RetentionDays`) are parsed with `int.TryParse` to prevent unhandled format exceptions from crashing the worker. If `BackupPath` is not configured, the worker logs a warning and skips the backup, defaulting to `%ProgramData%\SalesSystem\Backups`.

---

## JWT Secret from Environment Variable

The JWT secret key is never stored in any configuration file. In `Program.cs`, the JWT configuration reads the signing key from the `SALESSYSTEM_JWT_SECRET` environment variable. If the environment variable is not set or is empty, the application checks whether the current environment is `Development` — in development mode, it uses a hardcoded development key (at least 32 characters, clearly marked with a comment as a development-only placeholder). In production mode, the application throws `InvalidOperationException("JWT secret is not configured. Set the SALESSYSTEM_JWT_SECRET environment variable.")` and crashes immediately rather than running with an insecure default.

The same approach applies to the JWT issuer and audience configuration: `SALESSYSTEM_JWT_ISSUER` and `SALESSYSTEM_JWT_AUDIENCE` environment variables are read with fallback values of `"SalesSystem"` and `"SalesSystemDesktop"` respectively (these are informational, not security-sensitive). The token expiration is hardcoded to 24 hours and read from a non-sensitive configuration section.

---

## Rate Limiting

Rate limiting is configured at both the global and endpoint-specific levels. The global policy allows 100 requests per minute per IP address across all unauthenticated endpoints (health checks, static files). The login-specific policy (`LoginPolicy`) allows 5 requests per 15-minute sliding window per IP address — this is deliberately restrictive to prevent brute-force password guessing.

The rate limiter is added to the service collection in `Program.cs` via `builder.Services.AddRateLimiter(...)` and the middleware is inserted into the pipeline via `app.UseRateLimiter()`. Critically, the rate limiter middleware is placed **before** `UseAuthentication()` and `UseAuthorization()` in the pipeline — this ensures that rate limiting is enforced before authentication overhead is incurred, and it prevents unauthenticated brute-force traffic from consuming CPU cycles on password hashing.

The login endpoint (`/api/v1/auth/login`) is decorated with `[EnableRateLimiting("LoginPolicy")]`. When the rate limit is exceeded, the middleware returns HTTP 429 Too Many Requests with a standardized JSON response body containing an Arabic error message (`"تجاوزت الحد المسموح من محاولات الدخول. الرجاء المحاولة بعد ١٥ دقيقة."`), the error code `RATE_LIMIT_EXCEEDED`, a retry-after timestamp, and a unique request ID for support reference.

Global rate limiting uses the `SlidingWindowRateLimiter` with a window size of 1 minute, a segment count of 1, and a permit limit of 100. The `LoginPolicy` also uses `SlidingWindowRateLimiter` with a window of 15 minutes, a segment count of 1, and a permit limit of 5. Both policies are applied per IP address (extracted from the `X-Forwarded-For` header if present, falling back to the remote IP address). The `OnRejected` callback logs the rejected request at Warning level and returns the Arabic JSON response. Rejected requests from authenticated users (who should never hit the global limit during normal operation) are logged at Error level with user identifier for security monitoring.

A `Security-Plan.md` document in the `docs/` directory tracks the implementation status of all security measures across the system, including rate limiting, DPAPI encryption, JWT secret validation, password hashing work factor, soft-delete enforcement, and connection string security. Each measure is listed with its phase number, implementation status (Implemented / Not Implemented), and cross-reference to the relevant AGENTS.md rule.

---

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| DPAPI encryption instead of Azure Key Vault | No cloud dependency for single-store deployments | Key Vault requires Azure subscription, internet connectivity, and managed identity setup |
| FallbackErrorDialog with Environment.FailFast | Must guarantee error visibility even when WPF is corrupted | A simple `try-catch` in `App.xaml.cs` does not catch `AppDomain` level CLR exceptions |
| Rate limiter before Authentication middleware | Prevents hashing overhead for blocked IPs | Placing it after Authentication would waste CPU on BCrypt verification before rejection |
| Separate worker for ScheduledBackupWorker | Backup must run even if web API is idle | Embedding backup in a request path would create unpredictable latency for users |
