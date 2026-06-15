namespace SalesSystem.Contracts.Responses;

public record ProductCategoryDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive
);
