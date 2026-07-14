namespace SalesSystem.Contracts.Responses;

public record LoginResponse(int UserId, string UserName, byte Role, string Token, DateTime ExpiresAt, bool MustChangePassword = false, long PermissionsMask = 0)
{
    // Backward-compatible properties
    public string FullName => UserName;
}
