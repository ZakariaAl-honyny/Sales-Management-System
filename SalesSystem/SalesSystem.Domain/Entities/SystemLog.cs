using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Entity for system-wide logging and audit trail.
/// Schema Module 8.2: SystemLogs table with bigint PK, Level as tinyint, Source, Message, Exception, CreatedAt.
/// Extends BaseEntityLong for high-volume bigint Id.
/// </summary>
public class SystemLog : LongEntity
{
    /// <summary>
    /// Log level as string (e.g. "Info", "Warning", "Error", "Fatal") for detailed filtering.
    /// </summary>
    public string LogLevel { get; private set; } = string.Empty;

    /// <summary>
    /// Log level as tinyint matching schema (1=Info, 2=Warning, 3=Error).
    /// </summary>
    public byte? Level { get; private set; }

    /// <summary>
    /// The log message content.
    /// </summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Exception details if this log represents an error.
    /// </summary>
    public string? Exception { get; private set; }

    public string? StackTrace { get; private set; }

    /// <summary>
    /// Source of the log entry (e.g. "API", "Desktop", "BackgroundService").
    /// </summary>
    public string? Source { get; private set; }

    /// <summary>
    /// Context information (e.g. method name, screen name).
    /// </summary>
    public string? Context { get; private set; }

    public string? MachineName { get; private set; }

    private SystemLog() { }

    public static SystemLog Create(
        string logLevel,
        string message,
        string? exception = null,
        string? stackTrace = null,
        string? source = null,
        string? context = null,
        int? userId = null,
        string? machineName = null,
        byte? level = null)
    {
        if (string.IsNullOrWhiteSpace(logLevel))
            throw new DomainException("مستوى السجل مطلوب.");
        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("رسالة السجل مطلوبة.");

        var log = new SystemLog
        {
            LogLevel = logLevel,
            Level = level ?? logLevel.ToLowerInvariant() switch
            {
                "error" or "fatal" => (byte)3,
                "warning" or "warn" => (byte)2,
                _ => (byte)1
            },
            Message = message,
            Exception = exception,
            StackTrace = stackTrace,
            Source = source,
            Context = context,
            MachineName = machineName
        };

        if (userId.HasValue) log.SetCreatedBy(userId.Value);
        return log;
    }
}
