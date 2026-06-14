namespace SalesSystem.Contracts.Responses;

public record DepartmentDto(
    int Id,
    int BranchId,
    string? BranchName,
    string Name,
    bool IsActive
);
