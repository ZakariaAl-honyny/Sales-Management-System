namespace SalesSystem.Contracts.Responses;

public record DepartmentDto(
    int Id,
    string Name,
    bool IsActive
);
