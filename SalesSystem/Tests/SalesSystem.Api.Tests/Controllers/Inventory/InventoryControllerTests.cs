using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Api.Controllers;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Tests.Controllers.Inventory;

public class InventoryControllerTests : ControllerTestBase
{
    private readonly InventoryController _controller;

    public InventoryControllerTests()
    {
        _controller = new InventoryController(InventoryServiceMock.Object);
    }

    #region GetStock Tests

    [Fact]
    public async Task GetStock_WhenCalled_ReturnsOkWithQuantity()
    {
        // Arrange
        InventoryServiceMock.Setup(x => x.GetStockAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(100m));

        // Act
        var result = await _controller.GetStock(1, 1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetStock_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        InventoryServiceMock.Setup(x => x.GetStockAsync(999, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<decimal>("المنتج غير موجود"));

        // Act
        var result = await _controller.GetStock(999, 1);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetMovements Tests

    [Fact]
    public async Task GetMovements_WhenCalled_ReturnsOkWithPagedResult()
    {
        // Arrange
        var movements = new PagedResult<InventoryMovementDto>
        {
            Items = new List<InventoryMovementDto> { CreateMovementDto(1), CreateMovementDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        InventoryServiceMock.Setup(x => x.GetMovementsAsync(null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(movements));

        // Act
        var result = await _controller.GetMovements(null, null, null, 1, 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMovements_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        InventoryServiceMock.Setup(x => x.GetMovementsAsync(null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<InventoryMovementDto>>("فشل في استرجاع حركات المخزون"));

        // Act
        var result = await _controller.GetMovements(null, null, null, 1, 10);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMovements_WithProductFilter_ReturnsFilteredResults()
    {
        // Arrange
        var movements = new PagedResult<InventoryMovementDto>
        {
            Items = new List<InventoryMovementDto> { CreateMovementDto(1) },
            Page = 1, PageSize = 10, TotalCount = 1
        };

        InventoryServiceMock.Setup(x => x.GetMovementsAsync(1, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(movements));

        // Act
        var result = await _controller.GetMovements(1, null, null, 1, 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetWarehouseStocks Tests

    [Fact]
    public async Task GetWarehouseStocks_WhenCalled_ReturnsOkWithPagedResult()
    {
        // Arrange
        var stocks = new PagedResult<WarehouseStockDto>
        {
            Items = new List<WarehouseStockDto> { CreateStockDto(1), CreateStockDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        InventoryServiceMock.Setup(x => x.GetWarehouseStocksAsync(null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(stocks));

        // Act
        var result = await _controller.GetWarehouseStocks(null, null, 1, 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetWarehouseStocks_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        InventoryServiceMock.Setup(x => x.GetWarehouseStocksAsync(null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<WarehouseStockDto>>("فشل في استرجاع مخزون المستودع"));

        // Act
        var result = await _controller.GetWarehouseStocks(null, null, 1, 10);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetWarehouseStocks_WithWarehouseFilter_ReturnsFilteredResults()
    {
        // Arrange
        var stocks = new PagedResult<WarehouseStockDto>
        {
            Items = new List<WarehouseStockDto> { CreateStockDto(1) },
            Page = 1, PageSize = 10, TotalCount = 1
        };

        InventoryServiceMock.Setup(x => x.GetWarehouseStocksAsync(1, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(stocks));

        // Act
        var result = await _controller.GetWarehouseStocks(1, null, 1, 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static InventoryMovementDto CreateMovementDto(int id) => new(
        Id: id,
        ProductId: 1,
        ProductName: $"منتج {id}",
        WarehouseId: 1,
        WarehouseName: "المستودع الرئيسي",
        MovementType: 1,
        QuantityChange: 50.00m,
        QuantityBefore: 100.00m,
        QuantityAfter: 150.00m,
        ReferenceType: "PurchaseInvoice",
        ReferenceId: id,
        MovementDate: DateTime.UtcNow,
        Notes: null);

    private static WarehouseStockDto CreateStockDto(int id) => new(
        WarehouseId: 1,
        WarehouseName: "المستودع الرئيسي",
        ProductId: id,
        ProductName: $"منتج {id}",
        UnitName: "قطعة",
        Quantity: 100.00m,
        ReorderLevel: 10.00m);

    #endregion
}