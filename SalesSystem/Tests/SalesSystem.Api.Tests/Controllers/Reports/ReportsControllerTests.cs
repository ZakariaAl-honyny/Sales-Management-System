using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Tests.Controllers.Reports;

public class ReportsControllerTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _controller = new ReportsController(_reportServiceMock.Object);
    }

    [Fact]
    public async Task GetSalesReport_WhenValidDateRange_ReturnsOkWithReport()
    {
        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;
        var report = new SalesReportDto(from, to, 10000m, 50, 5000m, 5000m);

        _reportServiceMock
            .Setup(x => x.GetSalesReportAsync(It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SalesReportDto>.Success(report));

        var result = await _controller.GetSalesReport(null, from, to, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetSalesReport_WhenFromDateAfterToDate_ReturnsBadRequest()
    {
        var from = DateTime.Now;
        var to = DateTime.Now.AddDays(-30);

        var result = await _controller.GetSalesReport(null, from, to, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetSalesReport_WhenServiceFails_ReturnsBadRequest()
    {
        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;

        _reportServiceMock
            .Setup(x => x.GetSalesReportAsync(It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SalesReportDto>.Failure("فشل في جلب تقرير المبيعات"));

        var result = await _controller.GetSalesReport(null, from, to, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetPurchasesReport_WhenValidDateRange_ReturnsOkWithReport()
    {
        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;
        var report = new PurchasesReportDto(from, to, 8000m, 40, 4000m, 4000m);

        _reportServiceMock
            .Setup(x => x.GetPurchasesReportAsync(It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PurchasesReportDto>.Success(report));

        var result = await _controller.GetPurchasesReport(null, from, to, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetPurchasesReport_WhenFromDateAfterToDate_ReturnsBadRequest()
    {
        var from = DateTime.Now;
        var to = DateTime.Now.AddDays(-30);

        var result = await _controller.GetPurchasesReport(null, from, to, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetPurchasesReport_WhenServiceFails_ReturnsBadRequest()
    {
        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;

        _reportServiceMock
            .Setup(x => x.GetPurchasesReportAsync(It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PurchasesReportDto>.Failure("فشل في جلب تقرير المشتريات"));

        var result = await _controller.GetPurchasesReport(null, from, to, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetStockReport_WhenWarehouseExists_ReturnsOkWithReport()
    {
        var report = new StockReportDto(1, "المستودع الرئيسي", 100, 5000m);

        _reportServiceMock
            .Setup(x => x.GetStockReportAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockReportDto>.Success(report));

        var result = await _controller.GetStockReport(1, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetStockReport_WhenServiceFails_ReturnsBadRequest()
    {
        _reportServiceMock
            .Setup(x => x.GetStockReportAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockReportDto>.Failure("فشل في جلب تقرير المخزون"));

        var result = await _controller.GetStockReport(null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetFinancialReport_WhenValidDateRange_ReturnsOkWithReport()
    {
        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;
        var report = new FinancialReportDto(from, to, 10000m, 8000m, 2000m);

        _reportServiceMock
            .Setup(x => x.GetFinancialReportAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FinancialReportDto>.Success(report));

        var result = await _controller.GetFinancialReport(from, to, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetFinancialReport_WhenServiceFails_ReturnsBadRequest()
    {
        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;

        _reportServiceMock
            .Setup(x => x.GetFinancialReportAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FinancialReportDto>.Failure("فشل في جلب التقرير المالي"));

        var result = await _controller.GetFinancialReport(from, to, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
