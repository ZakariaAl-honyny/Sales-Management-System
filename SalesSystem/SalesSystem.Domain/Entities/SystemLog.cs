using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Entity for system-wide logging and audit trail
/// </summary>
public class SystemLog : BaseEntity
{
    public string LogLevel { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? Exception { get; private set; }
    public string? StackTrace { get; private set; }
    public string? Source { get; private set; } // Desktop or API
    public string? Context { get; private set; } // Method name or Screen
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
        string? machineName = null)
    {
        if (string.IsNullOrWhiteSpace(logLevel))
            throw new DomainException("مستوى السجل مطلوب.");
        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("رسالة السجل مطلوبة.");

        var log = new SystemLog
        {
            LogLevel = logLevel,
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
