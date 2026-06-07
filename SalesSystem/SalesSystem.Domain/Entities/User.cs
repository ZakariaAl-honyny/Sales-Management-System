using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class User : BaseEntity
{
    public string UserName { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }          // Nullable — passwordless setup
    public string FullName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public bool MustChangePassword { get; private set; } = true;
    public DateTime? PasswordChangedAt { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? AvatarPath { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public int LoginAttempts { get; private set; }
    public int? DefaultCashBoxId { get; private set; }

    // ─── Password Reset Token ──────────────────────
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }

    // Navigation
    public CashBox? DefaultCashBox { get; private set; }
    public User? CreatedByUser { get; private set; }

    protected User() { } // EF Core

    /// <summary>
    /// Creates a new user WITHOUT a password (passwordless setup).
    /// MustChangePassword is set to true — use <see cref="SetInitialPassword"/> or
    /// <see cref="ChangePassword"/> to set the password later.
    /// </summary>
    public static User Create(string userName, string fullName, UserRole role,
        string? phone = null, string? email = null, int? defaultCashBoxId = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainException("اسم المستخدم مطلوب.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("الاسم الكامل مطلوب.");

        var user = new User
        {
            UserName = userName.Trim(),
            FullName = fullName.Trim(),
            Role = role,
            Status = UserStatus.Active,
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            DefaultCashBoxId = defaultCashBoxId,
            MustChangePassword = true,
            PasswordHash = null,
            LoginAttempts = 0,
            IsActive = true
        };
        user.SetCreatedBy(createdByUserId);
        return user;
    }

    /// <summary>
    /// Creates a user directly with a pre-hashed password (internal/seed/admin use only).
    /// MustChangePassword is false by default for seeds.
    /// </summary>
    public static User CreateWithPassword(string userName, string passwordHash, string fullName,
        UserRole role, string? phone = null, string? email = null, int? defaultCashBoxId = null,
        int? createdByUserId = null, bool mustChangePassword = false)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainException("اسم المستخدم مطلوب.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("كلمة المرور مطلوبة.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("الاسم الكامل مطلوب.");

        var user = new User
        {
            UserName = userName.Trim(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Role = role,
            Status = UserStatus.Active,
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            DefaultCashBoxId = defaultCashBoxId,
            MustChangePassword = mustChangePassword,
            PasswordChangedAt = mustChangePassword ? null : DateTime.UtcNow,
            LoginAttempts = 0,
            IsActive = true
        };
        user.SetCreatedBy(createdByUserId);
        return user;
    }

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Updates the user's profile information.
    /// </summary>
    public void Update(string fullName, UserRole role, string? phone = null,
        string? email = null, int? defaultCashBoxId = null, int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("الاسم الكامل مطلوب.");
        FullName = fullName.Trim();
        Role = role;
        Phone = phone?.Trim();
        Email = email?.Trim();
        DefaultCashBoxId = defaultCashBoxId;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

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
        PasswordChangedAt = DateTime.UtcNow;
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
        PasswordChangedAt = DateTime.UtcNow;
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
        PasswordChangedAt = null;
        LoginAttempts = 0;
        UpdateTimestamp();
    }

    /// <summary>
    /// Records a login attempt. On success, resets attempts and updates LastLoginAt.
    /// On failure, increments attempts and locks the account after 5 failures.
    /// </summary>
    public void RecordLoginAttempt(bool success)
    {
        if (success)
        {
            LoginAttempts = 0;
            Status = UserStatus.Active;
            LastLoginAt = DateTime.UtcNow;
        }
        else
        {
            LoginAttempts++;
            if (LoginAttempts >= 5)
                Status = UserStatus.Locked;
        }
        UpdateTimestamp();
    }

    // ─── Status Management ────────────────────────

    public void Lock()
    {
        Status = UserStatus.Locked;
        UpdateTimestamp();
    }

    public void Unlock()
    {
        Status = UserStatus.Active;
        LoginAttempts = 0;
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
        UpdateTimestamp();
    }

    public void Activate()
    {
        Status = UserStatus.Active;
        UpdateTimestamp();
    }

    // ─── Avatar Management ────────────────────────

    public void SetAvatar(string avatarPath) => AvatarPath = avatarPath;
    public void ClearAvatar() => AvatarPath = null;

    // ─── Password Reset Token ─────────────────────
    /// <summary>
    /// Generates a one-time password reset token and stores it as plaintext
    /// (high-entropy, short-lived, one-time use — acceptable for reset tokens).
    /// Sets MustChangePassword to force the user to set a new password on next login.
    /// </summary>
    public void GeneratePasswordResetToken(string token, int expiryHours = 24)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new DomainException("رمز إعادة تعيين كلمة المرور مطلوب.");
        if (expiryHours < 1 || expiryHours > 168)
            throw new DomainException("فترة صلاحية الرمز يجب أن تكون بين ساعة و 168 ساعة (7 أيام).");

        PasswordResetToken = token;
        PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(expiryHours);
        PasswordHash = null;
        MustChangePassword = true;
        LoginAttempts = 0;
        UpdateTimestamp();
    }

    /// <summary>
    /// Validates the provided token against the stored reset token and expiry.
    /// Returns true if the token is valid and not expired.
    /// </summary>
    public bool IsPasswordResetTokenValid(string token) =>
        !string.IsNullOrWhiteSpace(token) &&
        PasswordResetToken == token &&
        PasswordResetTokenExpiresAt.HasValue &&
        PasswordResetTokenExpiresAt.Value > DateTime.UtcNow;

    /// <summary>
    /// Consumes (clears) the password reset token after successful password set.
    /// </summary>
    public void ConsumePasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiresAt = null;
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
        Status = UserStatus.Inactive;
        base.MarkAsDeleted();
    }

    public override void Restore()
    {
        Status = UserStatus.Active;
        base.Restore();
    }
}
