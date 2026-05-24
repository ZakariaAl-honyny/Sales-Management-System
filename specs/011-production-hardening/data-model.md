# Data Model: Production Hardening (v4.4)

**Feature**: `011-production-hardening`
**Date**: 2026-05-25

---

## No New Database Tables

This feature adds no new EF Core entities or migrations. All configuration is stored in the existing `SystemSettings` table.

---

## New SystemSettings Keys

| Key | Default Value | Description |
|-----|---------------|-------------|
| `Backup.Path` | `C:\SalesSystem\Backups` | Directory where .bak files are written |
| `Backup.ScheduleTime` | `02:00` | Daily backup time in `HH:mm` local time |
| `Backup.RetentionDays` | `30` | Number of days to keep backup files |
| `Update.ServerUrl` | *(empty)* | Base URL for update-manifest.json |

---

## New DTOs / Value Objects

### `HealthCheckDto`
Returned by the `/api/health` endpoint.

```csharp
public class HealthCheckDto
{
    public string Status { get; set; }           // "Healthy" or "Unhealthy"
    public string DatabaseStatus { get; set; }   // "Reachable" or "Unreachable"
    public DateTime Timestamp { get; set; }
}
```

### `UpdateManifest`
Deserialized from the update server's `update-manifest.json`.

```csharp
public class UpdateManifest
{
    public string Version { get; set; }      // e.g. "4.4.1"
    public string DownloadUrl { get; set; }  // Full URL to the installer binary
    public string Sha256Hash { get; set; }   // Expected SHA256 of the installer
    public string? ReleaseNotes { get; set; }
}
```

---

## New Services (no DB persistence)

| Service | Purpose |
|---------|---------|
| `IBackupService` / `BackupService` | Executes `BACKUP DATABASE` via ADO.NET, cleans up old files |
| `ScheduledBackupWorker` | `BackgroundService` that wakes at the configured time and calls `BackupService` |
| `IConnectionStringProtector` / `DpapiConnectionStringProtector` | Encrypts/decrypts DPAPI-prefixed connection string values |
| `DatabaseHealthCheck` | `IHealthCheck` — runs `SELECT 1` to verify DB reachability |
| `UpdateService` | Checks update server, downloads, SHA256-verifies, notifies user |
| `IHealthApiService` / `HealthApiService` | Desktop service that calls `/api/health` |
