# Implementation Plan: System Settings & Database Backup

**Branch**: `007-system-services`

## Summary

Implement the missing backend logic for Store Settings and Database Backup/Restore. While the domain entities and UI are already in place, the Application services and API controllers are missing. This phase will finalize these utility features to ensure the system is production-ready.

## User Review Required

> [!IMPORTANT]
> - **Backup Location**: Database backups will be stored in a subfolder within the application directory by default. The user needs to ensure the SQL Server service account has write permissions to this folder.
> - **Restore Operation**: Restoring a database will terminate all active connections. This is a high-risk operation.

## Proposed Changes

### SalesSystem.Application

#### [NEW] [IStoreSettingsService.cs](file:///c:/Users/ALlahabi/Desktop/Sales%20Management%20System/SalesSystem/SalesSystem.Application/Interfaces/Services/IStoreSettingsService.cs)
Define methods for retrieving and updating store-wide settings (Name, Address, Tax, etc.).

#### [NEW] [StoreSettingsService.cs](file:///c:/Users/ALlahabi/Desktop/Sales%20Management%20System/SalesSystem/SalesSystem.Application/Services/StoreSettingsService.cs)
Implement the settings logic using the `IUnitOfWork.StoreSettings` repository.

#### [NEW] [IBackupService.cs](file:///c:/Users/ALlahabi/Desktop/Sales%20Management%20System/SalesSystem/SalesSystem.Application/Interfaces/Services/IBackupService.cs)
Define methods for `CreateBackupAsync(string path)` and `RestoreBackupAsync(string path)`.

#### [NEW] [BackupService.cs](file:///c:/Users/ALlahabi/Desktop/Sales%20Management%20System/SalesSystem/SalesSystem.Application/Services/BackupService.cs)
Implement backup/restore using raw SQL commands (`BACKUP DATABASE` / `RESTORE DATABASE`) via EF Core's `Database.ExecuteSqlRawAsync`.

---

### SalesSystem.Api

#### [NEW] [SettingsController.cs](file:///c:/Users/ALlahabi/Desktop/Sales%20Management%20System/SalesSystem/SalesSystem.Api/Controllers/SettingsController.cs)
Expose settings endpoints under `api/v1/settings`. Protected by `AdminOnly` for updates.

#### [NEW] [BackupController.cs](file:///c:/Users/ALlahabi/Desktop/Sales%20Management%20System/SalesSystem/SalesSystem.Api/Controllers/BackupController.cs)
Expose backup/restore endpoints under `api/v1/backup`. Protected by `AdminOnly`.

---

### SalesSystem.Infrastructure

#### [MODIFY] [UnitOfWork.cs](file:///c:/Users/ALlahabi/Desktop/Sales%20Management%20System/SalesSystem/SalesSystem.Infrastructure/Data/UnitOfWork.cs)
Ensure all repositories are correctly wired (already mostly done, just verification).

---

## Verification Plan

### Automated Tests
- N/A (Manual verification via API endpoints)

### Manual Verification
1. **Settings**:
   - Update store name via `PUT /api/v1/settings`.
   - Retrieve settings via `GET /api/v1/settings` and verify the change.
2. **Backup**:
   - Trigger a backup via `POST /api/v1/backup/create`.
   - Verify the `.bak` file exists in the specified directory.
   - (Caution) Test restore on a test database if possible.
