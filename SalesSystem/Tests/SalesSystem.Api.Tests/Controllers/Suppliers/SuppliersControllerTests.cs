using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Api.Controllers;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers.Suppliers;

public class SuppliersControllerTests : ControllerTestBase
{
    private readonly SuppliersController _controller;

    public SuppliersControllerTests()
    {
        _controller = new SuppliersController(SupplierServiceMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        // Arrange
        var suppliers = new PagedResult<SupplierDto>
        {
            Items = new List<SupplierDto> { CreateSupplierDto(1), CreateSupplierDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        SupplierServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(suppliers));

        // Act
        var result = await _controller.GetAll(null, 1, 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        SupplierServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<SupplierDto>>("فشل في استرجاع الموردين"));

        // Act
        var result = await _controller.GetAll(null, 1, 10);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenSupplierExists_ReturnsOkWithSupplier()
    {
        // Arrange
        var supplier = CreateSupplierDto(1);
        SupplierServiceMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(supplier));

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenSupplierNotFound_ReturnsNotFound()
    {
        // Arrange
        SupplierServiceMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SupplierDto>("المورد غير موجود"));

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
        var request = new CreateSupplierRequest("مورد جديد", "S001", 0.00m, null, null, null);
        var createdSupplier = CreateSupplierDto(1);
        SupplierServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdSupplier));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(SuppliersController.GetById));
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSupplierRequest("مورد جديد", "S001", 0.00m, null, null, null);
        SupplierServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SupplierDto>("اسم المورد موجود مسبقاً"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedSupplier()
    {
        // Arrange
        var request = new UpdateSupplierRequest("مورد محدث", "S001", null, null, null, 0.00m, true);
        var updatedSupplier = CreateSupplierDto(1);
        SupplierServiceMock.Setup(x => x.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(updatedSupplier));

        // Act
        var result = await _controller.Update(1, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenSupplierNotFound_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateSupplierRequest("مورد محدث", "S001", null, null, null, 0.00m, true);
        SupplierServiceMock.Setup(x => x.UpdateAsync(999, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SupplierDto>("المورد غير موجود"));

        // Act
        var result = await _controller.Update(999, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WhenSupplierExists_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        SupplierServiceMock.Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenSupplierNotFound_ReturnsBadRequest()
    {
        // Arrange
        SupplierServiceMock.Setup(x => x.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("المورد غير موجود"));

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static SupplierDto CreateSupplierDto(int id) => new(
        Id: id,
        Code: $"S{id:D3}",
        Name: $"مورد {id}",
        Phone: null,
        Email: null,
        Address: null,
        OpeningBalance: 0.00m,
        CurrentBalance: 0.00m,
        IsActive: true);

    #endregion
}