using SalesSystem.Domain.Common;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Join entity linking a User to a Branch (many-to-many).
/// Determines which branches a user has access to.
/// Unique constraint: (UserId, BranchId).
/// </summary>
public class UserBranch : Entity
{
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;
    public short BranchId { get; private set; }
    public Branch Branch { get; private set; } = null!;

    protected UserBranch() { } // EF Core

    public static UserBranch Create(int userId, short branchId)
    {
        return new UserBranch
        {
            UserId = userId,
            BranchId = branchId
        };
    }
}
