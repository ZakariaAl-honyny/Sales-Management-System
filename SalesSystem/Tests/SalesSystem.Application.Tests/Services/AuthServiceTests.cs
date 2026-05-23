using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
/// Unit tests for AuthService business logic.
/// </summary>
public class AuthServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly SalesSystem.Contracts.Common.JwtSettings _jwtSettings;

    private readonly AuthService _sut;

    public AuthServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] AuthServiceTests initialized");

        _mockUow = new Mock<IUnitOfWork>();
        _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        _jwtSettings = new SalesSystem.Contracts.Common.JwtSettings
        {
            Secret = "ThisIsAVeryLongSecretKeyForTestingPurposes123!",
            Issuer = "SalesSystem",
            Audience = "SalesSystemClients",
            ExpirationHours = 24
        };

        _sut = new AuthService(
            _mockUow.Object,
            _mockJwtGenerator.Object,
            _jwtSettings,
            _mockLogger.Object);
    }

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUserInfo()
    {
        _output.WriteLine("[TEST] LoginAsync_ValidCredentials_ReturnsTokenAndUserInfo");

        var user = User.Create("testuser", BCrypt.Net.BCrypt.HashPassword("password123", workFactor: 12), "Test User", UserRole.Admin);
        user.Restore();

        var usersList = new List<User> { user };
        _mockUow.Setup(u => u.Users.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);

        _mockJwtGenerator.Setup(g => g.GenerateToken(It.IsAny<User>()))
            .Returns("jwt-token-here");

        var request = new SalesSystem.Contracts.Requests.LoginRequest("testuser", "password123");

        var result = await _sut.LoginAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be("jwt-token-here");
        result.Value.UserName.Should().Be("testuser");
        result.Value.FullName.Should().Be("Test User");

        _output.WriteLine("[PASS] Valid credentials return token and user info");
    }

    [Fact]
    public async Task LoginAsync_InvalidUsername_ReturnsUnauthorized()
    {
        _output.WriteLine("[TEST] LoginAsync_InvalidUsername_ReturnsUnauthorized");

        var usersList = new List<User>();
        _mockUow.Setup(u => u.Users.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);

        var request = new SalesSystem.Contracts.Requests.LoginRequest("nonexistent", "password123");

        var result = await _sut.LoginAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("بيانات الاعتماد غير صالحة");

        _output.WriteLine("[PASS] Invalid username returns unauthorized");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsUnauthorized()
    {
        _output.WriteLine("[TEST] LoginAsync_InvalidPassword_ReturnsUnauthorized");

        var user = User.Create("testuser", BCrypt.Net.BCrypt.HashPassword("correctpassword", workFactor: 12), "Test User", UserRole.Admin);
        user.Restore();

        var usersList = new List<User> { user };
        _mockUow.Setup(u => u.Users.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);

        var request = new SalesSystem.Contracts.Requests.LoginRequest("testuser", "wrongpassword");

        var result = await _sut.LoginAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("بيانات الاعتماد غير صالحة");

        _output.WriteLine("[PASS] Invalid password returns unauthorized");
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsForbidden()
    {
        _output.WriteLine("[TEST] LoginAsync_InactiveUser_ReturnsForbidden");

        var user = User.Create("testuser", BCrypt.Net.BCrypt.HashPassword("password123", workFactor: 12), "Test User", UserRole.Admin);
        user.MarkAsDeleted(); // Inactive

        var usersList = new List<User> { user };
        _mockUow.Setup(u => u.Users.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);

        var request = new SalesSystem.Contracts.Requests.LoginRequest("testuser", "password123");

        var result = await _sut.LoginAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("الحساب معطل");

        _output.WriteLine("[PASS] Inactive user returns forbidden");
    }

    [Fact]
    public async Task LoginAsync_CaseInsensitiveUsername_ReturnsSuccess()
    {
        _output.WriteLine("[TEST] LoginAsync_CaseInsensitiveUsername_ReturnsSuccess");

        var user = User.Create("TestUser", BCrypt.Net.BCrypt.HashPassword("password123", workFactor: 12), "Test User", UserRole.Admin);
        user.Restore();

        var usersList = new List<User> { user };
        _mockUow.Setup(u => u.Users.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);

        _mockJwtGenerator.Setup(g => g.GenerateToken(It.IsAny<User>()))
            .Returns("jwt-token-here");

        var request = new SalesSystem.Contracts.Requests.LoginRequest("TESTUSER", "password123"); // Different case

        var result = await _sut.LoginAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserName.Should().Be("TestUser");

        _output.WriteLine("[PASS] Case-insensitive username returns success");
    }

    [Fact]
    public async Task LoginAsync_ExpirationTimeInResponse_IsCorrect()
    {
        _output.WriteLine("[TEST] LoginAsync_ExpirationTimeInResponse_IsCorrect");

        var user = User.Create("testuser", BCrypt.Net.BCrypt.HashPassword("password123", workFactor: 12), "Test User", UserRole.Admin);
        user.Restore();

        var usersList = new List<User> { user };
        _mockUow.Setup(u => u.Users.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);

        _mockJwtGenerator.Setup(g => g.GenerateToken(It.IsAny<User>()))
            .Returns("jwt-token-here");

        var request = new SalesSystem.Contracts.Requests.LoginRequest("testuser", "password123");

        var beforeLogin = DateTime.UtcNow;
        var result = await _sut.LoginAsync(request, CancellationToken.None);
        var afterLogin = DateTime.UtcNow;

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExpiresAt.Should().BeAfter(beforeLogin.AddHours(_jwtSettings.ExpirationHours).AddMinutes(-1));
        result.Value.ExpiresAt.Should().BeBefore(afterLogin.AddHours(_jwtSettings.ExpirationHours).AddMinutes(1));

        _output.WriteLine($"[DEBUG] Token expires at: {result.Value.ExpiresAt}");
        _output.WriteLine("[PASS] Expiration time is correctly calculated");
    }

    #endregion
}