namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to create a new branch.
/// </summary>
public record CreateBranchRequest(
    string Name,
    string? Phone = null,
    string? Address = null,
    string? ManagerName = null,
    string? Notes = null);

/// <summary>
/// Request to update an existing branch.
/// </summary>
public record UpdateBranchRequest(
    string Name,
    string? Phone = null,
    string? Address = null,
    string? ManagerName = null,
    string? Notes = null);
