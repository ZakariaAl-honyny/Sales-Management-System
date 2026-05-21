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

/// <summary>
/// Unit tests for PaymentsController HTTP status codes
/// </summary>
public class PaymentsControllerTests
{
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        _paymentServiceMock = new Mock<IPaymentService>();
        _controller = new PaymentsController(_paymentServiceMock.Object);
        
        // Setup controller context with user claims
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };
    }

    #region Customer Payments - GetAll Tests

    /// <summary>
    /// Given payments exist, when getting customer payments, then returns 200 OK
    /// </summary>
    [Fact]
    public async Task GetCustomerPayments_WhenPaymentsExist_ReturnsOkWithPagedResult()
    {
        // Arrange
        var payments = new List<CustomerPaymentDto>
        {
            new(1, "CP-2026-000001", 1, "عميل 1", 100.00m, (byte)PaymentType.Cash, DateTime.Now, null, "دفعة أولى"),
            new(2, "CP-2026-000002", 1, "عميل 1", 50.00m, (byte)PaymentType.Cash, DateTime.Now, null, "دفعة ثانية")
        };
        var pagedResult = new PagedResult<CustomerPaymentDto>(payments, 2, 1, 10);

        _paymentServiceMock
            .Setup(x => x.GetCustomerPaymentsAsync(It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<CustomerPaymentDto>>.Success(pagedResult));

        // Act
        var result = await _controller.GetCustomerPayments(null, 1, 10, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given service fails, when getting customer payments, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetCustomerPayments_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _paymentServiceMock
            .Setup(x => x.GetCustomerPaymentsAsync(It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<CustomerPaymentDto>>.Failure("فشل في جلب المدفوعات"));

        // Act
        var result = await _controller.GetCustomerPayments(null, 1, 10, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Customer Payments - Create Tests

    /// <summary>
    /// Given valid request, when creating customer payment, then returns 201 Created
    /// </summary>
    [Fact]
    public async Task CreateCustomerPayment_WhenValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateCustomerPaymentRequest(1, 100.00m, PaymentType.Cash, DateTime.Now, null, "دفعه أولى");
        var payment = new CustomerPaymentDto(1, "CP-2026-000001", request.CustomerId, "عميل 1", request.Amount, (byte)request.PaymentMethod, DateTime.Now, null, "دفعة أولى");

        _paymentServiceMock
            .Setup(x => x.CreateCustomerPaymentAsync(It.IsAny<CreateCustomerPaymentRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerPaymentDto>.Success(payment));

        // Act
        var result = await _controller.CreateCustomerPayment(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given service fails, when creating customer payment, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateCustomerPayment_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCustomerPaymentRequest(1, 100.00m, PaymentType.Cash, DateTime.Now, null, "دفعه أولى");

        _paymentServiceMock
            .Setup(x => x.CreateCustomerPaymentAsync(It.IsAny<CreateCustomerPaymentRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerPaymentDto>.Failure("فشل في إنشاء الدفعة"));

        // Act
        var result = await _controller.CreateCustomerPayment(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given no user id, when creating customer payment, then returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task CreateCustomerPayment_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new CreateCustomerPaymentRequest(1, 100.00m, PaymentType.Cash, DateTime.Now, null, "دفعه أولى");

        // Act
        var result = await _controller.CreateCustomerPayment(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region Supplier Payments - GetAll Tests

    /// <summary>
    /// Given payments exist, when getting supplier payments, then returns 200 OK
    /// </summary>
    [Fact]
    public async Task GetSupplierPayments_WhenPaymentsExist_ReturnsOkWithPagedResult()
    {
        // Arrange
        var payments = new List<SupplierPaymentDto>
        {
            new(1, "SP-2026-000001", 1, "مورد 1", 200.00m, (byte)PaymentType.Cash, DateTime.Now, null, "دفعة أولى"),
            new(2, "SP-2026-000002", 1, "مورد 1", 150.00m, (byte)PaymentType.Cash, DateTime.Now, null, "دفعة ثانية")
        };
        var pagedResult = new PagedResult<SupplierPaymentDto>(payments, 2, 1, 10);

        _paymentServiceMock
            .Setup(x => x.GetSupplierPaymentsAsync(It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<SupplierPaymentDto>>.Success(pagedResult));

        // Act
        var result = await _controller.GetSupplierPayments(null, 1, 10, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given service fails, when getting supplier payments, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetSupplierPayments_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _paymentServiceMock
            .Setup(x => x.GetSupplierPaymentsAsync(It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<SupplierPaymentDto>>.Failure("فشل في جلب المدفوعات"));

        // Act
        var result = await _controller.GetSupplierPayments(null, 1, 10, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Customer Payments - GetById Tests

    /// <summary>
    /// Given payment exists, when getting customer payment by ID, then returns 200 OK
    /// </summary>
    [Fact]
    public async Task GetCustomerPaymentById_WhenExists_ReturnsOkWithPayment()
    {
        // Arrange
        var paymentId = 1;
        var payment = new CustomerPaymentDto(paymentId, "CP-2026-000001", 1, "عميل 1", 500.00m, (byte)PaymentType.Cash, DateTime.Now, null, "دفعة أولى");

        _paymentServiceMock
            .Setup(x => x.GetCustomerPaymentByIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerPaymentDto>.Success(payment));

        // Act
        var result = await _controller.GetCustomerPaymentById(paymentId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Given payment not found, when getting customer payment by ID, then returns 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetCustomerPaymentById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _paymentServiceMock
            .Setup(x => x.GetCustomerPaymentByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerPaymentDto>.Failure("الدفعة غير موجودة", ErrorCodes.NotFound));

        // Act
        var result = await _controller.GetCustomerPaymentById(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Supplier Payments - Create Tests

    /// <summary>
    /// Given valid request, when creating supplier payment, then returns 201 Created
    /// </summary>
    [Fact]
    public async Task CreateSupplierPayment_WhenValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateSupplierPaymentRequest(1, 200.00m, PaymentType.Cash, DateTime.Now, null, "دفعه أولى");
        var payment = new SupplierPaymentDto(1, "SP-2026-000001", request.SupplierId, "مورد 1", request.Amount, (byte)request.PaymentMethod, DateTime.Now, null, "دفعة أولى");

        _paymentServiceMock
            .Setup(x => x.CreateSupplierPaymentAsync(It.IsAny<CreateSupplierPaymentRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SupplierPaymentDto>.Success(payment));

        // Act
        var result = await _controller.CreateSupplierPayment(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given service fails, when creating supplier payment, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateSupplierPayment_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSupplierPaymentRequest(1, 200.00m, PaymentType.Cash, DateTime.Now, null, "دفعه أولى");

        _paymentServiceMock
            .Setup(x => x.CreateSupplierPaymentAsync(It.IsAny<CreateSupplierPaymentRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SupplierPaymentDto>.Failure("فشل في إنشاء الدفعة"));

        // Act
        var result = await _controller.CreateSupplierPayment(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given no user id, when creating supplier payment, then returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task CreateSupplierPayment_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new CreateSupplierPaymentRequest(1, 200.00m, PaymentType.Cash, DateTime.Now, null, "دفعه أولى");

        // Act
        var result = await _controller.CreateSupplierPayment(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion
}