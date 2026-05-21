using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
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

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        // Arrange
        var units = new PagedResult<UnitDto>
        {
            Items = new List<UnitDto> { CreateUnitDto(1), CreateUnitDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        UnitServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(units));

        // Act
        var result = await _controller.GetAll(null, 1, 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        UnitServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<UnitDto>>("فشل في استرجاع الوحدات"));

        // Act
        var result = await _controller.GetAll(null, 1, 10);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenUnitExists_ReturnsOkWithUnit()
    {
        // Arrange
        var unit = CreateUnitDto(1);
        UnitServiceMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(unit));

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenUnitNotFound_ReturnsNotFound()
    {
        // Arrange
        UnitServiceMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<UnitDto>("الوحدة غير موجودة"));

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateUnitRequest("قطعة", "pc");
        var createdUnit = CreateUnitDto(1);
        UnitServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdUnit));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateUnitRequest("قطعة", "pc");
        UnitServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<UnitDto>("اسم الوحدة موجود مسبقاً"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedUnit()
    {
        // Arrange
        var request = new UpdateUnitRequest("قطعة محدثة", "pcs", true);
        var updatedUnit = CreateUnitDto(1);
        UnitServiceMock.Setup(x => x.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(updatedUnit));

        // Act
        var result = await _controller.Update(1, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenUnitNotFound_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateUnitRequest("قطعة", "pc", true);
        UnitServiceMock.Setup(x => x.UpdateAsync(999, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<UnitDto>("الوحدة غير موجودة"));

        // Act
        var result = await _controller.Update(999, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WhenUnitExists_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        UnitServiceMock.Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenUnitNotFound_ReturnsBadRequest()
    {
        // Arrange
        UnitServiceMock.Setup(x => x.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("الوحدة غير موجودة"));

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static UnitDto CreateUnitDto(int id) => new(
        Id: id,
        Name: $"وحدة {id}",
        Symbol: $"u{id}",
        IsActive: true);

    #endregion
}