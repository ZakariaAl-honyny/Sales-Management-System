using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class User : BaseEntity
{
    public string UserName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }

    protected User() { } // EF Core

    public static User Create(string userName, string passwordHash, string fullName, UserRole role, int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainException("اسم المستخدم مطلوب.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("كلمة المرور مطلوبة.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("الاسم الكامل مطلوب.");

        var user = new User
        {
            UserName = userName,
            PasswordHash = passwordHash,
            FullName = fullName,
            Role = role,
            IsActive = true
        };
        user.SetCreatedBy(createdByUserId);
        return user;
    }

    public void Update(string fullName, UserRole role, int? updatedByUserId = null)
    {
        FullName = fullName;
        Role = role;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    public void ChangePassword(string newPasswordHash, int? updatedByUserId = null)
    {
        PasswordHash = newPasswordHash;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}