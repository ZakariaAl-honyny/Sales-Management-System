namespace SalesSystem.Contracts.Requests;

// ─── User CRUD ────────────────────────────────────────

public record CreateUserRequest(
    string UserName,
    string FullName,
    byte Role,
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
    string ConfirmPassword);

public record ResetUserPasswordRequest(int UserId);
