namespace SalesSystem.Contracts.Responses;

public record HealthCheckDto(
    string Status,
    string DatabaseStatus,
    DateTime Timestamp
);
