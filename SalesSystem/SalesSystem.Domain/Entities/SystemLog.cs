using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// System-level logging for error tracking and monitoring.
/// Schema 8.2: SystemLogs — bigint PK, high-volume log.
/// Columns: Level (tinyint: 1=Info,2=Warning,3=Error,4=Critical), Source, Message, Exception (JSON), CreatedAt.
/// </summary>
public class SystemLog : LongEntity
{
    /// <summary>
    /// Log level: 1=Info, 2=Warning, 3=Error, 4=Critical.
    /// </summary>
    public byte Level { get; private set; }

    /// <summary>
    /// Component that produced the log entry (e.g., "API", "BackgroundService").
    /// </summary>
    public string? Source { get; private set; }

    /// <summary>
    /// The log message content.
    /// </summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Serialized exception details (stack trace, etc.) as JSON or plain text.
    /// </summary>
    public string? Exception { get; private set; }

    private SystemLog() { } // EF Core

    public static SystemLog Create(
        byte level,
        string message,
        string? source = null,
        string? exception = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("رسالة السجل مطلوبة.");
        if (level < 1 || level > 4)
            throw new DomainException("مستوى السجل يجب أن يكون بين 1 و 4.");

        return new SystemLog
        {
            Level = level,
            Source = source,
            Message = message,
            Exception = exception
        };
    }
}
