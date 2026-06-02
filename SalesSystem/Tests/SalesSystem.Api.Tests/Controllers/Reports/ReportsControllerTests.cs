using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
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
        var report = new SalesReportDto(from, 1, "العميل", 10000m, 0m, 0m, 10000m, 5000m, 5000m);

        _reportServiceMock
            .Setup(x => x.GetSalesReportAsync(It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<SalesReportDto>>.Success(new List<SalesReportDto> { report }));

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
            .ReturnsAsync(Result<IEnumerable<SalesReportDto>>.Failure("فشل في جلب تقرير المبيعات"));

        var result = await _controller.GetSalesReport(null, from, to, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetPurchasesReport_WhenValidDateRange_ReturnsOkWithReport()
    {
        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;
        var report = new PurchaseReportDto(from, 1, "المورد", 8000m, 0m, 0m, 8000m, 4000m, 4000m);

        _reportServiceMock
            .Setup(x => x.GetPurchasesReportAsync(It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<PurchaseReportDto>>.Success(new List<PurchaseReportDto> { report }));

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
            .ReturnsAsync(Result<IEnumerable<PurchaseReportDto>>.Failure("فشل في جلب تقرير المشتريات"));

        var result = await _controller.GetPurchasesReport(null, from, to, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetStockReport_WhenWarehouseExists_ReturnsOkWithReport()
    {
        var report = new StockReportDto(1, "منتج", "تصنيف", "قطعة", "المستودع الرئيسي", 100m, 10m, 50m, 5000m);

        _reportServiceMock
            .Setup(x => x.GetStockReportAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<StockReportDto>>.Success(new List<StockReportDto> { report }));

        var result = await _controller.GetStockReport(1, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetStockReport_WhenServiceFails_ReturnsBadRequest()
    {
        _reportServiceMock
            .Setup(x => x.GetStockReportAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<StockReportDto>>.Failure("فشل في جلب تقرير المخزون"));

        var result = await _controller.GetStockReport(null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

}
