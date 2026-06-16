using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using System.Linq.Expressions;
using Xunit.Abstractions;
using UserRoleEntity = SalesSystem.Domain.Entities.UserRole;

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

    private readonly Mock<IGenericRepository<Role>> _mockRoles;
    private readonly Mock<IGenericRepository<UserRoleEntity>> _mockUserRoles;

    private readonly UserService _sut;

    public UserServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] UserServiceTests initialized");

        _mockUow = new Mock<IUnitOfWork>();
        _mockPermissionService = new Mock<IPermissionService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockLogger = new Mock<ILogger<UserService>>();

        // Set up Role and UserRole repositories (needed by CreateAsync, UpdateAsync, DeleteAsync)
        _mockRoles = new Mock<IGenericRepository<Role>>();
        _mockUserRoles = new Mock<IGenericRepository<UserRoleEntity>>();

        _mockUow.Setup(m => m.Roles).Returns(_mockRoles.Object);
        _mockUow.Setup(m => m.UserRoles).Returns(_mockUserRoles.Object);

        // Default: any role lookup returns a valid role entity
        _mockRoles.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Role.Create("Admin"));

        // Default: UserRole ToListAsync returns empty list
        _mockUserRoles.Setup(r => r.ToListAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserRoleEntity, bool>>>(),
                It.IsAny<Func<IQueryable<UserRoleEntity>, IQueryable<UserRoleEntity>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(new List<UserRoleEntity>());

        _mockUserRoles.Setup(r => r.ToListAsync(
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(new List<UserRoleEntity>());

        _mockUserRoles.Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserRoleEntity, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sut = new UserService(_mockUow.Object, _mockPermissionService.Object, _mockAuditLogService.Object, _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsDto()
    {
        _output.WriteLine("[TEST] GetByIdAsync_ExistingUser_ReturnsDto");

        var user = User.CreateWithPassword("testuser", "hash123");

        _mockUow.Setup(u => u.Users.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _sut.GetByIdAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserName.Should().Be("testuser");
        result.Value.IsLocked.Should().BeFalse();

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

        var request = new CreateUserRequest("newuser", (byte)1);

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserName.Should().Be("newuser");

        // Verify user is created with a BCrypt hash of the default "12345678" password
        // and MustChangePassword = true
        _mockUow.Verify(u => u.Users.AddAsync(
            It.Is<User>(user =>
                user.PasswordHash != null &&
                BCrypt.Net.BCrypt.Verify("12345678", user.PasswordHash) &&
                user.MustChangePassword),
            It.IsAny<CancellationToken>()), Times.Once,
            "User should be created with BCrypt hash of the default password");

        _output.WriteLine("[PASS] CreateAsync creates user with default password");
    }

    [Fact]
    public async Task CreateAsync_WithCustomPassword_CreatesUserWithCustomPasswordHash()
    {
        _output.WriteLine("[TEST] CreateAsync_WithCustomPassword_CreatesUserWithCustomPasswordHash");

        _mockUow.Setup(u => u.Users.AnyIgnoreFiltersAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockUow.Setup(u => u.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken ct) => u);

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var customPassword = "MySecurePass!2026";
        var request = new CreateUserRequest("newuser", (byte)1, Password: customPassword);

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserName.Should().Be("newuser");

        // Verify the user is created with a BCrypt hash of the custom password
        // and that the hash is NOT valid for the default password
        _mockUow.Verify(u => u.Users.AddAsync(
            It.Is<User>(user =>
                user.PasswordHash != null &&
                BCrypt.Net.BCrypt.Verify(customPassword, user.PasswordHash) &&
                !BCrypt.Net.BCrypt.Verify("12345678", user.PasswordHash) &&
                user.MustChangePassword),
            It.IsAny<CancellationToken>()), Times.Once,
            "User should be created with BCrypt hash of the custom password, not the default");

        _output.WriteLine("[PASS] CreateAsync with custom password uses the custom password hash");
    }

    [Fact]
    public async Task CreateAsync_DuplicateUsername_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_DuplicateUsername_ReturnsFailure");

        var existingUser = User.CreateWithPassword("existinguser", "hash123");

        _mockUow.Setup(u => u.Users.AnyIgnoreFiltersAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateUserRequest("existinguser", (byte)1); // Duplicate (case-insensitive)

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("اسم المستخدم موجود بالفعل");

        _output.WriteLine("[PASS] Duplicate username returns failure");
    }

    [Fact]
    public async Task CreateAsync_CaseInsensitiveDuplicate_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateAsync_CaseInsensitiveDuplicate_ReturnsFailure");

        var existingUser = User.CreateWithPassword("TestUser", "hash123");

        _mockUow.Setup(u => u.Users.AnyIgnoreFiltersAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateUserRequest("TESTUSER", (byte)1); // Same name different case

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

        var user = User.CreateWithPassword("testuser", "hash123");

        _mockUow.Setup(u => u.Users.FirstOrDefaultIgnoreFiltersAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(user);

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new UpdateUserRequest((byte)2);

        var result = await _sut.UpdateAsync(1, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.UserName.Should().Be("testuser");

        _output.WriteLine("[PASS] UpdateAsync updates user correctly");
    }

    [Fact]
    public async Task UpdateAsync_WithPasswordChange_UpdatesHashedPassword()
    {
        _output.WriteLine("[TEST] UpdateAsync_WithPasswordChange_UpdatesHashedPassword");

        var user = User.CreateWithPassword("testuser", "oldhash");

        _mockUow.Setup(u => u.Users.FirstOrDefaultIgnoreFiltersAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(user);

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new UpdateUserRequest((byte)1, Password: "newpassword123");

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

        var request = new UpdateUserRequest((byte)1);

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

        var user = User.CreateWithPassword("testuser", "hash123");
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

        var admin = User.CreateWithPassword("admin", "hash123");
        admin.Restore();

        _mockUow.Setup(u => u.Users.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        // UserRoles.AnyAsync for admin role check → return true (user has admin role)
        _mockUserRoles.Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserRoleEntity, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // UserRoles.ToListAsync for active admin count → return a UserRole with the admin user
        var role = Role.Create("Admin");
        var userRole = UserRoleEntity.Create(admin.Id, role.Id);
        var userRoles = new List<UserRoleEntity> { userRole };

        _mockUserRoles.Setup(r => r.ToListAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserRoleEntity, bool>>>(),
                It.IsAny<Func<IQueryable<UserRoleEntity>, IQueryable<UserRoleEntity>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(userRoles);

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
            User.CreateWithPassword("user1", "hash1"),
            User.CreateWithPassword("user2", "hash2"),
            User.CreateWithPassword("user3", "hash3")
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
