using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateUserWithoutPassword()
    {
        var user = User.Create(
            userName: "john.doe",
            fullName: "John Doe",
            role: UserRole.Manager,
            createdByUserId: 1
        );

        user.UserName.Should().Be("john.doe");
        user.PasswordHash.Should().BeNull();
        user.FullName.Should().Be("John Doe");
        user.Role.Should().Be(UserRole.Manager);
        user.Status.Should().Be(UserStatus.Active);
        user.MustChangePassword.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidUserName_ShouldThrowDomainException(string? invalidUserName)
    {
        var action = () => User.Create(
            userName: invalidUserName!,
            fullName: "Test",
            role: UserRole.Cashier
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم المستخدم مطلوب*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidFullName_ShouldThrowDomainException(string? invalidFullName)
    {
        var action = () => User.Create(
            userName: "testuser",
            fullName: invalidFullName!,
            role: UserRole.Cashier
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*الاسم الكامل مطلوب*");
    }

    [Fact]
    public void CreateWithPassword_GivenValidData_ShouldCreateUserWithPassword()
    {
        var user = User.CreateWithPassword(
            userName: "john.doe",
            passwordHash: "hashedpassword123",
            fullName: "John Doe",
            role: UserRole.Manager,
            createdByUserId: 1
        );

        user.UserName.Should().Be("john.doe");
        user.PasswordHash.Should().Be("hashedpassword123");
        user.FullName.Should().Be("John Doe");
        user.Role.Should().Be(UserRole.Manager);
        user.IsActive.Should().BeTrue();
        user.MustChangePassword.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateWithPassword_GivenInvalidPasswordHash_ShouldThrowDomainException(string? invalidPassword)
    {
        var action = () => User.CreateWithPassword(
            userName: "testuser",
            passwordHash: invalidPassword!,
            fullName: "Test",
            role: UserRole.Cashier
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*كلمة المرور مطلوبة*");
    }

    [Fact]
    public void Create_GivenAllRoles_ShouldCreateSuccessfully()
    {
        foreach (UserRole role in Enum.GetValues<UserRole>())
        {
            var user = User.Create(
                userName: $"user_{role}",
                fullName: $"User {role}",
                role: role
            );

            user.Role.Should().Be(role);
        }
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateUser()
    {
        var user = User.Create(
            userName: "john.doe",
            fullName: "Original Name",
            role: UserRole.Cashier,
            createdByUserId: 1
        );

        user.Update(
            fullName: "Updated Name",
            role: UserRole.Manager,
            updatedByUserId: 1
        );

        user.FullName.Should().Be("Updated Name");
        user.Role.Should().Be(UserRole.Manager);
    }

    [Fact]
    public void Update_GivenDifferentRole_ShouldUpdateRole()
    {
        var user = User.Create(
            userName: "test",
            fullName: "Test",
            role: UserRole.Cashier,
            createdByUserId: 1
        );

        user.Update(fullName: "Test", role: UserRole.Admin, updatedByUserId: 1);

        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void SetInitialPassword_GivenValidHash_ShouldSetPassword()
    {
        var user = User.Create(
            userName: "test",
            fullName: "Test",
            role: UserRole.Cashier,
            createdByUserId: 1
        );

        user.SetInitialPassword("new_hashed_password");

        user.PasswordHash.Should().Be("new_hashed_password");
        user.MustChangePassword.Should().BeFalse();
        user.PasswordChangedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetInitialPassword_WhenAlreadySet_ShouldThrow()
    {
        var user = User.Create(
            userName: "test",
            fullName: "Test",
            role: UserRole.Cashier,
            createdByUserId: 1
        );

        user.SetInitialPassword("first_hash");

        var action = () => user.SetInitialPassword("second_hash");
        action.Should().Throw<DomainException>()
            .WithMessage("*كلمة المرور تم تعيينها مسبقاً*");
    }

    [Fact]
    public void ChangePassword_GivenValidHash_ShouldUpdatePassword()
    {
        var user = User.Create(
            userName: "test",
            fullName: "Test",
            role: UserRole.Cashier,
            createdByUserId: 1
        );
        user.SetInitialPassword("old_hash");

        user.ChangePassword(newPasswordHash: "new_hash", updatedByUserId: 1);

        user.PasswordHash.Should().Be("new_hash");
    }

    [Fact]
    public void ChangePassword_GivenSameHash_ShouldUpdateSuccessfully()
    {
        var user = User.Create(
            userName: "test",
            fullName: "Test",
            role: UserRole.Cashier,
            createdByUserId: 1
        );
        user.SetInitialPassword("original_hash");

        user.ChangePassword(newPasswordHash: "original_hash", updatedByUserId: 2);

        user.PasswordHash.Should().Be("original_hash");
    }

    [Fact]
    public void ResetPassword_ShouldClearHashAndForceChange()
    {
        var user = User.Create(
            userName: "test",
            fullName: "Test",
            role: UserRole.Cashier,
            createdByUserId: 1
        );
        user.SetInitialPassword("some_hash");

        user.ResetPassword();

        user.PasswordHash.Should().BeNull();
        user.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public void RecordLoginAttempt_Success_ShouldResetCounterAndUpdateLastLogin()
    {
        var user = User.Create(userName: "test", fullName: "Test", role: UserRole.Cashier);

        user.RecordLoginAttempt(success: true);

        user.LoginAttempts.Should().Be(0);
        user.Status.Should().Be(UserStatus.Active);
        user.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordLoginAttempt_Failure_ShouldIncrementCounter()
    {
        var user = User.Create(userName: "test", fullName: "Test", role: UserRole.Cashier);

        user.RecordLoginAttempt(success: false);
        user.RecordLoginAttempt(success: false);
        user.RecordLoginAttempt(success: false);
        user.RecordLoginAttempt(success: false);

        user.LoginAttempts.Should().Be(4);
        user.Status.Should().Be(UserStatus.Active);

        // 5th failure locks the account
        user.RecordLoginAttempt(success: false);
        user.LoginAttempts.Should().Be(5);
        user.Status.Should().Be(UserStatus.Locked);
    }

    [Fact]
    public void LockAndUnlock_ShouldChangeStatus()
    {
        var user = User.Create(userName: "test", fullName: "Test", role: UserRole.Cashier);

        user.Lock();
        user.Status.Should().Be(UserStatus.Locked);

        user.Unlock();
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void DeactivateAndActivate_ShouldChangeStatus()
    {
        var user = User.Create(userName: "test", fullName: "Test", role: UserRole.Cashier);

        user.Deactivate();
        user.Status.Should().Be(UserStatus.Inactive);

        user.Activate();
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void MarkAsDeleted_ShouldSetStatusInactive()
    {
        var user = User.Create(userName: "test", fullName: "Test", role: UserRole.Cashier);

        user.MarkAsDeleted();

        user.Status.Should().Be(UserStatus.Inactive);
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Restore_ShouldSetStatusActive()
    {
        var user = User.Create(userName: "test", fullName: "Test", role: UserRole.Cashier);
        user.MarkAsDeleted();

        user.Restore();

        user.Status.Should().Be(UserStatus.Active);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_GivenCreatedByUserId_ShouldSetCreatedBy()
    {
        var user = User.Create(
            userName: "test",
            fullName: "Test",
            role: UserRole.Cashier,
            createdByUserId: 99
        );

        user.CreatedByUserId.Should().Be(99);
    }

    [Fact]
    public void SetAvatarAndClearAvatar_ShouldUpdatePath()
    {
        var user = User.Create(userName: "test", fullName: "Test", role: UserRole.Cashier);

        user.SetAvatar("/images/avatar.png");
        user.AvatarPath.Should().Be("/images/avatar.png");

        user.ClearAvatar();
        user.AvatarPath.Should().BeNull();
    }
}
