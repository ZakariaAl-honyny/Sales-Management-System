using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Tests.Controllers.Settings;

/// <summary>
/// Unit tests for SettingsController HTTP status codes
/// </summary>
public class SettingsControllerTests
{
    private readonly Mock<IStoreSettingsService> _settingsServiceMock;
    private readonly SettingsController _controller;

    public SettingsControllerTests()
    {
        _settingsServiceMock = new Mock<IStoreSettingsService>();
        _controller = new SettingsController(_settingsServiceMock.Object);
        
        // Setup controller context with user claims
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };
    }

    #region Get Tests

    /// <summary>
    /// Given settings exist, when getting settings, then returns 200 OK
    /// </summary>
    [Fact]
    public async Task Get_WhenSettingsExist_ReturnsOkWithSettings()
    {
        // Arrange
        var settings = new StoreSettingsDto("متجري", "0123456789", "الرياض", "الوصف", "شركة", "10%", 15);

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StoreSettingsDto>.Success(settings));

        // Act
        var result = await _controller.Get(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given service fails, when getting settings, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Get_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StoreSettingsDto>.Failure("فشل في جلب الإعدادات"));

        // Act
        var result = await _controller.Get(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    /// <summary>
    /// Given valid request, when updating settings, then returns 200 OK
    /// </summary>
    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedSettings()
    {
        // Arrange
        var request = new UpdateSettingsRequest("متجري المحدث", "0123456789", "جدة", "وصف جديد", "شركة", "15%", 20);
        var settings = new StoreSettingsDto(request.StoreName, request.Phone, request.Address, request.Description, request.BusinessType, request.TaxRate, request.CreditLimit);

        _settingsServiceMock
            .Setup(x => x.UpdateSettingsAsync(It.IsAny<UpdateSettingsRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StoreSettingsDto>.Success(settings));

        // Act
        var result = await _controller.Update(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given service fails, when updating settings, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateSettingsRequest("متجري", "0123456789", "الرياض", "الوصف", "شركة", "10%", 15);

        _settingsServiceMock
            .Setup(x => x.UpdateSettingsAsync(It.IsAny<UpdateSettingsRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StoreSettingsDto>.Failure("فشل في تحديث الإعدادات"));

        // Act
        var result = await _controller.Update(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given no user id, when updating settings, then returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task Update_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new UpdateSettingsRequest("متجري", "0123456789", "الرياض", "الوصف", "شركة", "10%", 15);

        // Act
        var result = await _controller.Update(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion
}