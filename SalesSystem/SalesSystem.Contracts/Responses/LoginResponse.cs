namespace SalesSystem.Contracts.Responses;

public record LoginResponse(int UserId, string UserName, string FullName, byte Role, string Token, DateTime ExpiresAt, bool MustChangePassword = false);
