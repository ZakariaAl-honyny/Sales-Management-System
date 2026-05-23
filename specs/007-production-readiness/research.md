# Research: Phase 7 — Production Readiness

**Phase**: 0 — Outline & Research  
**Date**: 2026-05-23  
**Spec**: [spec.md](spec.md)

---

## 1. Windows Service Hosting

**Decision**: Use `UseWindowsService()` from `Microsoft.Extensions.Hosting.WindowsServices` in `Program.cs`. The API process name is `SalesSystemService`.

**Rationale**: `UseWindowsService()` is the official .NET approach — it adapts `IHostedService` lifetime to the Windows Service Control Manager (SCM) without any third-party library. It automatically uses `ConsoleLifetime` when run interactively and `WindowsServiceLifetime` when run as a service.

**Key implementation details**:
- `builder.Host.UseWindowsService()` added before `Build()`
- Serilog EventLog sink added when running as a service: `WriteTo.EventLog("SalesSystemService")`
- Service recovery configured via `sc.exe` or Inno Setup `[Run]` section with `failure actions`: 3 restarts at 1 min, 5 min, 15 min delays
- Auto-migrate database on service startup: `await db.Database.MigrateAsync()` in a startup `IHostedService`

**Alternatives considered**:
- `Topshelf` (NuGet) — rejected: adds dependency, .NET 10 native support is sufficient
- Separate Worker Service project — rejected: increases solution complexity unnecessarily

---

## 2. DPAPI Connection String Encryption

**Decision**: Use `System.Security.Cryptography.ProtectedData` (Windows DPAPI) via a `IConnectionStringProtector` service with a `"DPAPI:"` prefix sentinel.

**Rationale**: DPAPI is built into Windows, requires no external library, and is machine-bound (user-scope or machine-scope). The `"DPAPI:"` prefix allows idempotent detection — the service checks the prefix before encrypting to avoid double-encryption.

**Key implementation details**:
```csharp
public class ConnectionStringProtector : IConnectionStringProtector
{
    private const string Prefix = "DPAPI:";

    public bool IsEncrypted(string value) => value.StartsWith(Prefix);

    public string Protect(string plaintext)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.LocalMachine);
        return Prefix + Convert.ToBase64String(encrypted);
    }

    public string Unprotect(string ciphertext)
    {
        var base64 = ciphertext[Prefix.Length..];
        var encrypted = Convert.FromBase64String(base64);
        var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.LocalMachine);
        return Encoding.UTF8.GetString(decrypted);
    }
}
```
- `FirstRunSetupService` calls `Protect()` on startup only if `!IsEncrypted(current)`
- Atomic file write: write to `.tmp`, then `File.Replace(tmp, target, backup)` to prevent corruption
- `DataProtectionScope.LocalMachine` used so the API (running as SYSTEM) can decrypt without user context

**Alternatives considered**:
- `Microsoft.AspNetCore.DataProtection` — rejected: designed for web app keys, not connection string protection
- Azure Key Vault — rejected: requires internet dependency, out of scope for local-only deployment
- Environment variable only — considered valid, but DPAPI provides an additional local layer

---

## 3. Database Backup & Restore (Raw SQL)

**Decision**: Use raw `SqlConnection` + `SqlCommand` with `BACKUP DATABASE` and `RESTORE DATABASE` T-SQL. No SMO (SQL Server Management Objects) dependency.

**Rationale**: SMO is a large library (~50MB) that introduces compatibility concerns with .NET 10. Raw SQL `BACKUP DATABASE` achieves the same result with zero extra dependencies and is supported on SQL Server Express.

**Key implementation details**:
```sql
-- Backup
BACKUP DATABASE [SalesSystem] TO DISK = N'C:\Backups\SalesSystem_20260523_235900.bak'
WITH FORMAT, INIT, NAME = 'SalesSystem-Full', SKIP, NOREWIND, NOUNLOAD, STATS = 10;

-- Restore (with forced single-user)
ALTER DATABASE [SalesSystem] SET SINGLE_USER WITH ROLLBACK AFTER 30;
RESTORE DATABASE [SalesSystem] FROM DISK = N'C:\...\backup.bak'
WITH FILE = 1, NOUNLOAD, REPLACE, STATS = 5;
ALTER DATABASE [SalesSystem] SET MULTI_USER;
```
- `ScheduledBackupWorker : BackgroundService` runs in the API process, calculates delay to next 02:00 AM on startup
- Retention cleanup: `Directory.GetFiles(backupPath, "*.bak").OrderByDescending(f => f).Skip(retentionDays)` then `File.Delete()`
- Restore failure path: `TrySetMultiUserAsync()` called in the catch block to recover DB access

**Alternatives considered**:
- SQL Server Agent Jobs — rejected: not available on SQL Server Express
- Third-party backup tools — rejected: out of scope, adds external dependency

---

## 4. Auto-Update System

**Decision**: Background `Task` (fire-and-forget) launched in `App.xaml.cs` `OnStartup`, fetching a JSON manifest from a GitHub Releases URL. SHA256 verification before executing installer.

**Rationale**: The update check must never delay login. A non-awaited `Task.Run()` launched on startup runs in parallel with the login window display. SHA256 verification prevents tampered files from executing.

**Key implementation details**:
```csharp
// In App.xaml.cs OnStartup:
_ = Task.Run(() => _autoUpdateService.CheckAndOfferUpdateAsync());

// AutoUpdateService:
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
var manifest = await httpClient.GetFromJsonAsync<UpdateManifest>(manifestUrl, cts.Token);
if (manifest.Version <= currentVersion) return;
var file = await DownloadAsync(manifest.DownloadUrl, cts.Token);
if (!VerifySha256(file, manifest.Sha256)) { Log.Warning(...); return; }
// Show update prompt on UI thread
```
- `settings.json` in `%AppData%\SalesSystem\` stores `skippedVersion` as a string
- Version comparison: `System.Version.Parse()` — never string comparison
- Timeout: `CancellationTokenSource(TimeSpan.FromSeconds(8))`
- `TaskCanceledException` caught silently — no user-visible error

**Alternatives considered**:
- Squirrel.Windows / Velopack — rejected: large library overhead for simple update scenario
- ClickOnce — rejected: requires specific server setup, not suitable for offline/LAN deployments

---

## 5. Inno Setup Installer

**Decision**: Single `setup.iss` script producing one `.exe` that installs the self-contained .NET 10 API service, the desktop client, registers `SalesSystemService`, and optionally installs SQL Server Express.

**Rationale**: Inno Setup is free, widely used, and produces a single-file installer with full control over installation steps.

**Key implementation details**:
- `[Run]` section installs service: `sc create SalesSystemService binPath= "..." start= auto`
- `[Run]` section sets service recovery: `sc failure SalesSystemService reset= 86400 actions= restart/60000/restart/300000/restart/900000`
- `[Files]` includes the self-contained publish output of `SalesSystem.Api` and `SalesSystem.DesktopPWF`
- `[Icons]` creates desktop shortcut for the desktop client
- `[Code]` section: detect existing SQL Server instance, prompt admin to install Express if absent

**Alternatives considered**:
- WiX Toolset — rejected: steeper learning curve, XML verbose
- MSIX — rejected: requires Microsoft Store signing or sideloading policy configuration

---

## 6. Settings & User Management API Endpoints

**Decision**: Reuse and extend existing `SettingsController` and `UsersController`. Both decorated with `[Authorize(Policy = "AdminOnly")]`.

**Rationale**: Endpoints already exist per the project structure. Changes are additive — adding `CostingMethod` to `StoreSettingsDto` and `UpdateSettingsRequest`, and ensuring `UsersController` only performs soft deletes.

**Key implementation details**:
- `GET /api/v1/settings` → returns `StoreSettingsDto` (includes `CostingMethod int`)
- `PUT /api/v1/settings` → accepts `UpdateSettingsRequest` (validated with FluentValidation)
- `GET /api/v1/users` → list all users (Admin sees inactive users too)
- `POST /api/v1/users` → create user (`CreateUserRequest` with FluentValidation)
- `PUT /api/v1/users/{id}` → update user details
- `DELETE /api/v1/users/{id}` → soft delete only (`IsActive = false`) — hard delete forbidden by Constitution XII
- `GET /api/v1/backup/list` → list backup files (Admin only)
- `POST /api/v1/backup` → trigger manual backup (Admin only)
- `POST /api/v1/backup/restore` → trigger restore (Admin only, with confirmation token)

---

## 7. Database Health Check on Desktop Startup

**Decision**: `IDatabaseHealthCheckService` calls `GET /api/v1/health/database` on startup (before login window is shown). On failure, show `DatabaseErrorDialog` (styled WPF dialog with Retry / Exit buttons).

**Rationale**: The health check endpoint already exists. Wrapping it in a startup service with a styled dialog is the correct UX for graceful failure without raw exception messages.

**Key implementation details**:
- Health check called in `App.xaml.cs` `OnStartup` before `MainWindow` is shown
- `DatabaseErrorDialog` uses the same `PositionOverOwner()` pattern with self-ownership guard
- Retry logic: up to 3 attempts with 2-second delay between attempts
- If all retries fail: `Application.Current.Shutdown()` with exit code 1

---

## Resolved NEEDS CLARIFICATION Items

*None — the spec had zero NEEDS CLARIFICATION markers.*

All research decisions are internally consistent and align with:
- Existing AGENTS.md rules (RULE-035 through RULE-040, RULE-237)
- Constitution Principles VII, VIII, X, XII
- PRD-MVP.md Sections 3.17–3.21
