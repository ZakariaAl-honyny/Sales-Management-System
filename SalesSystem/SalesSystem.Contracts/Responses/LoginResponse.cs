namespace SalesSystem.Contracts.Responses;

public record LoginResponse(string Token, string UserName, string FullName, byte Role, DateTime ExpiresAt);