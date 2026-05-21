using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using SalesSystem.Api.Controllers;
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
        _controller = new SalesReturnsController(SalesReturnServiceMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        // Arrange
        var expectedResult = new PagedResult<SalesReturnDto>
        {
            Items = new List<SalesReturnDto> { CreateReturnDto(1), CreateReturnDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        SalesReturnServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(expectedResult));

        // Act
        var result = await _controller.GetAll(null, 1, 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        SalesReturnServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<SalesReturnDto>>("حدث خطأ"));

        // Act
        var result = await _controller.GetAll(null, 1, 10);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenReturnExists_ReturnsOkWithReturn()
    {
        // Arrange
        var returnId = 1;
        var returnDto = CreateReturnDto(returnId);
        SalesReturnServiceMock.Setup(x => x.GetByIdAsync(returnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(returnDto));

        // Act
        var result = await _controller.GetById(returnId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenReturnNotFound_ReturnsNotFound()
    {
        // Arrange
        var returnId = 999;
        SalesReturnServiceMock.Setup(x => x.GetByIdAsync(returnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SalesReturnDto>("إرجاع غير موجود"));

        // Act
        var result = await _controller.GetById(returnId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtActionWithReturn()
    {
        // Arrange
        var createdReturn = CreateReturnDto(1);
        SetupUserId(_controller, 1);
        var request = CreateValidRequest();
        SalesReturnServiceMock.Setup(x => x.CreateAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdReturn));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(SalesReturnsController.GetById));
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        SetupUserId(_controller, 1);
        var request = CreateValidRequest();
        SalesReturnServiceMock.Setup(x => x.CreateAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<SalesReturnDto>("بيانات غير صالحة"));

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

    #region Helper Methods

    private static SalesReturnDto CreateReturnDto(int id) => new(
        Id: id,
        ReturnNo: $"SR-2026-{id:D6}",
        WarehouseId: 1,
        WarehouseName: "المستودع الرئيسي",
        CustomerId: 1,
        CustomerName: "عميل اختبار",
        SalesInvoiceId: 10 + id,
        ReturnDate: DateTime.UtcNow,
        TotalAmount: 100.00m,
        Notes: "ملاحظات",
        Status: 1,
        Items: new List<SalesReturnItemDto>
        {
            new(id * 10, 1, null, "منتج اختبار", 2.000m, 50.00m, 0.00m, 100.00m)
        });

    private static CreateSalesReturnRequest CreateValidRequest() => new(
        SalesInvoiceId: 10,
        WarehouseId: 1,
        CustomerId: 1,
        ReturnDate: null,
        Notes: "ملاحظات إرجاع",
        Status: 1,
        Items: new List<ReturnItemRequest>
        {
            new(ProductId: 1, Quantity: 2.000m, UnitPrice: 50.00m, DiscountAmount: 0.00m)
        });

    #endregion
}

[Trait("Category", "PurchaseReturnsController")]
public class PurchaseReturnsControllerTests : ControllerTestBase
{
    private readonly PurchaseReturnsController _controller;

    public PurchaseReturnsControllerTests()
    {
        _controller = new PurchaseReturnsController(PurchaseReturnServiceMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        // Arrange
        var expectedResult = new PagedResult<PurchaseReturnDto>
        {
            Items = new List<PurchaseReturnDto> { CreateReturnDto(1), CreateReturnDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        PurchaseReturnServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(expectedResult));

        // Act
        var result = await _controller.GetAll(null, 1, 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        PurchaseReturnServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<PurchaseReturnDto>>("حدث خطأ"));

        // Act
        var result = await _controller.GetAll(null, 1, 10);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenReturnExists_ReturnsOkWithReturn()
    {
        // Arrange
        var returnId = 1;
        var returnDto = CreateReturnDto(returnId);
        PurchaseReturnServiceMock.Setup(x => x.GetByIdAsync(returnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(returnDto));

        // Act
        var result = await _controller.GetById(returnId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenReturnNotFound_ReturnsNotFound()
    {
        // Arrange
        var returnId = 999;
        PurchaseReturnServiceMock.Setup(x => x.GetByIdAsync(returnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PurchaseReturnDto>("إرجاع غير موجود"));

        // Act
        var result = await _controller.GetById(returnId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtActionWithReturn()
    {
        // Arrange
        var createdReturn = CreateReturnDto(1);
        SetupUserId(_controller, 1);
        var request = CreateValidRequest();
        PurchaseReturnServiceMock.Setup(x => x.CreateAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdReturn));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(PurchaseReturnsController.GetById));
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        SetupUserId(_controller, 1);
        var request = CreateValidRequest();
        PurchaseReturnServiceMock.Setup(x => x.CreateAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PurchaseReturnDto>("بيانات غير صالحة"));

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

    #region Helper Methods

    private static PurchaseReturnDto CreateReturnDto(int id) => new(
        Id: id,
        ReturnNo: $"PR-2026-{id:D6}",
        WarehouseId: 1,
        WarehouseName: "المستودع الرئيسي",
        SupplierId: 1,
        SupplierName: "مورد اختبار",
        PurchaseInvoiceId: 10 + id,
        ReturnDate: DateTime.UtcNow,
        TotalAmount: 200.00m,
        Notes: "ملاحظات",
        Status: 1,
        Items: new List<PurchaseReturnItemDto>
        {
            new(id * 10, 1, null, "منتج اختبار", 5.000m, 40.00m, 0.00m, 200.00m)
        });

    private static CreatePurchaseReturnRequest CreateValidRequest() => new(
        PurchaseInvoiceId: 10,
        SupplierId: 1,
        WarehouseId: 1,
        ReturnDate: null,
        Notes: "ملاحظات إرجاع",
        Status: 1,
        Items: new List<ReturnItemRequest>
        {
            new(ProductId: 1, Quantity: 5.000m, UnitPrice: 40.00m, DiscountAmount: 0.00m)
        });

    #endregion
}