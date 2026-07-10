using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
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

        // Setup user claims for authorized requests
        SetupUserId(_controller, 1);
    }

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        var suppliers = new PagedResult<SupplierDto>
        {
            Items = new List<SupplierDto> { CreateSupplierDto(1), CreateSupplierDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        SupplierServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(suppliers));

        var result = await _controller.GetAll(null, 1, 10, false, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        SupplierServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<SupplierDto>>("فشل في استرجاع الموردين"));

        var result = await _controller.GetAll(null, 1, 10, false, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenSupplierExists_ReturnsOkWithSupplier()
    {
        var supplier = CreateSupplierDto(1);
        SupplierServiceMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(supplier));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenSupplierNotFound_ReturnsNotFound()
    {
        SupplierServiceMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SupplierDto>("المورد غير موجود"));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreateSupplierRequest("مورد جديد", null, null, null, null);
        var createdSupplier = CreateSupplierDto(1);
        SupplierServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdSupplier));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(SuppliersController.GetById));
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new CreateSupplierRequest("مورد جديد", null, null, null, null);
        SupplierServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SupplierDto>("اسم المورد موجود مسبقاً"));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedSupplier()
    {
        var request = new UpdateSupplierRequest("مورد محدث", null, null, null, null, null, 0m, null, true);
        var updatedSupplier = CreateSupplierDto(1);
        SupplierServiceMock.Setup(x => x.UpdateAsync(1, request, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(updatedSupplier));

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenSupplierNotFound_ReturnsBadRequest()
    {
        var request = new UpdateSupplierRequest("مورد محدث", null, null, null, null, null, 0m, null, true);
        SupplierServiceMock.Setup(x => x.UpdateAsync(999, request, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SupplierDto>("المورد غير موجود"));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenSupplierExists_ReturnsOkWithSuccessMessage()
    {
        SupplierServiceMock.Setup(x => x.DeleteAsync(1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        var result = await _controller.Delete(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenSupplierNotFound_ReturnsBadRequest()
    {
        SupplierServiceMock.Setup(x => x.DeleteAsync(999, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("المورد غير موجود"));

        var result = await _controller.Delete(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PermanentDelete_WhenSupplierExists_ReturnsOkWithSuccessMessage()
    {
        SupplierServiceMock.Setup(x => x.PermanentDeleteAsync(1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        var result = await _controller.PermanentDelete(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PermanentDelete_WhenSupplierNotFound_ReturnsBadRequest()
    {
        SupplierServiceMock.Setup(x => x.PermanentDeleteAsync(999, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("المورد غير موجود"));

        var result = await _controller.PermanentDelete(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static SupplierDto CreateSupplierDto(int id) => new(
        Id: id,
        Name: $"مورد {id}",
        Phone: null,
        Email: null,
        Address: null,
        TaxNumber: null,
        Notes: null,
        CreditLimit: 0m,
        IsActive: true,
        AccountId: 1,
        AccountName: null);
}


