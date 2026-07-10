using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Tests.Controllers.Settings;

public class SettingsControllerTests
{
    private readonly Mock<IStoreSettingsService> _settingsServiceMock;
    private readonly Mock<IPrintDataService> _printSettingsServiceMock;
    private readonly Mock<ILogger<SettingsController>> _loggerMock;
    private readonly SettingsController _controller;

    public SettingsControllerTests()
    {
        _settingsServiceMock = new Mock<IStoreSettingsService>();
        _printSettingsServiceMock = new Mock<IPrintDataService>();
        _loggerMock = new Mock<ILogger<SettingsController>>();
        _controller = new SettingsController(_settingsServiceMock.Object, _printSettingsServiceMock.Object);

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };

    }

    [Fact]
    public async Task Get_WhenSettingsExist_ReturnsOkWithSettings()
    {
        var settings = new StoreSettingsDto(1, "متجري", "0123456789", "الرياض", null, null, "SAR", 15m, true, null, true, false, true, "INV-");
        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StoreSettingsDto>.Success(settings));

        var result = await _controller.Get(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Get_WhenServiceFails_ReturnsBadRequest()
    {
        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StoreSettingsDto>.Failure("فشل في جلب الإعدادات"));

        var result = await _controller.Get(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedSettings()
    {
        var request = new UpdateSettingsRequest("متجري المحدث", "جدة", "0123456789", "info@store.com", null, "SAR", 15m, true, null, true, false, "INV-");
        var settings = new StoreSettingsDto(1, request.StoreName, request.Phone, request.Address, null, request.Email, request.Currency, request.DefaultTaxRate, request.IsTaxEnabled, request.TaxNumber, request.EnableStockAlerts, request.AllowNegativeStock, request.InvoicePrefix);

        _settingsServiceMock
            .Setup(x => x.UpdateSettingsAsync(It.IsAny<UpdateSettingsRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StoreSettingsDto>.Success(settings));

        var result = await _controller.Update(request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Update_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new UpdateSettingsRequest("متجري", "الرياض", "0123456789", null, null, "SAR", 15m, true, null, true, false, true, "INV-");

        _settingsServiceMock
            .Setup(x => x.UpdateSettingsAsync(It.IsAny<UpdateSettingsRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StoreSettingsDto>.Failure("فشل في تحديث الإعدادات"));

        var result = await _controller.Update(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        _settingsServiceMock
            .Setup(x => x.UpdateSettingsAsync(It.IsAny<UpdateSettingsRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StoreSettingsDto>.Failure("المنشأة غير موجودة", "NOT_FOUND"));

        var request = new UpdateSettingsRequest("متجري", "الرياض", "0123456789", null, null, "SAR", 15m, true, null, true, false, true, "INV-");
        var result = await _controller.Update(request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WithoutUserId_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new UpdateSettingsRequest("متجري", "الرياض", "0123456789", null, null, "SAR", 15m, true, null, true, false, true, "INV-");

        var result = await _controller.Update(request, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }
}
