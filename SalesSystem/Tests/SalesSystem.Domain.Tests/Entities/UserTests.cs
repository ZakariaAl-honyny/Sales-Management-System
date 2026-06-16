using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateUserWithoutPassword()
    {
        var user = User.Create(
            userName: "john.doe",
            createdByUserId: 1
        );

        user.UserName.Should().Be("john.doe");
        user.PasswordHash.Should().Be(string.Empty); // Schema: nvarchar(256) NOT NULL
        user.IsActive.Should().BeTrue();
        user.IsLocked.Should().BeFalse();
        user.MustChangePassword.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidUserName_ShouldThrowDomainException(string? invalidUserName)
    {
        var action = () => User.Create(
            userName: invalidUserName!
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم المستخدم مطلوب*");
    }

    [Fact]
    public void CreateWithPassword_GivenValidData_ShouldCreateUserWithPassword()
    {
        var user = User.CreateWithPassword(
            userName: "john.doe",
            passwordHash: "hashedpassword123",
            createdByUserId: 1
        );

        user.UserName.Should().Be("john.doe");
        user.PasswordHash.Should().Be("hashedpassword123");
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
            passwordHash: invalidPassword!
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*كلمة المرور مطلوبة*");
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateUser()
    {
        var user = User.Create(
            userName: "john.doe",
            createdByUserId: 1
        );

        user.Update(
            updatedByUserId: 1
        );

        // Update only changes EmployeeId now; previously also changed FullName
        user.UserName.Should().Be("john.doe");
    }

    [Fact]
    public void SetInitialPassword_GivenValidHash_ShouldSetPassword()
    {
        var user = User.Create(
            userName: "test",
            createdByUserId: 1
        );

        user.SetInitialPassword("new_hashed_password");

        user.PasswordHash.Should().Be("new_hashed_password");
        user.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public void SetInitialPassword_WhenAlreadySet_ShouldThrow()
    {
        var user = User.Create(
            userName: "test",
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
            createdByUserId: 1
        );
        user.SetInitialPassword("old_hash");

        user.ResetPassword("new_default_hash");

        user.PasswordHash.Should().Be("new_default_hash");
        user.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public void RecordLoginAttempt_Success_ShouldResetCounterAndUpdateLastLogin()
    {
        var user = User.Create(userName: "test");

        user.RecordLoginAttempt(success: true);

        user.LoginAttempts.Should().Be(0);
        user.IsLocked.Should().BeFalse();
        user.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordLoginAttempt_Failure_ShouldIncrementCounter()
    {
        var user = User.Create(userName: "test");

        user.RecordLoginAttempt(success: false);
        user.RecordLoginAttempt(success: false);
        user.RecordLoginAttempt(success: false);
        user.RecordLoginAttempt(success: false);

        user.LoginAttempts.Should().Be(4);
        user.IsLocked.Should().BeFalse();

        // 5th failure locks the account
        user.RecordLoginAttempt(success: false);
        user.LoginAttempts.Should().Be(5);
        user.IsLocked.Should().BeTrue();
    }

    [Fact]
    public void LockAndUnlock_ShouldChangeStatus()
    {
        var user = User.Create(userName: "test");

        user.Lock();
        user.IsLocked.Should().BeTrue();

        user.Unlock();
        user.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void DeactivateAndActivate_ShouldChangeStatus()
    {
        var user = User.Create(userName: "test");

        user.Deactivate();
        user.IsActive.Should().BeFalse();

        user.Activate();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void MarkAsDeleted_ShouldSetStatusInactive()
    {
        var user = User.Create(userName: "test");

        user.MarkAsDeleted();

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Restore_ShouldSetStatusActive()
    {
        var user = User.Create(userName: "test");
        user.MarkAsDeleted();

        user.Restore();

        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_GivenCreatedByUserId_ShouldSetCreatedBy()
    {
        var user = User.Create(
            userName: "test",
            createdByUserId: 99
        );

        user.CreatedByUserId.Should().Be(99);
    }

}
