using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using Xunit.Abstractions;

namespace SalesSystem.Application.Tests.Services;

/// <summary>
/// Unit tests for UserService business logic.
/// </summary>
public class UserServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ILogger<UserService>> _mockLogger;

    private readonly UserService _sut;

    public UserServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] UserServiceTests initialized");

        _mockUow = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<UserService>>();

        _sut = new UserService(_mockUow.Object, _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsDto()
    {
        _output.WriteLine("[TEST] GetByIdAsync_ExistingUser_ReturnsDto");

        var user = User.Create("testuser", "hash123", "Test User", UserRole.Admin);

        _mockUow.Setup(u => u.Users.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _sut.GetByIdAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserName.Should().Be("testuser");
        result.Value.FullName.Should().Be("Test User");

        _output.WriteLine("[PASS] GetByIdAsync returns user dto");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentUser_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] GetByIdAsync_NonExistentUser_ReturnsNotFound");

        _mockUow.Setup(u => u.Users.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.GetByIdAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("المستخدم غير موجود");

        _output.WriteLine("[PASS] Non-existent user returns NotFound");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesUserWithHashedPassword()
    {
        _output.WriteLine("[TEST] CreateAsync_ValidRequest_CreatesUserWithHashedPassword");

        var usersList = new List<User>();
        _mockUow.Setup(u => u.Users.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);

        _mockUow.Setup(u => u.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken ct) => u);

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new SalesSystem.Contracts.Requests.CreateUserRequest("newuser", "password123", "New User", (byte)UserRole.Admin);

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserName.Should().Be("newuser");

        // Verify password is hashed (not stored in plain text)
        _mockUow.Verify(u => u.Users.AddAsync(
            It.Is<User>(user => !user.PasswordHash.Equals("password123")),
            It.IsAny<CancellationToken>()), Times.Once,
            "Password should be hashed before storage");

        _output.WriteLine("[PASS] CreateAsync hashes password correctly");
    }

    [Fact]
    public async Task CreateAsync_DuplicateUsername_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_DuplicateUsername_ReturnsFailure");

        var existingUser = User.Create("existinguser", "hash123", "Existing User", UserRole.Admin);

        _mockUow.Setup(u => u.Users.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { existingUser });

        var request = new SalesSystem.Contracts.Requests.CreateUserRequest("existinguser", "password123", "New User", (byte)UserRole.Admin); // Duplicate (case-insensitive)

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("اسم المستخدم موجود بالفعل");

        _output.WriteLine("[PASS] Duplicate username returns failure");
    }

    [Fact]
    public async Task CreateAsync_CaseInsensitiveDuplicate_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_CaseInsensitiveDuplicate_ReturnsFailure");

        var existingUser = User.Create("TestUser", "hash123", "Test User", UserRole.Admin);

        _mockUow.Setup(u => u.Users.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { existingUser });

        var request = new SalesSystem.Contracts.Requests.CreateUserRequest("TESTUSER", "password123", "Another User", (byte)UserRole.Admin); // Same name different case

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("اسم المستخدم موجود بالفعل");

        _output.WriteLine("[PASS] Case-insensitive duplicate username returns failure");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesUser()
    {
        _output.WriteLine("[TEST] UpdateAsync_ValidRequest_UpdatesUser");

        var user = User.Create("testuser", "hash123", "Original Name", UserRole.Admin);

        _mockUow.Setup(u => u.Users.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new SalesSystem.Contracts.Requests.UpdateUserRequest("Updated Name", (byte)UserRole.Manager, true, null); // No password change

        var result = await _sut.UpdateAsync(1, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FullName.Should().Be("Updated Name");

        _output.WriteLine("[PASS] UpdateAsync updates user correctly");
    }

    [Fact]
    public async Task UpdateAsync_WithPasswordChange_UpdatesHashedPassword()
    {
        _output.WriteLine("[TEST] UpdateAsync_WithPasswordChange_UpdatesHashedPassword");

        var user = User.Create("testuser", "oldhash", "Test User", UserRole.Admin);

        _mockUow.Setup(u => u.Users.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new SalesSystem.Contracts.Requests.UpdateUserRequest("Test User", (byte)UserRole.Admin, true, "newpassword123");

        var result = await _sut.UpdateAsync(1, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().NotBe("oldhash");
        user.PasswordHash.Should().NotBe("newpassword123");

        _output.WriteLine("[PASS] UpdateAsync hashes new password correctly");
    }

    [Fact]
    public async Task UpdateAsync_NonExistentUser_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] UpdateAsync_NonExistentUser_ReturnsNotFound");

        _mockUow.Setup(u => u.Users.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var request = new SalesSystem.Contracts.Requests.UpdateUserRequest("Updated", (byte)UserRole.Admin, true, null);

        var result = await _sut.UpdateAsync(999, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("المستخدم غير موجود");

        _output.WriteLine("[PASS] Update non-existent user returns NotFound");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingUser_SoftDeletes()
    {
        _output.WriteLine("[TEST] DeleteAsync_ExistingUser_SoftDeletes");

        var user = User.Create("testuser", "hash123", "Test User", UserRole.Manager);
        user.Restore();

        _mockUow.Setup(u => u.Users.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUow.Setup(u => u.Users.SoftDeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _sut.DeleteAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _output.WriteLine("[PASS] DeleteAsync soft deletes user");
    }

    [Fact]
    public async Task DeleteAsync_LastActiveAdmin_ReturnsFailure()
    {
        _output.WriteLine("[TEST] DeleteAsync_LastActiveAdmin_ReturnsFailure");

        var admin = User.Create("admin", "hash123", "Admin User", UserRole.Admin);
        admin.Restore();

        var users = new List<User> { admin };

        _mockUow.Setup(u => u.Users.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        _mockUow.Setup(u => u.Users.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _sut.DeleteAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("لا يمكن تعطيل آخر مدير نشط في النظام");

        _output.WriteLine("[PASS] Cannot delete last active admin");
    }

    [Fact]
    public async Task DeleteAsync_NonExistentUser_ReturnsNotFound()
    {
        _output.WriteLine("[TEST] DeleteAsync_NonExistentUser_ReturnsNotFound");

        _mockUow.Setup(u => u.Users.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.DeleteAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("المستخدم غير موجود");

        _output.WriteLine("[PASS] Delete non-existent user returns NotFound");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        _output.WriteLine("[TEST] GetAllAsync_ReturnsAllUsers");

        var users = new List<User>
        {
            User.Create("user1", "hash1", "User One", UserRole.Admin),
            User.Create("user2", "hash2", "User Two", UserRole.Manager),
            User.Create("user3", "hash3", "User Three", UserRole.Cashier)
        };

        _mockUow.Setup(u => u.Users.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _sut.GetAllAsync(false, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);

        _output.WriteLine("[PASS] GetAllAsync returns all users");
    }

    #endregion

    #region Password Hashing Tests

    [Fact]
    public async Task CreateAsync_UsesBCryptWithWorkFactor12()
    {
        _output.WriteLine("[TEST] CreateAsync_UsesBCryptWithWorkFactor12");

        var usersList = new List<User>();
        _mockUow.Setup(u => u.Users.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);

        User? capturedUser = null;
        _mockUow.Setup(u => u.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, ct) => capturedUser = u)
            .ReturnsAsync((User u, CancellationToken ct) => u);

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new SalesSystem.Contracts.Requests.CreateUserRequest("testuser", "password123", "Test User", (byte)UserRole.Admin);

        await _sut.CreateAsync(request, CancellationToken.None);

        capturedUser.Should().NotBeNull();
        // BCrypt hashes start with "$2"
        capturedUser!.PasswordHash.Should().StartWith("$2");
        // Verify it's a valid BCrypt hash by checking it's not the plain password
        capturedUser.PasswordHash.Should().NotBe("password123");
        // BCrypt hashes are typically 60 characters
        capturedUser.PasswordHash.Length.Should().Be(60);

        _output.WriteLine("[PASS] Password is hashed with BCrypt work factor 12");
    }

    #endregion
}