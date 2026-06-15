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
            createdByUserId: 1
        );

        user.UserName.Should().Be("john.doe");
        user.PasswordHash.Should().BeNull();
        user.FullName.Should().Be("John Doe");
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
            fullName: "Test"
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
            fullName: invalidFullName!
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
            createdByUserId: 1
        );

        user.UserName.Should().Be("john.doe");
        user.PasswordHash.Should().Be("hashedpassword123");
        user.FullName.Should().Be("John Doe");
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
            fullName: "Test"
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*كلمة المرور مطلوبة*");
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateUser()
    {
        var user = User.Create(
            userName: "john.doe",
            fullName: "Original Name",
            createdByUserId: 1
        );

        user.Update(
            fullName: "Updated Name",
            updatedByUserId: 1
        );

        user.FullName.Should().Be("Updated Name");
    }

    [Fact]
    public void SetInitialPassword_GivenValidHash_ShouldSetPassword()
    {
        var user = User.Create(
            userName: "test",
            fullName: "Test",
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
            createdByUserId: 1
        );
        user.SetInitialPassword("original_hash");

        user.ChangePassword(newPasswordHash: "original_hash", updatedByUserId: 2);

        user.PasswordHash.Should().Be("original_hash");
    }

    [Fact]
    public void ResetPassword_ShouldSetNewHashAndForceChange()
    {
        var user = User.Create(
            userName: "test",
            fullName: "Test",
            createdByUserId: 1
        );
        user.SetInitialPassword("old_hash");

        user.ResetPassword("new_default_hash");

        user.PasswordHash.Should().Be("new_default_hash");
        user.MustChangePassword.Should().BeTrue();
        user.PasswordChangedAt.Should().BeNull();
    }

    [Fact]
    public void RecordLoginAttempt_Success_ShouldResetCounterAndUpdateLastLogin()
    {
        var user = User.Create(userName: "test", fullName: "Test");

        user.RecordLoginAttempt(success: true);

        user.LoginAttempts.Should().Be(0);
        user.Status.Should().Be(UserStatus.Active);
        user.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordLoginAttempt_Failure_ShouldIncrementCounter()
    {
        var user = User.Create(userName: "test", fullName: "Test");

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
        var user = User.Create(userName: "test", fullName: "Test");

        user.Lock();
        user.Status.Should().Be(UserStatus.Locked);

        user.Unlock();
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void DeactivateAndActivate_ShouldChangeStatus()
    {
        var user = User.Create(userName: "test", fullName: "Test");

        user.Deactivate();
        user.Status.Should().Be(UserStatus.Inactive);

        user.Activate();
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void MarkAsDeleted_ShouldSetStatusInactive()
    {
        var user = User.Create(userName: "test", fullName: "Test");

        user.MarkAsDeleted();

        user.Status.Should().Be(UserStatus.Inactive);
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Restore_ShouldSetStatusActive()
    {
        var user = User.Create(userName: "test", fullName: "Test");
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
            createdByUserId: 99
        );

        user.CreatedByUserId.Should().Be(99);
    }

}
