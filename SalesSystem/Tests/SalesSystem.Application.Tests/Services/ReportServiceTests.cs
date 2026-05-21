using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Common;
using Xunit.Abstractions;

namespace SalesSystem.Application.Tests.Services;

/// <summary>
/// Unit tests for ReportService business logic.
/// </summary>
public class ReportServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IReportRepository> _mockReportRepository;
    private readonly Mock<ILogger<ReportService>> _mockLogger;

    private readonly ReportService _sut;

    public ReportServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] ReportServiceTests initialized");

        _mockUow = new Mock<IUnitOfWork>();
        _mockReportRepository = new Mock<IReportRepository>();
        _mockLogger = new Mock<ILogger<ReportService>>();

        _sut = new ReportService(
            _mockUow.Object,
            _mockReportRepository.Object,
            _mockLogger.Object);
    }

    #region GetSalesReportAsync Tests

    [Fact]
    public async Task GetSalesReportAsync_ValidDateRange_ReturnsReport()
    {
        _output.WriteLine("[TEST] GetSalesReportAsync_ValidDateRange_ReturnsReport");

        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;
        var reportItems = new List<SalesSystem.Contracts.DTOs.SalesReportDto>
        {
            new(SalesSystem.Contracts.DTOs.SalesReportPeriod.Daily, DateTime.Now.AddDays(-1), 10m, 1000m, 500m),
            new(SalesSystem.Contracts.DTOs.SalesReportPeriod.Daily, DateTime.Now, 12m, 1200m, 600m)
        };

        _mockReportRepository.Setup(r => r.GetSalesReportAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportItems);

        var result = await _sut.GetSalesReportAsync(from, to, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        _output.WriteLine("[PASS] GetSalesReportAsync returns report data");
    }

    [Fact]
    public async Task GetSalesReportAsync_InvalidDateRange_ReturnsFailure()
    {
        _output.WriteLine("[TEST] GetSalesReportAsync_InvalidDateRange_ReturnsFailure");

        var from = DateTime.Now;
        var to = DateTime.Now.AddDays(-30); // to < from

        var result = await _sut.GetSalesReportAsync(from, to, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

        _output.WriteLine("[PASS] Invalid date range returns failure");
    }

    #endregion

    #region GetPurchasesReportAsync Tests

    [Fact]
    public async Task GetPurchasesReportAsync_ValidDateRange_ReturnsReport()
    {
        _output.WriteLine("[TEST] GetPurchasesReportAsync_ValidDateRange_ReturnsReport");

        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;
        var reportItems = new List<SalesSystem.Contracts.DTOs.PurchaseReportDto>
        {
            new(SalesSystem.Contracts.DTOs.PurchaseReportPeriod.Daily, DateTime.Now.AddDays(-1), 10m, 1000m),
            new(SalesSystem.Contracts.DTOs.PurchaseReportPeriod.Daily, DateTime.Now, 12m, 1200m)
        };

        _mockReportRepository.Setup(r => r.GetPurchasesReportAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportItems);

        var result = await _sut.GetPurchasesReportAsync(from, to, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        _output.WriteLine("[PASS] GetPurchasesReportAsync returns report data");
    }

    [Fact]
    public async Task GetPurchasesReportAsync_InvalidDateRange_ReturnsFailure()
    {
        _output.WriteLine("[TEST] GetPurchasesReportAsync_InvalidDateRange_ReturnsFailure");

        var from = DateTime.Now;
        var to = DateTime.Now.AddDays(-30);

        var result = await _sut.GetPurchasesReportAsync(from, to, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

        _output.WriteLine("[PASS] Invalid date range returns failure");
    }

    #endregion

    #region GetStockReportAsync Tests

    [Fact]
    public async Task GetStockReportAsync_WithWarehouseId_ReturnsFilteredReport()
    {
        _output.WriteLine("[TEST] GetStockReportAsync_WithWarehouseId_ReturnsFilteredReport");

        var reportItems = new List<SalesSystem.Contracts.DTOs.StockReportDto>
        {
            new(1, "Product 1", 100m, 50m, 10m),
            new(1, "Product 2", 80m, 40m, 8m)
        };

        _mockReportRepository.Setup(r => r.GetStockReportAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportItems);

        var result = await _sut.GetStockReportAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        _output.WriteLine("[PASS] GetStockReportAsync filters by warehouse");
    }

    [Fact]
    public async Task GetStockReportAsync_NullWarehouseId_ReturnsAllStock()
    {
        _output.WriteLine("[TEST] GetStockReportAsync_NullWarehouseId_ReturnsAllStock");

        var reportItems = new List<SalesSystem.Contracts.DTOs.StockReportDto>
        {
            new(1, "Product 1", 100m, 50m, 10m),
            new(2, "Product 2", 80m, 40m, 8m)
        };

        _mockReportRepository.Setup(r => r.GetStockReportAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportItems);

        var result = await _sut.GetStockReportAsync(null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        _output.WriteLine("[PASS] GetStockReportAsync returns all stock when no warehouse specified");
    }

    #endregion

    #region GetCustomerBalancesReportAsync Tests

    [Fact]
    public async Task GetCustomerBalancesReportAsync_ValidCustomerId_ReturnsReport()
    {
        _output.WriteLine("[TEST] GetCustomerBalancesReportAsync_ValidCustomerId_ReturnsReport");

        var reportItems = new List<SalesSystem.Contracts.DTOs.CustomerBalanceReportDto>
        {
            new(1, "Customer 1", 1000m, 500m, 500m)
        };

        _mockReportRepository.Setup(r => r.GetCustomerBalancesReportAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportItems);

        var result = await _sut.GetCustomerBalancesReportAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        _output.WriteLine("[PASS] GetCustomerBalancesReportAsync filters by customer");
    }

    [Fact]
    public async Task GetCustomerBalancesReportAsync_NullCustomerId_ReturnsAllBalances()
    {
        _output.WriteLine("[TEST] GetCustomerBalancesReportAsync_NullCustomerId_ReturnsAllBalances");

        var reportItems = new List<SalesSystem.Contracts.DTOs.CustomerBalanceReportDto>
        {
            new(1, "Customer 1", 1000m, 500m, 500m),
            new(2, "Customer 2", 2000m, 800m, 1200m)
        };

        _mockReportRepository.Setup(r => r.GetCustomerBalancesReportAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportItems);

        var result = await _sut.GetCustomerBalancesReportAsync(null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        _output.WriteLine("[PASS] GetCustomerBalancesReportAsync returns all balances");
    }

    #endregion

    #region GetSupplierBalancesReportAsync Tests

    [Fact]
    public async Task GetSupplierBalancesReportAsync_ValidSupplierId_ReturnsReport()
    {
        _output.WriteLine("[TEST] GetSupplierBalancesReportAsync_ValidSupplierId_ReturnsReport");

        var reportItems = new List<SalesSystem.Contracts.DTOs.SupplierBalanceReportDto>
        {
            new(1, "Supplier 1", 5000m, 2000m, 3000m)
        };

        _mockReportRepository.Setup(r => r.GetSupplierBalancesReportAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportItems);

        var result = await _sut.GetSupplierBalancesReportAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        _output.WriteLine("[PASS] GetSupplierBalancesReportAsync filters by supplier");
    }

    #endregion

    #region GetProductMovementsReportAsync Tests

    [Fact]
    public async Task GetProductMovementsReportAsync_ValidProductId_ReturnsReport()
    {
        _output.WriteLine("[TEST] GetProductMovementsReportAsync_ValidProductId_ReturnsReport");

        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;
        var reportItems = new List<SalesSystem.Contracts.DTOs.ProductMovementReportDto>
        {
            new(1, "Product 1", 100m, 50m, DateTime.Now.AddDays(-5), "Sale"),
            new(1, "Product 1", 200m, 100m, DateTime.Now.AddDays(-3), "Purchase")
        };

        _mockReportRepository.Setup(r => r.GetProductMovementsReportAsync(1, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportItems);

        var result = await _sut.GetProductMovementsReportAsync(1, from, to, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        _output.WriteLine("[PASS] GetProductMovementsReportAsync returns movement data");
    }

    [Fact]
    public async Task GetProductMovementsReportAsync_InvalidProductId_ReturnsFailure()
    {
        _output.WriteLine("[TEST] GetProductMovementsReportAsync_InvalidProductId_ReturnsFailure");

        var result = await _sut.GetProductMovementsReportAsync(0, null, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("معرف المنتج غير صالح");

        _output.WriteLine("[PASS] Invalid product ID returns failure");
    }

    [Fact]
    public async Task GetProductMovementsReportAsync_NegativeProductId_ReturnsFailure()
    {
        _output.WriteLine("[TEST] GetProductMovementsReportAsync_NegativeProductId_ReturnsFailure");

        var result = await _sut.GetProductMovementsReportAsync(-1, null, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("معرف المنتج غير صالح");

        _output.WriteLine("[PASS] Negative product ID returns failure");
    }

    #endregion

    #region GetLowStockReportAsync Tests

    [Fact]
    public async Task GetLowStockReportAsync_ReturnsLowStockItems()
    {
        _output.WriteLine("[TEST] GetLowStockReportAsync_ReturnsLowStockItems");

        var reportItems = new List<SalesSystem.Contracts.DTOs.LowStockReportDto>
        {
            new(1, "Product 1", 10m, 50m, 40m),
            new(2, "Product 2", 5m, 20m, 15m)
        };

        _mockReportRepository.Setup(r => r.GetLowStockReportAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportItems);

        var result = await _sut.GetLowStockReportAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        _output.WriteLine("[PASS] GetLowStockReportAsync returns low stock items");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetSalesReportAsync_RepositoryThrows_ReturnsFailure()
    {
        _output.WriteLine("[TEST] GetSalesReportAsync_RepositoryThrows_ReturnsFailure");

        var from = DateTime.Now.AddDays(-30);
        var to = DateTime.Now;

        _mockReportRepository.Setup(r => r.GetSalesReportAsync(from, to, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await _sut.GetSalesReportAsync(from, to, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("حدث خطأ أثناء إنشاء تقرير المبيعات");

        _output.WriteLine("[PASS] Repository exception returns failure with Arabic message");
    }

    #endregion
}