using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Api.Controllers;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers.Customers;

public class CustomersControllerTests : ControllerTestBase
{
    private readonly CustomersController _controller;

    public CustomersControllerTests()
    {
        _controller = new CustomersController(CustomerServiceMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        // Arrange
        var customers = new PagedResult<CustomerDto>
        {
            Items = new List<CustomerDto> { CreateCustomerDto(1), CreateCustomerDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        CustomerServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(customers));

        // Act
        var result = await _controller.GetAll(null, 1, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        CustomerServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<CustomerDto>>("فشل في استرجاع العملاء"));

        // Act
        var result = await _controller.GetAll(null, 1, 10);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenCustomerExists_ReturnsOkWithCustomer()
    {
        // Arrange
        var customer = CreateCustomerDto(1);
        CustomerServiceMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(customer));

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenCustomerNotFound_ReturnsNotFound()
    {
        // Arrange
        CustomerServiceMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<CustomerDto>("العميل غير موجود"));

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
        var request = new CreateCustomerRequest("عميل جديد", "C001", 0.00m, null, null, null, 0.00m);
        var createdCustomer = CreateCustomerDto(1);
        CustomerServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdCustomer));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCustomerRequest("عميل جديد", "C001", 0.00m, null, null, null, 0.00m);
        CustomerServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<CustomerDto>("اسم العميل موجود مسبقاً"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedCustomer()
    {
        // Arrange
        var request = new UpdateCustomerRequest("عميل محدث", "C001", null, null, null, 0.00m, 0.00m, true);
        var updatedCustomer = CreateCustomerDto(1);
        CustomerServiceMock.Setup(x => x.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(updatedCustomer));

        // Act
        var result = await _controller.Update(1, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenCustomerNotFound_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateCustomerRequest("عميل محدث", "C001", null, null, null, 0.00m, 0.00m, true);
        CustomerServiceMock.Setup(x => x.UpdateAsync(999, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<CustomerDto>("العميل غير موجود"));

        // Act
        var result = await _controller.Update(999, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WhenCustomerExists_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        CustomerServiceMock.Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenCustomerNotFound_ReturnsBadRequest()
    {
        // Arrange
        CustomerServiceMock.Setup(x => x.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("العميل غير موجود"));

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static CustomerDto CreateCustomerDto(int id) => new(
        Id: id,
        Code: $"C{id:D3}",
        Name: $"عميل {id}",
        Phone: null,
        Email: null,
        Address: null,
        OpeningBalance: 0.00m,
        CurrentBalance: 0.00m,
        CreditLimit: 1000.00m,
        IsActive: true);

    #endregion
}