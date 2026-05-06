using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Domain.Entities;

public class User : BaseEntity
{
    public string UserName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    protected User() { } // EF Core

    public static User Create(string userName, string passwordHash, string fullName, UserRole role, string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("UserName is required.", nameof(userName));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("FullName is required.", nameof(fullName));

        return new User
        {
            UserName = userName,
            PasswordHash = passwordHash,
            FullName = fullName,
            Role = role,
            CreatedBy = createdBy,
            IsActive = true
        };
    }

    public void Update(string fullName, UserRole role, string? updatedBy = null)
    {
        FullName = fullName;
        Role = role;
        UpdatedBy = updatedBy;
        UpdateTimestamp();
    }

    public void ChangePassword(string newPasswordHash, string? updatedBy = null)
    {
        PasswordHash = newPasswordHash;
        UpdatedBy = updatedBy;
        UpdateTimestamp();
    }
}