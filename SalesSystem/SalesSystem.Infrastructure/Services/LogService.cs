using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
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
            var level = logLevel switch
            {
                "Info" => (byte)1,
                "Warning" => (byte)2,
                "Error" => (byte)3,
                "Fatal" => (byte)4,
                _ => (byte)2
            };

            var log = SystemLog.Create(level, message, source, exception);
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

    public async Task<Result<PagedResult<SystemLogDto>>> QueryLogsAsync(
        int? level, string? source, string? search,
        DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct = default)
    {
        try
        {
            var (items, totalCount) = await _uow.SystemLogs.GetAllAsync(level, source, search, from, to, page, pageSize, ct);
            var dtos = items.Select(x => new SystemLogDto(
                x.Id,
                null,           // LogLevel (string) — mapped from Level byte
                x.Level,        // Level (byte?)
                x.Message,
                x.Exception,
                null,           // StackTrace
                x.Source,
                null,           // Context
                null,           // MachineName
                x.CreatedAt)).ToList();

            return Result<PagedResult<SystemLogDto>>.Success(
                new PagedResult<SystemLogDto> { Items = dtos, TotalCount = totalCount, Page = page, PageSize = pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query system logs");
            return Result<PagedResult<SystemLogDto>>.Failure("حدث خطأ أثناء استعلام سجلات النظام");
        }
    }
}
