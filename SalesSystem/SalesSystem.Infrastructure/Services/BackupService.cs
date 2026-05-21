using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using System.Data;

namespace SalesSystem.Infrastructure.Services;

public sealed class BackupService : IBackupService
{
    private readonly string _connectionString;
    private readonly string _databaseName;
    private readonly ILogger<BackupService> _logger;
    private readonly string _defaultBackupFolder;

    public BackupService(IConfiguration configuration, ILogger<BackupService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("SALESSYSTEM_DB_CONNECTION")
            ?? throw new InvalidOperationException("Connection string not found.");

        var builder = new SqlConnectionStringBuilder(_connectionString);
        _databaseName = builder.InitialCatalog;
        _logger = logger;

        // Default backup folder in application directory
        _defaultBackupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
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

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            var query = $"BACKUP DATABASE [{_databaseName}] TO DISK = @path WITH FORMAT, MEDIANAME = 'SalesSystemBackup', NAME = 'Full Backup of {_databaseName}'";
            using var command = new SqlCommand(query, connection);
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

            // To restore, we need to connect to 'master' database to close connections to our DB
            var builder = new SqlConnectionStringBuilder(_connectionString)
            {
                InitialCatalog = "master"
            };

            using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(ct);

            // 1. Set to single user mode to kick everyone out
            var sqlSetSingle = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
            using (var cmd = new SqlCommand(sqlSetSingle, connection))
            {
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // 2. Restore
            var sqlRestore = $"RESTORE DATABASE [{_databaseName}] FROM DISK = @path WITH REPLACE";
            using (var cmd = new SqlCommand(sqlRestore, connection))
            {
                cmd.Parameters.AddWithValue("@path", filePath);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // 3. Set back to multi user mode
            var sqlSetMulti = $"ALTER DATABASE [{_databaseName}] SET MULTI_USER";
            using (var cmd = new SqlCommand(sqlSetMulti, connection))
            {
                await cmd.ExecuteNonQueryAsync(ct);
            }

            _logger.LogInformation("Database restored successfully from {FilePath}", filePath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring database");
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
}
