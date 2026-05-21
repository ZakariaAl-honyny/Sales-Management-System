using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Enums;
using SalesSystem.Contracts.Enums;

namespace SalesSystem.DesktopPWF.Services.App;

/// <summary>
/// Session service for managing JWT token and user info (in-memory)
/// </summary>
public class SessionService : ISessionService
{
    private string? _token;
    private string? _userName;
    private int? _userId;
    private UserRole? _role;
    private Permission _permissions = Permission.None;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public string? GetToken() => _token;
    public string? GetUserName() => _userName;
    public int? GetUserId() => _userId;
    public UserRole? GetUserRole() => _role;

    public void SetSession(string token, string userName, int userId, UserRole role)
    {
        _token = token;
        _userName = userName;
        _userId = userId;
        _role = role;
        // Calculate permissions based on role
        _permissions = role.GetPermissionsForRole();
    }

    public void ClearSession()
    {
        _token = null;
        _userName = null;
        _userId = null;
        _role = null;
        _permissions = Permission.None;
    }

    public bool HasPermission(UserRole requiredRole)
    {
        if (!_role.HasValue) return false;

        return _role.Value switch
        {
            UserRole.Admin => true,
            UserRole.Manager => requiredRole != UserRole.Admin,
            UserRole.Cashier => requiredRole == UserRole.Cashier,
            _ => false
        };
    }

    /// <summary>
    /// Check if current user has specific permission (UI access control)
    /// </summary>
    public bool CanAccess(Permission permission)
    {
        return _permissions.HasPermission(permission);
    }

    /// <summary>
    /// Get all permissions for current user
    /// </summary>
    public Permission GetPermissions() => _permissions;
}

