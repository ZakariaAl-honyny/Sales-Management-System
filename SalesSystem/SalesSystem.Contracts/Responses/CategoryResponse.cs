namespace SalesSystem.Contracts.Responses;

public record CategoryResponse(int Id, string Name, string? Description, bool IsActive);
