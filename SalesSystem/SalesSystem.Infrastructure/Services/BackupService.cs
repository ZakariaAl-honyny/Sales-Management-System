using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Infrastructure.Persistence;

namespace SalesSystem.Infrastructure.Services;

public sealed class BackupService : IBackupService
{
    private readonly SecureDbContextFactory _dbFactory;
    private readonly ILogger<BackupService> _logger;
    private readonly string _databaseName;
    private readonly string _defaultBackupFolder;

    public BackupService(
        SecureDbContextFactory dbFactory,
        IConfiguration configuration,
        ILogger<BackupService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;

        var connectionString = _dbFactory.GetDecryptedConnectionString();
        var builder = new SqlConnectionStringBuilder(connectionString);
        _databaseName = builder.InitialCatalog;
        _defaultBackupFolder = configuration["Backup:DefaultBackupPath"]
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

        if (!Directory.Exists(_defaultBackupFolder))
        {
            Directory.CreateDirectory(_defaultBackupFolder);
        }
    }

    public async Task<Result<string>> CreateBackupAsync(string? folderPath = null, CancellationToken ct = default)
    {
        try
        {
            folderPath ??= _defaultBackupFolder;
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var fileName = $"{_databaseName}_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            var filePath = Path.Combine(folderPath, fileName);

            var connectionString = _dbFactory.GetDecryptedConnectionString();
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            var backupSql = $"""
                BACKUP DATABASE [{_databaseName}]
                TO DISK = @path
                WITH FORMAT,
                     MEDIANAME = 'SalesSystemBackup',
                     NAME = 'Full Database Backup - {DateTime.Now:yyyy-MM-dd HH:mm}',
                     COMPRESSION,
                     STATS = 10;
                """;

            await using var command = new SqlCommand(backupSql, connection);
            command.CommandTimeout = 300;
            command.Parameters.AddWithValue("@path", filePath);

            await command.ExecuteNonQueryAsync(ct);

            _logger.LogInformation("Database backup created successfully at {FilePath}", filePath);
            return Result<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating database backup");
            return Result<string>.Failure("حدث خطأ أثناء إنشاء النسخة الاحتياطية: " + ex.Message);
        }
    }

    public async Task<Result> RestoreBackupAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(filePath))
                return Result.Failure("ملف النسخة الاحتياطية غير موجود");

            var connectionString = _dbFactory.GetDecryptedConnectionString();
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master"
            };

            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(ct);

            var singleUserSql = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
            await using (var cmd1 = new SqlCommand(singleUserSql, connection))
            {
                cmd1.CommandTimeout = 60;
                await cmd1.ExecuteNonQueryAsync(ct);
            }

            var restoreSql = $"""
                RESTORE DATABASE [{_databaseName}]
                FROM DISK = @path
                WITH REPLACE,
                     RECOVERY,
                     STATS = 10;
                """;

            await using (var cmd2 = new SqlCommand(restoreSql, connection))
            {
                cmd2.CommandTimeout = 600;
                cmd2.Parameters.AddWithValue("@path", filePath);
                await cmd2.ExecuteNonQueryAsync(ct);
            }

            var multiUserSql = $"ALTER DATABASE [{_databaseName}] SET MULTI_USER;";
            await using (var cmd3 = new SqlCommand(multiUserSql, connection))
            {
                await cmd3.ExecuteNonQueryAsync(ct);
            }

            _logger.LogInformation("Database restored successfully from {FilePath}", filePath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring database");

            await TrySetMultiUserAsync(ct);

            return Result.Failure("حدث خطأ أثناء استعادة النسخة الاحتياطية: " + ex.Message);
        }
    }

    public Task<Result<List<string>>> GetBackupListAsync(CancellationToken ct = default)
    {
        try
        {
            var files = Directory.GetFiles(_defaultBackupFolder, "*.bak")
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .Cast<string>()
                .OrderByDescending(f => f)
                .ToList();

            return Task.FromResult(Result<List<string>>.Success(files));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backup list");
            return Task.FromResult(Result<List<string>>.Failure("حدث خطأ أثناء جلب قائمة النسخ الاحتياطية"));
        }
    }

    public async Task<Result> DeleteOldBackupsAsync(int retentionDays, CancellationToken ct = default)
    {
        try
        {
            if (!Directory.Exists(_defaultBackupFolder))
                return Result.Success();

            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            var oldFiles = Directory.GetFiles(_defaultBackupFolder, "*.bak")
                .Select(f => new FileInfo(f))
                .Where(f => f.CreationTime < cutoffDate)
                .ToList();

            foreach (var file in oldFiles)
            {
                file.Delete();
                _logger.LogInformation("Deleted old backup: {File}", file.Name);
            }

            _logger.LogInformation("Cleanup complete. Deleted {Count} old backups", oldFiles.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup cleanup failed");
            return Result.Failure("حدث خطأ أثناء تنظيف النسخ الاحتياطية القديمة: " + ex.Message);
        }
    }

    private async Task TrySetMultiUserAsync(CancellationToken ct)
    {
        try
        {
            var connectionString = _dbFactory.GetDecryptedConnectionString();
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master"
            };

            await using var conn = new SqlConnection(builder.ConnectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand(
                $"ALTER DATABASE [{_databaseName}] SET MULTI_USER;", conn);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore MULTI_USER mode — manual intervention required");
        }
    }
}
