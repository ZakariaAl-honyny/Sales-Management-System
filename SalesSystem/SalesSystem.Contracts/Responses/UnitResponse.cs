namespace SalesSystem.Contracts.Responses;

public record UnitResponse(int Id, string Name, string Symbol, string? Description, bool IsActive);
