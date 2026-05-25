# API Contracts: Production Hardening (v4.4)

**Feature**: `011-production-hardening`
**Date**: 2026-05-25

---

## `HealthController` — `/api/health`

### `GET /api/health`

Returns the current health status of the API and its database connection.

**Authorization**: **NONE** — this endpoint is intentionally unauthenticated. The Desktop must call it before logging in.
**Request**: Empty.
**Response**:
- `200 OK`: `HealthCheckDto { Status: "Healthy", DatabaseStatus: "Reachable", Timestamp }`
- `503 Service Unavailable`: `HealthCheckDto { Status: "Unhealthy", DatabaseStatus: "Unreachable", Timestamp }`

**Usage by Desktop**: Called on startup (before `MainWindow` loads) with a 5-second timeout. On failure, `DatabaseErrorDialog` is shown.

---

## Windows Service Registration (No HTTP endpoint — operational)

The API is registered as a Windows Service named `SalesSystemService`. The service is installed via the command:

```shell
sc.exe create SalesSystemService binPath="C:\SalesSystem\Api\SalesSystem.Api.exe" start=auto
sc.exe failure SalesSystemService reset=60 actions=restart/5000/restart/10000/restart/30000
```

This configures 3 auto-restart attempts (5s, 10s, 30s delays).

---

## Existing Settings Endpoints (Extended)

The existing `SettingsController` will handle reading/updating backup and update settings. No new endpoints are needed — the existing GET/PUT pattern handles the new `SystemSettings` keys:

- `Backup.Path`
- `Backup.ScheduleTime`
- `Backup.RetentionDays`
- `Update.ServerUrl`
