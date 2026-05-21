namespace SalesSystem.Contracts.Requests;

public record CreateUserRequest(string UserName, string Password, string FullName, byte Role);

public record UpdateUserRequest(string FullName, byte Role, bool IsActive, string? Password);
