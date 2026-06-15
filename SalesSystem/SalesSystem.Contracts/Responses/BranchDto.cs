namespace SalesSystem.Contracts.Responses;

public record BranchDto(
    int Id,
    string Name,
    bool IsActive
);
