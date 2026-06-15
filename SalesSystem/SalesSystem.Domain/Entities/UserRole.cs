using SalesSystem.Domain.Common;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Join entity linking a User to a Role (many-to-many).
/// A user can have multiple roles; a role can be assigned to multiple users.
/// Unique constraint: (UserId, RoleId).
///
/// NOTE: Pure join entity — no audit fields, no IsActive. Extends Entity (Id only).
/// </summary>
public class UserRole : Entity
{
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;
    public short RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    protected UserRole() { } // EF Core

    public static UserRole Create(int userId, short roleId)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };
    }
}
