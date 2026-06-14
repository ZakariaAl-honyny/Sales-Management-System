namespace SalesSystem.Contracts.Requests;

public record CreateRoleRequest(string Name, string? Description);

public record UpdateRoleRequest(string Name, string? Description, bool IsActive = true);
