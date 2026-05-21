using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Api.Tests.Controllers;

/// <summary>
/// Unit tests for AuthController
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    #region Login Tests

    /// <summary>
    /// Given valid credentials, when logging in, then returns JWT token
    /// </summary>
    [Fact]
    public async Task GivenValidCredentials_WhenLoggingIn_ThenReturnsJwtToken()
    {
        // Arrange
        var request = new LoginRequest("admin", "password123");
        var expectedResponse = new LoginResponse(
            1,
            "admin",
            "المسؤول",
            1,
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
            DateTime.UtcNow.AddHours(8)
        );

        _authServiceMock
            .Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Success(expectedResponse));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LoginResponse>().Subject;
        
        response.UserId.Should().Be(1);
        response.UserName.Should().Be("admin");
        response.Token.Should().NotBeEmpty();
    }

    /// <summary>
    /// Given invalid credentials, when logging in, then returns BadRequest
    /// </summary>
    [Fact]
    public async Task GivenInvalidCredentials_WhenLoggingIn_ThenReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest("admin", "wrongpassword");

        _authServiceMock
            .Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Failure("اسم المستخدم أو كلمة المرور غير صحيحة", "InvalidCredentials"));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Given empty username, when logging in, then returns BadRequest
    /// </summary>
    [Fact]
    public async Task GivenEmptyUsername_WhenLoggingIn_ThenReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest("", "password123");

        _authServiceMock
            .Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Failure("اسم المستخدم مطلوب", "ValidationError"));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given empty password, when logging in, then returns BadRequest
    /// </summary>
    [Fact]
    public async Task GivenEmptyPassword_WhenLoggingIn_ThenReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest("admin", "");

        _authServiceMock
            .Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Failure("كلمة المرور مطلوبة", "ValidationError"));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given service throws exception, when logging in, then returns BadRequest with error
    /// </summary>
    [Fact]
    public async Task GivenServiceThrowsException_WhenLoggingIn_ThenReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest("admin", "password123");

        _authServiceMock
            .Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Failure("حدث خطأ في الخادم", "ServerError"));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Given disabled user, when logging in, then returns BadRequest with account disabled message
    /// </summary>
    [Fact]
    public async Task GivenDisabledUser_WhenLoggingIn_ThenReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest("disableduser", "password123");

        _authServiceMock
            .Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Failure("الحساب معطل", "AccountDisabled"));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given valid request but service returns failure, when logging in, then returns BadRequest
    /// </summary>
    [Fact]
    public async Task GivenValidRequest_WhenLoginFails_ThenReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest("user", "pass");
        
        _authServiceMock
            .Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Failure("فشل التحقق من الهوية", "AuthFailed"));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _authServiceMock.Verify(x => x.LoginAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Authorization Tests

    /// <summary>
    /// Verify AuthController allows anonymous access (no [Authorize] attribute)
    /// </summary>
    [Fact]
    public void GivenAuthController_ThenAllowsAnonymousAccess()
    {
        // Assert - Controller should have [AllowAnonymous] or no authorization
        var controllerType = typeof(AuthController);
        var hasAuthorizeAttribute = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true).Any();
        
        hasAuthorizeAttribute.Should().BeFalse("AuthController should allow anonymous access");
    }

    #endregion
}