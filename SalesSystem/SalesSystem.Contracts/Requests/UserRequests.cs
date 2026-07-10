namespace SalesSystem.Contracts.Requests;

// ─── User CRUD ────────────────────────────────────────

public record CreateUserRequest(
    string UserName,
    byte Role,
    string? Password = null,
    int? DefaultCashBoxId = null,
    List<int>? RoleIds = null);

public record UpdateUserRequest(
    byte Role,
    bool? IsLocked = null,
    string? Password = null,
    int? DefaultCashBoxId = null);

// ─── Password management ──────────────────────────────

public record SetPasswordRequest(
    string Password,
    string ConfirmPassword,
    string Token);  // Required: one-time password reset token (from admin reset or creation flow)

public record ResetUserPasswordRequest(int UserId);
