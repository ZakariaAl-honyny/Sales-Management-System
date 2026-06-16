using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
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

    [Fact]
    public async Task GetStock_WhenCalled_ReturnsOkWithQuantity()
    {
        InventoryServiceMock.Setup(x => x.GetStockAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<decimal>.Success(100m));

        var result = await _controller.GetStock(1, 1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetStock_WhenServiceFails_ReturnsBadRequest()
    {
        InventoryServiceMock.Setup(x => x.GetStockAsync(999, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<decimal>.Failure("المنتج غير موجود"));

        var result = await _controller.GetStock(999, 1, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMovements_WhenCalled_ReturnsOkWithPagedResult()
    {
        var movements = new PagedResult<InventoryTransactionDto>
        {
            Items = new List<InventoryTransactionDto> { CreateMovementDto(1), CreateMovementDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        InventoryServiceMock.Setup(x => x.GetMovementsAsync(null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(movements));

        var result = await _controller.GetMovements(null, null, null, 1, 10, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMovements_WhenServiceFails_ReturnsBadRequest()
    {
        InventoryServiceMock.Setup(x => x.GetMovementsAsync(null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<InventoryTransactionDto>>("فشل في استرجاع حركات المخزون"));

        var result = await _controller.GetMovements(null, null, null, 1, 10, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMovements_WithProductFilter_ReturnsFilteredResults()
    {
        var movements = new PagedResult<InventoryTransactionDto>
        {
            Items = new List<InventoryTransactionDto> { CreateMovementDto(1) },
            Page = 1, PageSize = 10, TotalCount = 1
        };

        InventoryServiceMock.Setup(x => x.GetMovementsAsync(1, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(movements));

        var result = await _controller.GetMovements(1, null, null, 1, 10, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetWarehouseStocks_WhenCalled_ReturnsOkWithPagedResult()
    {
        var stocks = new PagedResult<WarehouseStockDto>
        {
            Items = new List<WarehouseStockDto> { CreateStockDto(1), CreateStockDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        InventoryServiceMock.Setup(x => x.GetWarehouseStocksAsync(null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(stocks));

        var result = await _controller.GetWarehouseStocks(null, null, 1, 10, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetWarehouseStocks_WhenServiceFails_ReturnsBadRequest()
    {
        InventoryServiceMock.Setup(x => x.GetWarehouseStocksAsync(null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<WarehouseStockDto>>("فشل في استرجاع مخزون المستودع"));

        var result = await _controller.GetWarehouseStocks(null, null, 1, 10, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetWarehouseStocks_WithWarehouseFilter_ReturnsFilteredResults()
    {
        var stocks = new PagedResult<WarehouseStockDto>
        {
            Items = new List<WarehouseStockDto> { CreateStockDto(1) },
            Page = 1, PageSize = 10, TotalCount = 1
        };

        InventoryServiceMock.Setup(x => x.GetWarehouseStocksAsync(1, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(stocks));

        var result = await _controller.GetWarehouseStocks(1, null, 1, 10, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    private static InventoryTransactionDto CreateMovementDto(int id) => new(
        Id: id,
        TransactionNo: id.ToString(),
        MovementType: (byte)1,
        WarehouseId: (short)1,
        WarehouseName: "المستودع الرئيسي",
        ReferenceId: id,
        ReferenceType: (byte?)1,
        Notes: null,
        CreatedAt: DateTime.UtcNow,
        CreatedByUserId: 1,
        Lines: new List<InventoryTransactionLineDto>
        {
            new(Id: id, InventoryTransactionId: 1, ProductUnitId: 1, ProductUnitName: "قطعة", Quantity: 50.00m, UnitCost: 100.00m, BatchNo: null, ExpiryDate: null, WarehouseId: null)
        });

    private static WarehouseStockDto CreateStockDto(int id) => new(
        WarehouseId: 1,
        WarehouseName: "المستودع الرئيسي",
        ProductId: id,
        ProductName: $"منتج {id}",
        UnitName: "قطعة",
        Quantity: 100.00m,
        AvgCost: 0);
}
