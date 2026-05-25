# Quickstart: Production Hardening (v4.4)

## Implementation Order

1. **DPAPI Protector** (no dependencies): Implement `DpapiConnectionStringProtector` and `IConnectionStringProtector`. Wire into `Program.cs` startup — decrypt connection string before `DbContext` is built.
2. **Health Check**: Implement `DatabaseHealthCheck` and map to `/api/health` (unauthenticated). Test with `curl http://localhost:5000/api/health`.
3. **Windows Service**: Add `UseWindowsService()` to `Program.cs`. Test by running the API as a console app first (`dotnet run`), then install as service.
4. **BackupService + Worker**: Implement `BackupService` (ADO.NET raw SQL), then `ScheduledBackupWorker` (BackgroundService). Test manually by triggering a backup via a temporary admin endpoint.
5. **Desktop Health Check**: Implement `IHealthApiService` + `HealthApiService`. Add the startup check in `App.xaml.cs` using a 5-second timeout. Build `DatabaseErrorDialog`.
6. **UpdateService**: Implement last — it requires an update server URL to test. Can be tested locally by hosting a simple HTTP file server with a `update-manifest.json`.

## Key Invariants to Verify

- **DPAPI**: Verify that after encryption, the config value starts with `"DPAPI:"`. Verify the API starts cleanly reading the encrypted value. Verify the decrypted value is NEVER written to any log (Serilog sink inspection test).
- **Windows Service**: Verify service recovery settings are applied (`sc qfailure SalesSystemService`). Kill the service process with Task Manager — it should restart within 30 seconds.
- **Backup**: Verify the `.bak` file is a valid SQL backup by running `RESTORE VERIFYONLY FROM DISK = 'path.bak'` on SQL Server.
- **Retention Cleanup**: Manually create `.bak` files with old `LastWriteTime` and verify they are deleted after the next backup run.
- **SHA256 Update Verification**: Create a test manifest with a wrong hash — verify the downloaded file is deleted and no install is attempted.
- **Auto-Update Timeout**: Point the update URL at an intentionally slow/unreachable endpoint — verify the Desktop loads normally within 8 seconds.
- **Desktop Startup**: Stop the API service, launch the Desktop, verify `DatabaseErrorDialog` appears within 5 seconds.
