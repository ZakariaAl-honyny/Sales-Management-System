using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a system user for authentication and authorization.
/// Schema §1.9 — Users table. Uses two bit flags for state:
///   IsActive (from ActivatableEntity): active=1, soft-deleted=0
///   IsLocked: unlocked=0, locked=1 (after 5 failed login attempts)
/// Roles are assigned via many-to-many UserRole join table.
/// Uses passwordless creation by default (MustChangePassword = true).
/// </summary>
public class User : ActivatableEntity
{
    public string UserName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public int? EmployeeId { get; private set; }               // Optional link to employee record
    public bool IsLocked { get; private set; }                  // true = account locked (max failed attempts)
    public bool MustChangePassword { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }
    public short LoginAttempts { get; private set; }

    // ─── Navigation ─────────────────────────────────
    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private readonly List<UserBranch> _userBranches = new();
    public IReadOnlyCollection<UserBranch> UserBranches => _userBranches.AsReadOnly();

    protected User() { } // EF Core

    /// <summary>
    /// Creates a new user WITHOUT a password (passwordless setup).
    /// MustChangePassword is set to true — use <see cref="SetInitialPassword"/> or
    /// <see cref="ChangePassword"/> to set the password later.
    /// Roles/branches are assigned separately via UserRole/UserBranch join entities.
    /// </summary>
    public static User Create(string userName, int? employeeId = null, int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainException("اسم المستخدم مطلوب.");

        var user = new User
        {
            UserName = userName.Trim(),
            EmployeeId = employeeId,
            MustChangePassword = true,
            LoginAttempts = 0
        };
        user.SetCreatedBy(createdByUserId);
        return user;
    }

    /// <summary>
    /// Creates a user directly with a pre-hashed password (internal/seed/admin use only).
    /// MustChangePassword is false by default for seeds.
    /// </summary>
    public static User CreateWithPassword(string userName, string passwordHash,
        int? employeeId = null, int? createdByUserId = null,
        bool mustChangePassword = false)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainException("اسم المستخدم مطلوب.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("كلمة المرور مطلوبة.");

        var user = new User
        {
            UserName = userName.Trim(),
            PasswordHash = passwordHash,
            EmployeeId = employeeId,
            MustChangePassword = mustChangePassword,
            LoginAttempts = 0
        };
        user.SetCreatedBy(createdByUserId);
        return user;
    }

    // ─── Update Method ─────────────────────────────

    /// <summary>
    /// Updates the user's profile information.
    /// Does NOT change UserName or password — those have dedicated methods.
    /// </summary>
    public void Update(int? employeeId = null, int? updatedByUserId = null)
    {
        EmployeeId = employeeId;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    // ─── Password Management ────────────────────────

    /// <summary>
    /// Sets the initial password for a user created without one.
    /// Only allowed when MustChangePassword is true.
    /// </summary>
    public void SetInitialPassword(string passwordHash)
    {
        if (!MustChangePassword)
            throw new DomainException("كلمة المرور تم تعيينها مسبقاً.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("كلمة المرور مطلوبة.");
        PasswordHash = passwordHash;
        MustChangePassword = false;
        UpdateTimestamp();
    }

    /// <summary>
    /// Changes an existing password (requires a non-empty new hash).
    /// </summary>
    public void ChangePassword(string newPasswordHash, int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("كلمة المرور الجديدة مطلوبة.");
        PasswordHash = newPasswordHash;
        MustChangePassword = false;
        LoginAttempts = 0;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Resets the password to a new hash (admin-initiated reset).
    /// Sets MustChangePassword = true to force the user to change on next login.
    /// </summary>
    public void ResetPassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("كلمة المرور مطلوبة.");
        PasswordHash = newPasswordHash;
        MustChangePassword = true;
        LoginAttempts = 0;
        UpdateTimestamp();
    }

    // ─── Login Tracking ─────────────────────────────

    /// <summary>
    /// Records a login attempt. On success, resets attempts and updates LastLoginAt.
    /// On failure, increments attempts and locks the account after 5 failures.
    /// </summary>
    public void RecordLoginAttempt(bool success)
    {
        if (success)
        {
            LoginAttempts = 0;
            IsLocked = false;
            LastLoginAt = DateTime.UtcNow;
        }
        else
        {
            LoginAttempts++;
            if (LoginAttempts >= 5)
                IsLocked = true;
        }
        UpdateTimestamp();
    }

    // ─── Status Management ────────────────────────

    public void Lock()
    {
        IsLocked = true;
        UpdateTimestamp();
    }

    public void Unlock()
    {
        IsLocked = false;
        LoginAttempts = 0;
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
    }

    // ─── Password Policy ──────────────────────────

    public void SetMustChangePassword(bool value)
    {
        MustChangePassword = value;
        UpdateTimestamp();
    }

    // ─── Soft Delete Overrides ────────────────────

    public override void MarkAsDeleted()
    {
        IsActive = false;
        IsLocked = false;
        base.MarkAsDeleted();
    }

    public override void Restore()
    {
        IsActive = true;
        IsLocked = false;
        base.Restore();
    }

    // ─── Employee Link ────────────────────────────

    public void SetEmployeeId(int? employeeId)
    {
        EmployeeId = employeeId;
        UpdateTimestamp();
    }
}
