using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers.Warehouses;

public class WarehousesControllerTests
{
    private readonly Mock<IWarehouseService> _warehouseServiceMock;
    private readonly WarehousesController _controller;

    public WarehousesControllerTests()
    {
        _warehouseServiceMock = new Mock<IWarehouseService>();
        _controller = new WarehousesController(_warehouseServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_WhenWarehousesExist_ReturnsOkWithPagedResult()
    {
        var warehouses = new List<WarehouseDto>
        {
            new(Id: 1, Code: "WH-001", Name: "المستودع الرئيسي", Type: 1, Location: "الرياض", Phone: null, Address: null, ManagerName: null, IsActive: true),
            new(Id: 2, Code: "WH-002", Name: "المستودع الفرعي", Type: 1, Location: "جدة", Phone: null, Address: null, ManagerName: null, IsActive: true)
        };
        var pagedResult = PagedResult<WarehouseDto>.Create(warehouses, 2, 1, 10);

        _warehouseServiceMock
            .Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<WarehouseDto>>.Success(pagedResult));

        var result = await _controller.GetAll(null, 1, 10, false, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        _warehouseServiceMock
            .Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<WarehouseDto>>.Failure("فشل في جلب المستودعات"));

        var result = await _controller.GetAll(null, 1, 10, false, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenWarehouseExists_ReturnsOkWithWarehouse()
    {
        var warehouse = new WarehouseDto(1, "WH-001", "المستودع الرئيسي", 1, "الرياض", null, null, null, true);

        _warehouseServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseDto>.Success(warehouse));

        var result = await _controller.GetById(1, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetById_WhenWarehouseNotFound_ReturnsNotFound()
    {
        _warehouseServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseDto>.Failure("المستودع غير موجود", ErrorCodes.NotFound));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreateWarehouseRequest(BranchId: 1, Code: "WH-001", Name: "المستودع الرئيسي", Location: "الرياض");
        var warehouse = new WarehouseDto(1, "WH-001", request.Name, 1, request.Location, null, null, null, true);

        _warehouseServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateWarehouseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseDto>.Success(warehouse));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new CreateWarehouseRequest(BranchId: 1, Code: "WH-001", Name: "المستودع الرئيسي", Location: "الرياض");

        _warehouseServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateWarehouseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseDto>.Failure("فشل في إنشاء المستودع"));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedWarehouse()
    {
        var request = new UpdateWarehouseRequest(BranchId: 1, Code: "WH-001", Name: "المستودع المحدث", Location: "الرياض", IsActive: true);
        var warehouse = new WarehouseDto(1, "WH-001", request.Name, 1, request.Location, null, null, null, true);

        _warehouseServiceMock
            .Setup(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateWarehouseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseDto>.Success(warehouse));

        var result = await _controller.Update(1, request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Update_WhenWarehouseNotFound_ReturnsBadRequest()
    {
        var request = new UpdateWarehouseRequest(BranchId: 1, Code: "WH-001", Name: "المستودع", Location: "الرياض", IsActive: true);

        _warehouseServiceMock
            .Setup(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateWarehouseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseDto>.Failure("المستودع غير موجود"));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenWarehouseExists_ReturnsOkWithSuccessMessage()
    {
        _warehouseServiceMock
            .Setup(x => x.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Delete(1, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Delete_WhenWarehouseNotFound_ReturnsBadRequest()
    {
        _warehouseServiceMock
            .Setup(x => x.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("المستودع غير موجود"));

        var result = await _controller.Delete(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PermanentDelete_WhenWarehouseExists_ReturnsOkWithSuccessMessage()
    {
        _warehouseServiceMock
            .Setup(x => x.PermanentDeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.PermanentDelete(1, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task PermanentDelete_WhenWarehouseNotFound_ReturnsBadRequest()
    {
        _warehouseServiceMock
            .Setup(x => x.PermanentDeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("المستودع غير موجود"));

        var result = await _controller.PermanentDelete(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
