using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using SalesSystem.Api.Controllers;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers.Sales;

[Trait("Category", "SalesInvoicesController")]
public class SalesInvoicesControllerTests : ControllerTestBase
{
    private readonly SalesInvoicesController _controller;

    public SalesInvoicesControllerTests()
    {
        _controller = new SalesInvoicesController(SalesServiceMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        // Arrange
        var expectedResult = new PagedResult<SalesInvoiceDto>
        {
            Items = new List<SalesInvoiceDto> { CreateInvoiceDto(1, 1), CreateInvoiceDto(2, 2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        SalesServiceMock.Setup(x => x.GetAllAsync(null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(expectedResult));

        // Act
        var result = await _controller.GetAll(null, null, 1, 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        SalesServiceMock.Setup(x => x.GetAllAsync(null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<SalesInvoiceDto>>("حدث خطأ"));

        // Act
        var result = await _controller.GetAll(null, null, 1, 10);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAll_WithCustomerFilter_PassesFilterToService()
    {
        // Arrange
        var customerId = 5;
        var pagedResult = new PagedResult<SalesInvoiceDto> { Items = new List<SalesInvoiceDto>(), Page = 1, PageSize = 10, TotalCount = 0 };
        SalesServiceMock.Setup(x => x.GetAllAsync(customerId, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(pagedResult));

        // Act
        await _controller.GetAll(customerId, null, 1, 10);

        // Assert
        SalesServiceMock.Verify(x => x.GetAllAsync(customerId, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenInvoiceExists_ReturnsOkWithInvoice()
    {
        // Arrange
        var invoiceId = 1;
        var invoiceDto = CreateInvoiceDto(invoiceId, 1);
        SalesServiceMock.Setup(x => x.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(invoiceDto));

        // Act
        var result = await _controller.GetById(invoiceId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<SalesInvoiceDto>();
    }

    [Fact]
    public async Task GetById_WhenInvoiceNotFound_ReturnsNotFound()
    {
        // Arrange
        var invoiceId = 999;
        SalesServiceMock.Setup(x => x.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SalesInvoiceDto>("الفاتورة غير موجودة"));

        // Act
        var result = await _controller.GetById(invoiceId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtActionWithInvoice()
    {
        // Arrange
        var createdInvoice = CreateInvoiceDto(1, 1);
        SetupUserId(_controller, 1);
        var request = CreateValidRequest();
        SalesServiceMock.Setup(x => x.CreateAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdInvoice));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(SalesInvoicesController.GetById));
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        SetupUserId(_controller, 1);
        var request = CreateValidRequest();
        SalesServiceMock.Setup(x => x.CreateAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SalesInvoiceDto>("بيانات غير صالحة"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUserWithoutId(_controller);
        var request = CreateValidRequest();

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region Post Tests

    [Fact]
    public async Task Post_WhenDraftInvoice_ReturnsOkWithPostedInvoice()
    {
        // Arrange
        var invoiceId = 1;
        var postedInvoice = CreateInvoiceDto(invoiceId, 2);
        SetupUserId(_controller, 1);
        SalesServiceMock.Setup(x => x.PostAsync(invoiceId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(postedInvoice));

        // Act
        var result = await _controller.Post(invoiceId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<SalesInvoiceDto>();
    }

    [Fact]
    public async Task Post_WhenInvoiceNotFound_ReturnsBadRequest()
    {
        // Arrange
        var invoiceId = 999;
        SetupUserId(_controller, 1);
        SalesServiceMock.Setup(x => x.PostAsync(invoiceId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SalesInvoiceDto>("الفاتورة غير موجودة"));

        // Act
        var result = await _controller.Post(invoiceId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Post_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUserWithoutId(_controller);

        // Act
        var result = await _controller.Post(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public async Task Cancel_WhenPostedInvoice_ReturnsOkWithCancelledInvoice()
    {
        // Arrange
        var invoiceId = 1;
        var cancelledInvoice = CreateInvoiceDto(invoiceId, 3);
        SetupUserId(_controller, 1);
        SalesServiceMock.Setup(x => x.CancelAsync(invoiceId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(cancelledInvoice));

        // Act
        var result = await _controller.Cancel(invoiceId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<SalesInvoiceDto>();
    }

    [Fact]
    public async Task Cancel_WhenInvoiceNotFound_ReturnsBadRequest()
    {
        // Arrange
        var invoiceId = 999;
        SetupUserId(_controller, 1);
        SalesServiceMock.Setup(x => x.CancelAsync(invoiceId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SalesInvoiceDto>("الفاتورة غير موجودة"));

        // Act
        var result = await _controller.Cancel(invoiceId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Cancel_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupUserWithoutId(_controller);

        // Act
        var result = await _controller.Cancel(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region Helper Methods

    private static SalesInvoiceDto CreateInvoiceDto(int id, byte status) => new(
        Id: id,
        InvoiceNo: $"INV-2026-{id:D6}",
        CustomerId: 1,
        CustomerName: "عميل اختبار",
        WarehouseId: 1,
        WarehouseName: "المستودع الرئيسي",
        InvoiceDate: DateTime.UtcNow,
        DueDate: null,
        PaymentType: 1,
        SubTotal: 100.00m,
        DiscountAmount: 0.00m,
        TaxAmount: 15.00m,
        TotalAmount: 115.00m,
        PaidAmount: 50.00m,
        DueAmount: 65.00m,
        Notes: null,
        Status: status,
        Items: new List<SalesInvoiceItemDto>
        {
            new(id * 10, 1, null, "منتج اختبار", 2.000m, 50.00m, 0.00m, 100.00m)
        });

    private static CreateSalesInvoiceRequest CreateValidRequest() => new(
        CustomerId: 1,
        WarehouseId: 1,
        InvoiceDate: null,
        DueDate: null,
        PaymentType: PaymentType.Cash,
        DiscountAmount: 0.00m,
        TaxRate: 15.00m,
        PaidAmount: 50.00m,
        Notes: null,
        Items: new List<CreateSalesInvoiceItemRequest>
        {
            new(ProductId: 1, Quantity: 2.000m, UnitPrice: 50.00m, DiscountAmount: 0.00m, Notes: null)
        });

    #endregion
}