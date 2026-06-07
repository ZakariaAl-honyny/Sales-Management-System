namespace SalesSystem.Contracts.Requests;

// ─── User CRUD ────────────────────────────────────────

public record CreateUserRequest(
    string UserName,
    string FullName,
    byte Role,
    string? Password = null,
    string? Phone = null,
    string? Email = null,
    int? DefaultCashBoxId = null);

public record UpdateUserRequest(
    string FullName,
    byte Role,
    byte Status,
    string? Password,
    string? Phone = null,
    string? Email = null,
    int? DefaultCashBoxId = null);

// ─── Password management ──────────────────────────────

public record SetPasswordRequest(
    string Password,
    string ConfirmPassword,
    string Token);  // Required: one-time password reset token (from admin reset or creation flow)

public record ResetUserPasswordRequest(int UserId);
