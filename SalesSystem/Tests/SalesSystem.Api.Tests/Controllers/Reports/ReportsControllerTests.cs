using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Tests.Controllers.Reports;

/// <summary>
/// Unit tests for ReportsController HTTP status codes
/// </summary>
public class ReportsControllerTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _controller = new ReportsController(_reportServiceMock.Object);
    }

    #region Sales Report Tests

    /// <summary>
    /// Given valid date range, when getting sales report, then returns 200 OK
    /// </summary>
    [Fact]
    public async Task GetSalesReport_WhenValidDateRange_ReturnsOkWithReport()
    {
        // Arrange
        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;
        var report = new SalesReportDto(from, to, 10000m, 50, 5000m, 5000m);

        _reportServiceMock
            .Setup(x => x.GetSalesReportAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SalesReportDto>.Success(report));

        // Act
        var result = await _controller.GetSalesReport(from, to, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given from date after to date, when getting sales report, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetSalesReport_WhenFromDateAfterToDate_ReturnsBadRequest()
    {
        // Arrange
        var from = DateTime.Now;
        var to = DateTime.Now.AddDays(-30);

        // Act
        var result = await _controller.GetSalesReport(from, to, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given service fails, when getting sales report, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetSalesReport_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;

        _reportServiceMock
            .Setup(x => x.GetSalesReportAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SalesReportDto>.Failure("فشل في جلب تقرير المبيعات"));

        // Act
        var result = await _controller.GetSalesReport(from, to, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Purchases Report Tests

    /// <summary>
    /// Given valid date range, when getting purchases report, then returns 200 OK
    /// </summary>
    [Fact]
    public async Task GetPurchasesReport_WhenValidDateRange_ReturnsOkWithReport()
    {
        // Arrange
        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;
        var report = new PurchasesReportDto(from, to, 8000m, 40, 4000m, 4000m);

        _reportServiceMock
            .Setup(x => x.GetPurchasesReportAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PurchasesReportDto>.Success(report));

        // Act
        var result = await _controller.GetPurchasesReport(from, to, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given from date after to date, when getting purchases report, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetPurchasesReport_WhenFromDateAfterToDate_ReturnsBadRequest()
    {
        // Arrange
        var from = DateTime.Now;
        var to = DateTime.Now.AddDays(-30);

        // Act
        var result = await _controller.GetPurchasesReport(from, to, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given service fails, when getting purchases report, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetPurchasesReport_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;

        _reportServiceMock
            .Setup(x => x.GetPurchasesReportAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PurchasesReportDto>.Failure("فشل في جلب تقرير المشتريات"));

        // Act
        var result = await _controller.GetPurchasesReport(from, to, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Stock Report Tests

    /// <summary>
    /// Given warehouse exists, when getting stock report, then returns 200 OK
    /// </summary>
    [Fact]
    public async Task GetStockReport_WhenWarehouseExists_ReturnsOkWithReport()
    {
        // Arrange
        var report = new StockReportDto(1, "المستودع الرئيسي", 100, 5000m);

        _reportServiceMock
            .Setup(x => x.GetStockReportAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockReportDto>.Success(report));

        // Act
        var result = await _controller.GetStockReport(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given service fails, when getting stock report, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetStockReport_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _reportServiceMock
            .Setup(x => x.GetStockReportAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockReportDto>.Failure("فشل في جلب تقرير المخزون"));

        // Act
        var result = await _controller.GetStockReport(null, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Financial Report Tests

    /// <summary>
    /// Given valid date range, when getting financial report, then returns 200 OK
    /// </summary>
    [Fact]
    public async Task GetFinancialReport_WhenValidDateRange_ReturnsOkWithReport()
    {
        // Arrange
        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;
        var report = new FinancialReportDto(from, to, 10000m, 8000m, 2000m);

        _reportServiceMock
            .Setup(x => x.GetFinancialReportAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FinancialReportDto>.Success(report));

        // Act
        var result = await _controller.GetFinancialReport(from, to, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given service fails, when getting financial report, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetFinancialReport_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;

        _reportServiceMock
            .Setup(x => x.GetFinancialReportAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FinancialReportDto>.Failure("فشل في جلب التقرير المالي"));

        // Act
        var result = await _controller.GetFinancialReport(from, to, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion
}