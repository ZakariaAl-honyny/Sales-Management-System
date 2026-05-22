using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Services;

/// <summary>
/// Stores client logs in the database via IUnitOfWork.
/// Also forwards critical logs to Serilog on the server side.
/// </summary>
public class LogService : ILogService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<LogService> _logger;

    public LogService(IUnitOfWork uow, ILogger<LogService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result> CreateLogAsync(
        string logLevel,
        string message,
        string? exception,
        string? stackTrace,
        string? source,
        string? context,
        int? userId,
        string? machineName,
        CancellationToken ct = default)
    {
        try
        {
            var log = SystemLog.Create(logLevel, message, exception, stackTrace, source, context, userId, machineName);
            await _uow.SystemLogs.AddAsync(log, ct);
            await _uow.SaveChangesAsync(ct);

            // Also log to Serilog on server side for visibility
            if (logLevel == "Error" || logLevel == "Fatal")
            {
                _logger.LogError("Client Log [{Source}]: {Message} | Context: {Context} | Machine: {Machine}",
                    source, message, context, machineName);
            }
            else
            {
                _logger.LogWarning("Client Log [{Source}]: {Message} | Context: {Context} | Machine: {Machine}",
                    source, message, context, machineName);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store client log");
            return Result.Failure("فشل تخزين السجل");
        }
    }
}
