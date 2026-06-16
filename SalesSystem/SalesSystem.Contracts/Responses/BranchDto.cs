namespace SalesSystem.Contracts.Responses;

/// <summary>
/// Branch response DTO — matches Branches table schema (smallint PK, audit fields, no Code).
/// </summary>
public record BranchDto(
    short Id,
    string Name,
    string? Phone,
    string? Address,
    string? ManagerName,
    string? Notes,
    bool IsActive
);
