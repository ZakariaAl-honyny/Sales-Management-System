using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers.Purchases;

[Trait("Category", "PurchaseInvoicesController")]
public class PurchaseInvoicesControllerTests : ControllerTestBase
{
    private readonly PurchaseInvoicesController _controller;
    private readonly Mock<ILogger<PurchaseInvoicesController>> _loggerMock;

    public PurchaseInvoicesControllerTests()
    {
        _loggerMock = new Mock<ILogger<PurchaseInvoicesController>>();
        _controller = new PurchaseInvoicesController(PurchaseServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        var expectedResult = new PagedResult<PurchaseInvoiceDto>
        {
            Items = new List<PurchaseInvoiceDto> { CreateInvoiceDto(1, 1), CreateInvoiceDto(2, 2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        PurchaseServiceMock.Setup(x => x.GetAllAsync(null, null, null, null, null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(expectedResult));

        var result = await _controller.GetAll(null, null, null, null, null, 1, 10, false, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        PurchaseServiceMock.Setup(x => x.GetAllAsync(null, null, null, null, null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<PurchaseInvoiceDto>>("حدث خطأ"));

        var result = await _controller.GetAll(null, null, null, null, null, 1, 10, false, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenInvoiceExists_ReturnsOkWithInvoice()
    {
        var invoiceId = 1;
        var invoiceDto = CreateInvoiceDto(invoiceId, 1);
        PurchaseServiceMock.Setup(x => x.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(invoiceDto));

        var result = await _controller.GetById(invoiceId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenInvoiceNotFound_ReturnsNotFound()
    {
        var invoiceId = 999;
        PurchaseServiceMock.Setup(x => x.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PurchaseInvoiceDto>("الفاتورة غير موجودة"));

        var result = await _controller.GetById(invoiceId, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtActionWithInvoice()
    {
        var createdInvoice = CreateInvoiceDto(1, 1);
        SetupUserId(_controller, 1);
        var request = CreateValidRequest();
        PurchaseServiceMock.Setup(x => x.CreateAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdInvoice));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(PurchaseInvoicesController.GetById));
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        SetupUserId(_controller, 1);
        var request = CreateValidRequest();
        PurchaseServiceMock.Setup(x => x.CreateAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PurchaseInvoiceDto>("بيانات غير صالحة"));

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
        PurchaseServiceMock.Setup(x => x.PostAsync(invoiceId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(postedInvoice));

        var result = await _controller.Post(invoiceId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Post_WhenInvoiceNotFound_ReturnsBadRequest()
    {
        var invoiceId = 999;
        SetupUserId(_controller, 1);
        PurchaseServiceMock.Setup(x => x.PostAsync(invoiceId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PurchaseInvoiceDto>("الفاتورة غير موجودة"));

        var result = await _controller.Post(invoiceId, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Post_WithoutUserId_ReturnsUnauthorized()
    {
        SetupUserWithoutId(_controller);

        var result = await _controller.Post(1, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Cancel_WhenPostedInvoice_ReturnsOkWithCancelledInvoice()
    {
        var invoiceId = 1;
        var cancelledInvoice = CreateInvoiceDto(invoiceId, 3);
        SetupUserId(_controller, 1);
        PurchaseServiceMock.Setup(x => x.CancelAsync(invoiceId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(cancelledInvoice));

        var result = await _controller.Cancel(invoiceId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Cancel_WhenInvoiceNotFound_ReturnsBadRequest()
    {
        var invoiceId = 999;
        SetupUserId(_controller, 1);
        PurchaseServiceMock.Setup(x => x.CancelAsync(invoiceId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PurchaseInvoiceDto>("الفاتورة غير موجودة"));

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

    private static PurchaseInvoiceDto CreateInvoiceDto(int id, byte status) => new(
        Id: id,
        InvoiceNo: 1,
        SupplierId: 1,
        SupplierName: "مورد اختبار",
        WarehouseId: 1,
        WarehouseName: "المستودع الرئيسي",
        InvoiceDate: DateOnly.FromDateTime(DateTime.UtcNow),
        PaymentType: 1,
        SubTotal: 200.00m,
        DiscountAmount: 10.00m,
        TaxAmount: 28.50m,
        OtherCharges: 0,
        NetTotal: 218.50m,
        PaidAmount: 100.00m,
        RemainingAmount: 118.50m,
        Notes: null,
        SupplierInvoiceNo: null,
        Status: status,
        TaxId: null,
        TaxName: null,
        TaxRate: null,
        CurrencyId: null,
        ExchangeRate: null,
        CashBoxId: null,
        Items: new List<PurchaseInvoiceLineDto>
        {
            new(id * 10, 1, "منتج اختبار", 1, null, 5.000m, 40.00m, 200.00m, 0m)
        });

    private static CreatePurchaseInvoiceRequest CreateValidRequest() => new(
        WarehouseId: 1,
        SupplierId: 1,
        InvoiceNo: null,
        InvoiceDate: null,
        PaymentType: PaymentType.Cash,
        DiscountAmount: 10.00m,
        TaxAmount: 28.50m,
        OtherCharges: 0,
        PaidAmount: 100.00m,
        CurrencyId: null,
        ExchangeRate: null,
        Notes: null,
        Items: new List<CreatePurchaseInvoiceLineRequest>
        {
            new(ProductId: 1, ProductUnitId: 1, Quantity: 5.000m, UnitPrice: 40.00m)
        });
}

