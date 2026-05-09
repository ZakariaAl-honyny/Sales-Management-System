namespace SalesSystem.Contracts.Requests;

public record CreateCategoryRequest(string Name, string? Description);
public record UpdateCategoryRequest(string Name, string? Description, bool IsActive);
