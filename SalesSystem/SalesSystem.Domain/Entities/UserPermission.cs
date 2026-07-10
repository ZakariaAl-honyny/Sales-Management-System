using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class UserPermission : Entity
{
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;
    public int PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;
    public bool IsGranted { get; private set; }

    protected UserPermission() { }

    public static UserPermission Create(int userId, int permissionId, bool isGranted)
    {
        if (userId <= 0) throw new DomainException("معرف المستخدم مطلوب");
        if (permissionId <= 0) throw new DomainException("معرف الصلاحية مطلوب");
        return new UserPermission { UserId = userId, PermissionId = permissionId, IsGranted = isGranted };
    }
}
