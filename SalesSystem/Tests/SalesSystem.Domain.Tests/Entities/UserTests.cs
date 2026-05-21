using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateUser()
    {
        var user = User.Create(
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
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidUserName_ShouldThrowDomainException(string? invalidUserName)
    {
        var action = () => User.Create(
            userName: invalidUserName!,
            passwordHash: "hash",
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
    public void Create_GivenInvalidPasswordHash_ShouldThrowDomainException(string? invalidPassword)
    {
        var action = () => User.Create(
            userName: "testuser",
            passwordHash: invalidPassword!,
            fullName: "Test",
            role: UserRole.Cashier
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*كلمة المرور مطلوبة*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidFullName_ShouldThrowDomainException(string? invalidFullName)
    {
        var action = () => User.Create(
            userName: "testuser",
            passwordHash: "hash",
            fullName: invalidFullName!,
            role: UserRole.Cashier
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*الاسم الكامل مطلوب*");
    }

    [Fact]
    public void Create_GivenAllRoles_ShouldCreateSuccessfully()
    {
        foreach (UserRole role in Enum.GetValues<UserRole>())
        {
            var user = User.Create(
                userName: $"user_{role}",
                passwordHash: "hash",
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
            passwordHash: "hash",
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
            passwordHash: "hash",
            fullName: "Test",
            role: UserRole.Cashier,
            createdByUserId: 1
        );

        user.Update(fullName: "Test", role: UserRole.Admin, updatedByUserId: 1);

        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void ChangePassword_GivenValidHash_ShouldUpdatePassword()
    {
        var user = User.Create(
            userName: "test",
            passwordHash: "old_hash",
            fullName: "Test",
            role: UserRole.Cashier,
            createdByUserId: 1
        );

        user.ChangePassword(newPasswordHash: "new_hash", updatedByUserId: 1);

        user.PasswordHash.Should().Be("new_hash");
    }

    [Fact]
    public void ChangePassword_GivenSameHash_ShouldUpdateSuccessfully()
    {
        var user = User.Create(
            userName: "test",
            passwordHash: "original_hash",
            fullName: "Test",
            role: UserRole.Cashier,
            createdByUserId: 1
        );

        user.ChangePassword(newPasswordHash: "original_hash", updatedByUserId: 2);

        user.PasswordHash.Should().Be("original_hash");
    }

    [Fact]
    public void Create_GivenCreatedByUserId_ShouldSetCreatedBy()
    {
        var user = User.Create(
            userName: "test",
            passwordHash: "hash",
            fullName: "Test",
            role: UserRole.Cashier,
            createdByUserId: 99
        );

        user.CreatedByUserId.Should().Be(99);
    }

    [Fact]
    public void ChangePassword_WithNoUserId_ShouldSucceed()
    {
        var user = User.Create(
            userName: "test",
            passwordHash: "hash",
            fullName: "Test",
            role: UserRole.Cashier
        );

        var action = () => user.ChangePassword(newPasswordHash: "new_hash");

        action.Should().NotThrow();
        user.PasswordHash.Should().Be("new_hash");
    }
}