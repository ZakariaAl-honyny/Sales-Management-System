using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers.Returns;

[Trait("Category", "SalesReturnsController")]
public class SalesReturnsControllerTests : ControllerTestBase
{
    private readonly SalesReturnsController _controller;

    public SalesReturnsControllerTests()
    {
        var loggerMock = new Mock<ILogger<SalesReturnsController>>();
        _controller = new SalesReturnsController(SalesReturnServiceMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        var expectedResult = new PagedResult<SalesReturnDto>
        {
            Items = new List<SalesReturnDto> { CreateReturnDto(1), CreateReturnDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        SalesReturnServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(expectedResult));

        var result = await _controller.GetAll(null, false, 1, 10, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        SalesReturnServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<SalesReturnDto>>("حدث خطأ"));

        var result = await _controller.GetAll(null, false, 1, 10, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenReturnExists_ReturnsOkWithReturn()
    {
        var returnId = 1;
        var returnDto = CreateReturnDto(returnId);
        SalesReturnServiceMock.Setup(x => x.GetByIdAsync(returnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(returnDto));

        var result = await _controller.GetById(returnId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenReturnNotFound_ReturnsNotFound()
    {
        var returnId = 999;
        SalesReturnServiceMock.Setup(x => x.GetByIdAsync(returnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SalesReturnDto>("إرجاع غير موجود"));

        var result = await _controller.GetById(returnId, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtActionWithReturn()
    {
        var createdReturn = CreateReturnDto(1);
        SetupUserId(_controller, 1);
        var request = CreateValidRequest();
        SalesReturnServiceMock.Setup(x => x.CreateAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdReturn));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(SalesReturnsController.GetById));
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        SetupUserId(_controller, 1);
        var request = CreateValidRequest();
        SalesReturnServiceMock.Setup(x => x.CreateAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SalesReturnDto>("بيانات غير صالحة"));

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

    private static SalesReturnDto CreateReturnDto(int id) => new(
        Id: id,
        ReturnNo: $"SR-2026-{id:D6}",
        WarehouseId: 1,
        WarehouseName: "المستودع الرئيسي",
        CustomerId: 1,
        CustomerName: "عميل اختبار",
        SalesInvoiceId: 10 + id,
        ReturnDate: DateTime.UtcNow,
        SubTotal: 100.00m,
        TaxAmount: 0.00m,
        DiscountAmount: 0.00m,
        TotalAmount: 100.00m,
        CurrencyId: null,
        ExchangeRate: null,
        Notes: "ملاحظات",
        Status: 1,
        CashBoxId: null,
        CashBoxName: null,
        RefundAmount: 0m,
        Items: new List<SalesReturnItemDto>
        {
            new(id * 10, 1, "منتج اختبار", 2.000m, 50.00m, 0.00m, 100.00m, 1)
        });

    private static CreateSalesReturnRequest CreateValidRequest() => new(
        SalesInvoiceId: 10,
        CustomerId: 1,
        WarehouseId: (short)1,
        ReturnDate: null,
        CurrencyId: null,
        Notes: "ملاحظات إرجاع",
        Items: new List<ReturnItemRequest>
        {
            new(SalesInvoiceLineId: 1, ProductId: 1, ProductUnitId: 1, Quantity: 2.000m, UnitPrice: 50.00m, Amount: 100.00m, DiscountAmount: 0.00m)
        });
}

[Trait("Category", "PurchaseReturnsController")]
public class PurchaseReturnsControllerTests : ControllerTestBase
{
    private readonly PurchaseReturnsController _controller;

    public PurchaseReturnsControllerTests()
    {
        var loggerMock = new Mock<ILogger<PurchaseReturnsController>>();
        _controller = new PurchaseReturnsController(PurchaseReturnServiceMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        var expectedResult = new PagedResult<PurchaseReturnDto>
        {
            Items = new List<PurchaseReturnDto> { CreateReturnDto(1), CreateReturnDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        PurchaseReturnServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(expectedResult));

        var result = await _controller.GetAll(null, 1, 10, false, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        PurchaseReturnServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<PurchaseReturnDto>>("حدث خطأ"));

        var result = await _controller.GetAll(null, 1, 10, false, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenReturnExists_ReturnsOkWithReturn()
    {
        var returnId = 1;
        var returnDto = CreateReturnDto(returnId);
        PurchaseReturnServiceMock.Setup(x => x.GetByIdAsync(returnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(returnDto));

        var result = await _controller.GetById(returnId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenReturnNotFound_ReturnsNotFound()
    {
        var returnId = 999;
        PurchaseReturnServiceMock.Setup(x => x.GetByIdAsync(returnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PurchaseReturnDto>("إرجاع غير موجود"));

        var result = await _controller.GetById(returnId, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtActionWithReturn()
    {
        var createdReturn = CreateReturnDto(1);
        SetupUserId(_controller, 1);
        var request = CreateValidRequest();
        PurchaseReturnServiceMock.Setup(x => x.CreateAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdReturn));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(PurchaseReturnsController.GetById));
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        SetupUserId(_controller, 1);
        var request = CreateValidRequest();
        PurchaseReturnServiceMock.Setup(x => x.CreateAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PurchaseReturnDto>("بيانات غير صالحة"));

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

    private static PurchaseReturnDto CreateReturnDto(int id) => new(
        Id: id,
        ReturnNo: id,
        WarehouseId: 1,
        WarehouseName: "المستودع الرئيسي",
        SupplierId: 1,
        SupplierName: "مورد اختبار",
        PurchaseInvoiceId: 10 + id,
        LinkToInvoice: true,
        ReturnDate: DateTime.UtcNow,
        SubTotal: 200.00m,
        TotalAmount: 200.00m,
        CurrencyId: null,
        ExchangeRate: null,
        Notes: "ملاحظات",
        Status: 1,
        Items: new List<PurchaseReturnItemDto>
        {
            new(id * 10, 1, "منتج اختبار", 1, null, 5.000m, 40.00m, 200.00m)
        });

    private static CreatePurchaseReturnRequest CreateValidRequest() => new(
        PurchaseInvoiceId: 10,
        SupplierId: 1,
        WarehouseId: 1,
        ReturnDate: null,
        CurrencyId: null,
        ExchangeRate: null,
        Notes: "ملاحظات إرجاع",
        Items: new List<CreatePurchaseReturnItemRequest>
        {
            new(PurchaseInvoiceLineId: 1, ProductId: 1, ProductUnitId: 1, Quantity: 5.000m, UnitCost: 40.00m, Amount: 200.00m)
        });
}
