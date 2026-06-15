using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers.Customers;

public class CustomersControllerTests : ControllerTestBase
{
    private readonly CustomersController _controller;
    private readonly Mock<IValidator<CreateCustomerRequest>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateCustomerRequest>> _updateValidatorMock;

    public CustomersControllerTests()
    {
        _createValidatorMock = new Mock<IValidator<CreateCustomerRequest>>();
        _updateValidatorMock = new Mock<IValidator<UpdateCustomerRequest>>();

        _createValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<CreateCustomerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _updateValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<UpdateCustomerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _controller = new CustomersController(CustomerServiceMock.Object);

        // Setup user claims for authorized requests
        SetupUserId(_controller, 1);
    }

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        var customers = new PagedResult<CustomerDto>
        {
            Items = new List<CustomerDto> { CreateCustomerDto(1), CreateCustomerDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        CustomerServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(customers));

        var result = await _controller.GetAll(null, 1, 10, false, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        CustomerServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<CustomerDto>>("فشل في استرجاع العملاء"));

        var result = await _controller.GetAll(null, 1, 10, false, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenCustomerExists_ReturnsOkWithCustomer()
    {
        var customer = CreateCustomerDto(1);
        CustomerServiceMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(customer));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenCustomerNotFound_ReturnsNotFound()
    {
        CustomerServiceMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<CustomerDto>("العميل غير موجود"));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreateCustomerRequest("عميل جديد", null, null, null, null);
        var createdCustomer = CreateCustomerDto(1);
        CustomerServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdCustomer));

        var result = await _controller.Create(request, _createValidatorMock.Object, CancellationToken.None);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new CreateCustomerRequest("عميل جديد", null, null, null, null);
        CustomerServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<CustomerDto>("اسم العميل موجود مسبقاً"));

        var result = await _controller.Create(request, _createValidatorMock.Object, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedCustomer()
    {
        var request = new UpdateCustomerRequest("عميل محدث", null, null, null, null, 0.00m, true);
        var updatedCustomer = CreateCustomerDto(1);
        CustomerServiceMock.Setup(x => x.UpdateAsync(1, request, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(updatedCustomer));

        var result = await _controller.Update(1, request, _updateValidatorMock.Object, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenCustomerNotFound_ReturnsBadRequest()
    {
        var request = new UpdateCustomerRequest("عميل محدث", null, null, null, null, 0.00m, true);
        CustomerServiceMock.Setup(x => x.UpdateAsync(999, request, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<CustomerDto>("العميل غير موجود"));

        var result = await _controller.Update(999, request, _updateValidatorMock.Object, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenCustomerExists_ReturnsOkWithSuccessMessage()
    {
        CustomerServiceMock.Setup(x => x.DeleteAsync(1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        var result = await _controller.Delete(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenCustomerNotFound_ReturnsBadRequest()
    {
        CustomerServiceMock.Setup(x => x.DeleteAsync(999, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("العميل غير موجود"));

        var result = await _controller.Delete(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PermanentDelete_WhenCustomerExists_ReturnsOkWithSuccessMessage()
    {
        CustomerServiceMock.Setup(x => x.PermanentDeleteAsync(1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        var result = await _controller.PermanentDelete(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PermanentDelete_WhenCustomerNotFound_ReturnsBadRequest()
    {
        CustomerServiceMock.Setup(x => x.PermanentDeleteAsync(999, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("العميل غير موجود"));

        var result = await _controller.PermanentDelete(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static CustomerDto CreateCustomerDto(int id) => new(
        Id: id,
        Name: $"عميل {id}",
        Phone: null,
        Email: null,
        Address: null,
        TaxNumber: null,
        CreditLimit: 1000.00m,
        IsActive: true,
        AccountId: 1,
        AccountName: null);
}
