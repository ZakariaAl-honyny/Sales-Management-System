using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers.Warehouses;

/// <summary>
/// Unit tests for WarehousesController HTTP status codes
/// </summary>
public class WarehousesControllerTests
{
    private readonly Mock<IWarehouseService> _warehouseServiceMock;
    private readonly WarehousesController _controller;

    public WarehousesControllerTests()
    {
        _warehouseServiceMock = new Mock<IWarehouseService>();
        _controller = new WarehousesController(_warehouseServiceMock.Object);
    }

    #region GetAll Tests

    /// <summary>
    /// Given warehouses exist, when getting all warehouses, then returns 200 OK with paged result
    /// </summary>
    [Fact]
    public async Task GetAll_WhenWarehousesExist_ReturnsOkWithPagedResult()
    {
        // Arrange
        var warehouses = new List<WarehouseDto>
        {
            new(1, "المستودع الرئيسي", "WH001", "الرياض"),
            new(2, "المستودع الفرعي", "WH002", "جدة")
        };
        var pagedResult = new PagedResult<WarehouseDto>(warehouses, 2, 1, 10);

        _warehouseServiceMock
            .Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<WarehouseDto>>.Success(pagedResult));

        // Act
        var result = await _controller.GetAll(null, 1, 10, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given service fails, when getting all warehouses, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _warehouseServiceMock
            .Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<WarehouseDto>>.Failure("فشل في جلب المستودعات"));

        // Act
        var result = await _controller.GetAll(null, 1, 10, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetById Tests

    /// <summary>
    /// Given warehouse exists, when getting by id, then returns 200 OK with warehouse
    /// </summary>
    [Fact]
    public async Task GetById_WhenWarehouseExists_ReturnsOkWithWarehouse()
    {
        // Arrange
        var warehouse = new WarehouseDto(1, "المستودع الرئيسي", "WH001", "الرياض");

        _warehouseServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseDto>.Success(warehouse));

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given warehouse not found, when getting by id, then returns 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetById_WhenWarehouseNotFound_ReturnsNotFound()
    {
        // Arrange
        _warehouseServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseDto>.Failure("المستودع غير موجود", ErrorCodes.NotFound));

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    /// <summary>
    /// Given valid request, when creating warehouse, then returns 201 Created
    /// </summary>
    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateWarehouseRequest("المستودع الرئيسي", "WH001", "الرياض");
        var warehouse = new WarehouseDto(1, request.Name, request.Code, request.Location);

        _warehouseServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateWarehouseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseDto>.Success(warehouse));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    /// <summary>
    /// Given service fails, when creating warehouse, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateWarehouseRequest("المستودع الرئيسي", "WH001", "الرياض");

        _warehouseServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateWarehouseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseDto>.Failure("فشل في إنشاء المستودع"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    /// <summary>
    /// Given valid request, when updating warehouse, then returns 200 OK
    /// </summary>
    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedWarehouse()
    {
        // Arrange
        var request = new UpdateWarehouseRequest(1, "المستودع المحدث", "WH001", "الرياض");
        var warehouse = new WarehouseDto(1, request.Name, request.Code, request.Location);

        _warehouseServiceMock
            .Setup(x => x.UpdateAsync(It.IsAny<UpdateWarehouseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseDto>.Success(warehouse));

        // Act
        var result = await _controller.Update(1, request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given warehouse not found, when updating, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_WhenWarehouseNotFound_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateWarehouseRequest(999, "المستودع", "WH001", "الرياض");

        _warehouseServiceMock
            .Setup(x => x.UpdateAsync(It.IsAny<UpdateWarehouseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseDto>.Failure("المستودع غير موجود"));

        // Act
        var result = await _controller.Update(999, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Delete Tests

    /// <summary>
    /// Given warehouse exists, when deleting, then returns 200 OK
    /// </summary>
    [Fact]
    public async Task Delete_WhenWarehouseExists_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        _warehouseServiceMock
            .Setup(x => x.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given warehouse not found, when deleting, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Delete_WhenWarehouseNotFound_ReturnsBadRequest()
    {
        // Arrange
        _warehouseServiceMock
            .Setup(x => x.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("المستودع غير موجود"));

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion
}