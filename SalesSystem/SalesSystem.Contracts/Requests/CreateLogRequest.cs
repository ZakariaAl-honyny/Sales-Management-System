namespace SalesSystem.Contracts.Requests;

public record CreateLogRequest(
    string LogLevel,
    string Message,
    string? Exception,
    string? StackTrace,
    string? Source,
    string? Context,
    string? MachineName);
