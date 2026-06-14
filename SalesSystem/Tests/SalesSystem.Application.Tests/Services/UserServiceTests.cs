using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using System.Linq.Expressions;
using Xunit.Abstractions;

namespace SalesSystem.Application.Tests.Services;

/// <summary>
/// Unit tests for UserService business logic.
/// </summary>
public class UserServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IPermissionService> _mockPermissionService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ILogger<UserService>> _mockLogger;

    private readonly UserService _sut;

    public UserServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] UserServiceTests initialized");

        _mockUow = new Mock<IUnitOfWork>();
        _mockPermissionService = new Mock<IPermissionService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockLogger = new Mock<ILogger<UserService>>();

        _sut = new UserService(_mockUow.Object, _mockPermissionService.Object, _mockAuditLogService.Object, _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsDto()
    {
        _output.WriteLine("[TEST] GetByIdAsync_ExistingUser_ReturnsDto");

        var user = User.CreateWithPassword("testuser", "hash123", "Test User");

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
    public async Task CreateAsync_ValidRequest_CreatesUserWithDefaultPassword()
    {
        _output.WriteLine("[TEST] CreateAsync_ValidRequest_CreatesUserWithDefaultPassword");

        _mockUow.Setup(u => u.Users.AnyIgnoreFiltersAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockUow.Setup(u => u.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken ct) => u);

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new CreateUserRequest("newuser", "New User", (byte)1);

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserName.Should().Be("newuser");

        // Verify user is created with a hashed default password and MustChangePassword = true
        _mockUow.Verify(u => u.Users.AddAsync(
            It.Is<User>(user => user.PasswordHash != null && user.MustChangePassword),
            It.IsAny<CancellationToken>()), Times.Once,
            "User should be created with default password hash");

        _output.WriteLine("[PASS] CreateAsync creates user with default password");
    }

    [Fact]
    public async Task CreateAsync_DuplicateUsername_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_DuplicateUsername_ReturnsFailure");

        var existingUser = User.CreateWithPassword("existinguser", "hash123", "Existing User");

        _mockUow.Setup(u => u.Users.AnyIgnoreFiltersAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateUserRequest("existinguser", "New User", (byte)1); // Duplicate (case-insensitive)

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("اسم المستخدم موجود بالفعل");

        _output.WriteLine("[PASS] Duplicate username returns failure");
    }

    [Fact]
    public async Task CreateAsync_CaseInsensitiveDuplicate_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_CaseInsensitiveDuplicate_ReturnsFailure");

        var existingUser = User.CreateWithPassword("TestUser", "hash123", "Test User");

        _mockUow.Setup(u => u.Users.AnyIgnoreFiltersAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateUserRequest("TESTUSER", "Another User", (byte)1); // Same name different case

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

        var user = User.CreateWithPassword("testuser", "hash123", "Original Name");

        _mockUow.Setup(u => u.Users.FirstOrDefaultIgnoreFiltersAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(user);

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new UpdateUserRequest("Updated Name", (byte)2, (byte)UserStatus.Active, null);

        var result = await _sut.UpdateAsync(1, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FullName.Should().Be("Updated Name");

        _output.WriteLine("[PASS] UpdateAsync updates user correctly");
    }

    [Fact]
    public async Task UpdateAsync_WithPasswordChange_UpdatesHashedPassword()
    {
        _output.WriteLine("[TEST] UpdateAsync_WithPasswordChange_UpdatesHashedPassword");

        var user = User.CreateWithPassword("testuser", "oldhash", "Test User");

        _mockUow.Setup(u => u.Users.FirstOrDefaultIgnoreFiltersAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(user);

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new UpdateUserRequest("Test User", (byte)1, (byte)UserStatus.Active, "newpassword123");

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

        _mockUow.Setup(u => u.Users.FirstOrDefaultIgnoreFiltersAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync((User?)null);

        var request = new UpdateUserRequest("Updated", (byte)1, (byte)UserStatus.Active, null);

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

        var user = User.CreateWithPassword("testuser", "hash123", "Test User");
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

        var admin = User.CreateWithPassword("admin", "hash123", "Admin User");
        admin.Restore();

        var users = new List<User> { admin };

        _mockUow.Setup(u => u.Users.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        _mockUow.Setup(u => u.Users.CountIgnoreFiltersAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

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
            User.CreateWithPassword("user1", "hash1", "User One"),
            User.CreateWithPassword("user2", "hash2", "User Two"),
            User.CreateWithPassword("user3", "hash3", "User Three")
        };

        _mockUow.Setup(u => u.Users.ToListAsync(It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(users);

        var result = await _sut.GetAllAsync(false, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);

        _output.WriteLine("[PASS] GetAllAsync returns all users");
    }

    #endregion
}
