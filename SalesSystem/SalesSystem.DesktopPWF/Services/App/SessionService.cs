using System.Collections.Generic;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Enums;

namespace SalesSystem.DesktopPWF.Services.App;

/// <summary>
/// Session service for managing JWT token and user info (in-memory)
/// </summary>
public class SessionService : ISessionService
{
    private string? _token;
    private string? _userName;
    private int? _userId;
    private List<int> _roleIds = new();
    private string? _roleName;
    private Permission _permissions = Permission.None;
    private ViewMode _viewMode = ViewMode.Basic;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public string? GetToken() => _token;
    public string? GetUserName() => _userName;
    public int? GetUserId() => _userId;
    public List<int>? GetUserRoleIds() => _roleIds.Count > 0 ? _roleIds : null;
    public string? GetUserRoleName() => _roleName;

    public bool HasRole(int roleId) => _roleIds.Contains(roleId);

    public bool IsAdmin => HasRole(1);

    public bool IsManagerOrAbove => HasRole(1) || HasRole(2);

    public ViewMode GetViewMode() => _viewMode;

    public void SetSession(string token, string userName, int userId, List<int> roleIds, string roleName)
    {
        _token = token;
        _userName = userName;
        _userId = userId;
        _roleIds = roleIds ?? new List<int>();
        _roleName = roleName;
        // Calculate permissions based on primary role
        var primaryRoleId = _roleIds.Count > 0 ? _roleIds[0] : 0;
        _permissions = primaryRoleId.GetPermissionsForRole();
        // Determine view mode based on role: Admin(1) and Manager(2) get Advanced, others get Basic
        _viewMode = primaryRoleId switch
        {
            1 => ViewMode.Advanced,  // Admin
            2 => ViewMode.Advanced,  // Manager
            _ => ViewMode.Basic      // Cashier, Observer, BranchManager
        };
    }

    public void ClearSession()
    {
        _token = null;
        _userName = null;
        _userId = null;
        _roleIds = new List<int>();
        _roleName = null;
        _permissions = Permission.None;
        _viewMode = ViewMode.Basic;
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

