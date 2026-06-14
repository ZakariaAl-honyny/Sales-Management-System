using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
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

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        var expectedResult = new PagedResult<SalesInvoiceDto>
        {
            Items = new List<SalesInvoiceDto> { CreateInvoiceDto(1, 1), CreateInvoiceDto(2, 2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        SalesServiceMock.Setup(x => x.GetAllAsync(null, null, null, null, null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(expectedResult));

        var result = await _controller.GetAll(null, null, null, null, null, false, 1, 10, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        SalesServiceMock.Setup(x => x.GetAllAsync(null, null, null, null, null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<SalesInvoiceDto>>("حدث خطأ"));

        var result = await _controller.GetAll(null, null, null, null, null, false, 1, 10, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAll_WithCustomerFilter_PassesFilterToService()
    {
        var customerId = 5;
        var pagedResult = new PagedResult<SalesInvoiceDto> { Items = new List<SalesInvoiceDto>(), Page = 1, PageSize = 10, TotalCount = 0 };
        SalesServiceMock.Setup(x => x.GetAllAsync(customerId, null, null, null, null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(pagedResult));

        await _controller.GetAll(customerId, null, null, null, null, false, 1, 10, CancellationToken.None);

        SalesServiceMock.Verify(x => x.GetAllAsync(customerId, null, null, null, null, 1, 10, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenInvoiceExists_ReturnsOkWithInvoice()
    {
        var invoiceId = 1;
        var invoiceDto = CreateInvoiceDto(invoiceId, 1);
        SalesServiceMock.Setup(x => x.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(invoiceDto));

        var result = await _controller.GetById(invoiceId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<SalesInvoiceDto>();
    }

    [Fact]
    public async Task GetById_WhenInvoiceNotFound_ReturnsNotFound()
    {
        var invoiceId = 999;
        SalesServiceMock.Setup(x => x.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SalesInvoiceDto>("الفاتورة غير موجودة"));

        var result = await _controller.GetById(invoiceId, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtActionWithInvoice()
    {
        var createdInvoice = CreateInvoiceDto(1, 1);
        SetupUserId(_controller, 1);
        var request = CreateValidRequest();
        SalesServiceMock.Setup(x => x.CreateAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdInvoice));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(SalesInvoicesController.GetById));
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        SetupUserId(_controller, 1);
        var request = CreateValidRequest();
        SalesServiceMock.Setup(x => x.CreateAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SalesInvoiceDto>("بيانات غير صالحة"));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WithoutUserId_ReturnsUnauthorized()
    {
        SetupUserWithoutId(_controller);
        var request = CreateValidRequest();

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Post_WhenDraftInvoice_ReturnsOkWithPostedInvoice()
    {
        var invoiceId = 1;
        var postedInvoice = CreateInvoiceDto(invoiceId, 2);
        SetupUserId(_controller, 1);
        SalesServiceMock.Setup(x => x.PostAsync(invoiceId, It.IsAny<PostSalesInvoiceRequest>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(postedInvoice));

        var result = await _controller.Post(invoiceId, new PostSalesInvoiceRequest(), CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<SalesInvoiceDto>();
    }

    [Fact]
    public async Task Post_WhenInvoiceNotFound_ReturnsBadRequest()
    {
        var invoiceId = 999;
        SetupUserId(_controller, 1);
        SalesServiceMock.Setup(x => x.PostAsync(invoiceId, It.IsAny<PostSalesInvoiceRequest>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SalesInvoiceDto>("الفاتورة غير موجودة"));

        var result = await _controller.Post(invoiceId, new PostSalesInvoiceRequest(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Post_WithoutUserId_ReturnsUnauthorized()
    {
        SetupUserWithoutId(_controller);

        var result = await _controller.Post(1, new PostSalesInvoiceRequest(), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Cancel_WhenPostedInvoice_ReturnsOkWithCancelledInvoice()
    {
        var invoiceId = 1;
        var cancelledInvoice = CreateInvoiceDto(invoiceId, 3);
        SetupUserId(_controller, 1);
        SalesServiceMock.Setup(x => x.CancelAsync(invoiceId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(cancelledInvoice));

        var result = await _controller.Cancel(invoiceId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<SalesInvoiceDto>();
    }

    [Fact]
    public async Task Cancel_WhenInvoiceNotFound_ReturnsBadRequest()
    {
        var invoiceId = 999;
        SetupUserId(_controller, 1);
        SalesServiceMock.Setup(x => x.CancelAsync(invoiceId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SalesInvoiceDto>("الفاتورة غير موجودة"));

        var result = await _controller.Cancel(invoiceId, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Cancel_WithoutUserId_ReturnsUnauthorized()
    {
        SetupUserWithoutId(_controller);

        var result = await _controller.Cancel(1, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    private static SalesInvoiceDto CreateInvoiceDto(int id, byte status) => new(
        Id: id,
        InvoiceNo: 1,
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
        OtherCharges: 0m,
        TotalAmount: 115.00m,
        PaidAmount: 50.00m,
        DueAmount: 65.00m,
        Notes: null,
        Status: status,
        TaxId: null,
        TaxName: null,
        TaxRate: null,
        CurrencyId: null,
        ExchangeRate: null,
        CashBoxId: null,
        CashBoxName: null,
        TotalCost: null,
        TotalProfit: null,
        Items: new List<SalesInvoiceItemDto>
        {
            new(id * 10, 1, "منتج اختبار", 2.000m, 50.00m, 0.00m, 100.00m, 1)
        });

    private static CreateSalesInvoiceRequest CreateValidRequest() => new(
        WarehouseId: 1,
        InvoiceNo: null,
        CustomerId: 1,
        CashBoxId: null,
        InvoiceDate: null,
        DueDate: null,
        PaymentType: PaymentType.Cash,
        DiscountAmount: 0.00m,
        TaxAmount: 15.00m,
        OtherCharges: 0m,
        PaidAmount: 50.00m,
        Notes: null,
        CurrencyId: null,
        ExchangeRate: null,
        TaxId: null,
        Items: new List<CreateSalesInvoiceItemRequest>
        {
            new(ProductId: 1, Quantity: 2.000m, UnitPrice: 50.00m, DiscountAmount: 0.00m, Mode: SaleMode.Retail, Notes: null)
        });
}
