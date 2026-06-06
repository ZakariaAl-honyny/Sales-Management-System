using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Enums;
using System.Security.Claims;

namespace SalesSystem.Api.Tests.Controllers.Payments;

public class CustomerPaymentsControllerTests
{
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly CustomerPaymentsController _controller;

    public CustomerPaymentsControllerTests()
    {
        _paymentServiceMock = new Mock<IPaymentService>();
        _controller = new CustomerPaymentsController(_paymentServiceMock.Object);

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetAll_WhenPaymentsExist_ReturnsOkWithPagedResult()
    {
        var payments = new List<CustomerPaymentDto>
        {
            new(1, "CP-2026-000001", 1, "عميل 1", 100.00m, (byte)PaymentType.Cash, null, null, DateTime.Now, null, "دفعة أولى"),
            new(2, "CP-2026-000002", 1, "عميل 1", 50.00m, (byte)PaymentType.Cash, null, null, DateTime.Now, null, "دفعة ثانية")
        };
        var pagedResult = PagedResult<CustomerPaymentDto>.Create(payments, 2, 1, 10);

        _paymentServiceMock
            .Setup(x => x.GetCustomerPaymentsAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<CustomerPaymentDto>>.Success(pagedResult));

        var result = await _controller.GetAll(null, null, null, 1, 10, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        _paymentServiceMock
            .Setup(x => x.GetCustomerPaymentsAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<CustomerPaymentDto>>.Failure("فشل في جلب المدفوعات"));

        var result = await _controller.GetAll(null, null, null, 1, 10, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreateCustomerPaymentRequest(1, 100.00m, PaymentType.Cash, DateTime.Now, null, "دفعه أولى");
        var payment = new CustomerPaymentDto(1, "CP-2026-000001", request.CustomerId, "عميل 1", request.Amount, (byte)request.PaymentMethod, null, null, DateTime.Now, null, "دفعة أولى");

        _paymentServiceMock
            .Setup(x => x.CreateCustomerPaymentAsync(It.IsAny<CreateCustomerPaymentRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerPaymentDto>.Success(payment));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new CreateCustomerPaymentRequest(1, 100.00m, PaymentType.Cash, DateTime.Now, null, "دفعه أولى");

        _paymentServiceMock
            .Setup(x => x.CreateCustomerPaymentAsync(It.IsAny<CreateCustomerPaymentRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerPaymentDto>.Failure("فشل في إنشاء الدفعة"));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WithoutUserId_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new CreateCustomerPaymentRequest(1, 100.00m, PaymentType.Cash, DateTime.Now, null, "دفعه أولى");

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithPayment()
    {
        var paymentId = 1;
        var payment = new CustomerPaymentDto(paymentId, "CP-2026-000001", 1, "عميل 1", 500.00m, (byte)PaymentType.Cash, null, null, DateTime.Now, null, "دفعة أولى");

        _paymentServiceMock
            .Setup(x => x.GetCustomerPaymentByIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerPaymentDto>.Success(payment));

        var result = await _controller.GetById(paymentId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _paymentServiceMock
            .Setup(x => x.GetCustomerPaymentByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerPaymentDto>.Failure("الدفعة غير موجودة", Contracts.Common.ErrorCodes.NotFound));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}

public class SupplierPaymentsControllerTests
{
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly SupplierPaymentsController _controller;

    public SupplierPaymentsControllerTests()
    {
        _paymentServiceMock = new Mock<IPaymentService>();
        _controller = new SupplierPaymentsController(_paymentServiceMock.Object);

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetAll_WhenPaymentsExist_ReturnsOkWithPagedResult()
    {
        var payments = new List<SupplierPaymentDto>
        {
            new(1, "SP-2026-000001", 1, "مورد 1", 200.00m, (byte)PaymentType.Cash, null, null, DateTime.Now, null, "دفعة أولى"),
            new(2, "SP-2026-000002", 1, "مورد 1", 150.00m, (byte)PaymentType.Cash, null, null, DateTime.Now, null, "دفعة ثانية")
        };
        var pagedResult = PagedResult<SupplierPaymentDto>.Create(payments, 2, 1, 10);

        _paymentServiceMock
            .Setup(x => x.GetSupplierPaymentsAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<SupplierPaymentDto>>.Success(pagedResult));

        var result = await _controller.GetAll(null, null, null, 1, 10, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        _paymentServiceMock
            .Setup(x => x.GetSupplierPaymentsAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<SupplierPaymentDto>>.Failure("فشل في جلب المدفوعات"));

        var result = await _controller.GetAll(null, null, null, 1, 10, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreateSupplierPaymentRequest(1, 200.00m, PaymentType.Cash, DateTime.Now, null, "دفعه أولى");
        var payment = new SupplierPaymentDto(1, "SP-2026-000001", request.SupplierId, "مورد 1", request.Amount, (byte)request.PaymentMethod, null, null, DateTime.Now, null, "دفعة أولى");

        _paymentServiceMock
            .Setup(x => x.CreateSupplierPaymentAsync(It.IsAny<CreateSupplierPaymentRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SupplierPaymentDto>.Success(payment));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new CreateSupplierPaymentRequest(1, 200.00m, PaymentType.Cash, DateTime.Now, null, "دفعه أولى");

        _paymentServiceMock
            .Setup(x => x.CreateSupplierPaymentAsync(It.IsAny<CreateSupplierPaymentRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SupplierPaymentDto>.Failure("فشل في إنشاء الدفعة"));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WithoutUserId_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new CreateSupplierPaymentRequest(1, 200.00m, PaymentType.Cash, DateTime.Now, null, "دفعه أولى");

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithPayment()
    {
        var paymentId = 1;
        var payment = new SupplierPaymentDto(paymentId, "SP-2026-000001", 1, "مورد 1", 500.00m, (byte)PaymentType.Cash, null, null, DateTime.Now, null, "دفعة أولى");

        _paymentServiceMock
            .Setup(x => x.GetSupplierPaymentByIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SupplierPaymentDto>.Success(payment));

        var result = await _controller.GetById(paymentId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _paymentServiceMock
            .Setup(x => x.GetSupplierPaymentByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SupplierPaymentDto>.Failure("الدفعة غير موجودة", Contracts.Common.ErrorCodes.NotFound));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
