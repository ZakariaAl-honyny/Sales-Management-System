using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces.Services;

namespace SalesSystem.Infrastructure.Backup;

public sealed class ScheduledBackupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ScheduledBackupWorker> _logger;

    public ScheduledBackupWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ScheduledBackupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled backup worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var nextRun = DateTime.Today.AddHours(2);
                if (now.Hour >= 2)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;

                _logger.LogInformation("Next automatic backup scheduled at {Time}", nextRun);

                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Scheduled backup worker cancelled during delay");
                break;
            }

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();

                var result = await backupService.CreateBackupAsync(ct: stoppingToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Automatic backup completed: {File}",
                        result.Value);

                    var retentionDays = int.TryParse(
                        _configuration["Backup:RetentionDays"], out var days) ? days : 30;

                    await backupService.DeleteOldBackupsAsync(retentionDays, stoppingToken);
                }
                else
                {
                    _logger.LogError(
                        "Automatic backup FAILED: {Error}", result.Error);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Scheduled backup worker cancelled during backup");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during automatic backup");
            }
        }

        _logger.LogInformation("Scheduled backup worker stopped");
    }
}
