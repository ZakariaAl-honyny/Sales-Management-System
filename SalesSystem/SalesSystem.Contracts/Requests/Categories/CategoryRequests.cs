namespace SalesSystem.Contracts.Requests.Categories;

public record CreateCategoryRequest(string Name, string? Description);

public record UpdateCategoryRequest(int Id,string Name, string? Description, bool IsActive);