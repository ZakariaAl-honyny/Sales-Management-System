using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers.Units;

public class UnitsControllerTests : ControllerTestBase
{
    private readonly UnitsController _controller;

    public UnitsControllerTests()
    {
        _controller = new UnitsController(UnitServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        var units = new PagedResult<UnitDto>
        {
            Items = new List<UnitDto> { CreateUnitDto(1), CreateUnitDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        UnitServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(units));

        var result = await _controller.GetAll(null, 1, 10, false, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        UnitServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<UnitDto>>("فشل في استرجاع الوحدات"));

        var result = await _controller.GetAll(null, 1, 10, false, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenUnitExists_ReturnsOkWithUnit()
    {
        var unit = CreateUnitDto(1);
        UnitServiceMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(unit));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenUnitNotFound_ReturnsNotFound()
    {
        UnitServiceMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<UnitDto>("الوحدة غير موجودة"));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreateUnitRequest("قطعة", "pc");
        var createdUnit = CreateUnitDto(1);
        UnitServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdUnit));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new CreateUnitRequest("قطعة", "pc");
        UnitServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<UnitDto>("اسم الوحدة موجود مسبقاً"));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedUnit()
    {
        var request = new UpdateUnitRequest("قطعة محدثة", "pcs", true);
        var updatedUnit = CreateUnitDto(1);
        UnitServiceMock.Setup(x => x.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(updatedUnit));

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenUnitNotFound_ReturnsBadRequest()
    {
        var request = new UpdateUnitRequest("قطعة", "pc", true);
        UnitServiceMock.Setup(x => x.UpdateAsync(999, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<UnitDto>("الوحدة غير موجودة"));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenUnitExists_ReturnsOkWithSuccessMessage()
    {
        UnitServiceMock.Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        var result = await _controller.Delete(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenUnitNotFound_ReturnsBadRequest()
    {
        UnitServiceMock.Setup(x => x.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("الوحدة غير موجودة"));

        var result = await _controller.Delete(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PermanentDelete_WhenUnitExists_ReturnsOkWithSuccessMessage()
    {
        UnitServiceMock.Setup(x => x.PermanentDeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        var result = await _controller.PermanentDelete(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PermanentDelete_WhenUnitNotFound_ReturnsBadRequest()
    {
        UnitServiceMock.Setup(x => x.PermanentDeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("الوحدة غير موجودة"));

        var result = await _controller.PermanentDelete(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static UnitDto CreateUnitDto(int id) => new(
        Id: id,
        Name: $"وحدة {id}",
        Symbol: $"u{id}",
        IsActive: true);
}
