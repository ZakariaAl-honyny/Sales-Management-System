using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Tests.Controllers.StockTransfers;

public class StockTransfersControllerTests
{
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly StockTransfersController _controller;

    public StockTransfersControllerTests()
    {
        _inventoryServiceMock = new Mock<IInventoryService>();
        _controller = new StockTransfersController(_inventoryServiceMock.Object);

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetAll_WhenTransfersExist_ReturnsOkWithPagedResult()
    {
        var transfers = new List<StockTransferDto>
        {
            new(1, "TRF-001", 1, "من المستودع الرئيسي", 2, "إلى المستودع الفرعي", DateTime.Now, null, 1, Array.Empty<StockTransferItemDto>())
        };
        var pagedResult = PagedResult<StockTransferDto>.Create(transfers, 1, 1, 10);

        _inventoryServiceMock
            .Setup(x => x.GetAllTransfersAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<StockTransferDto>>.Success(pagedResult));

        var result = await _controller.GetAll(null, null, 1, 10, false, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        _inventoryServiceMock
            .Setup(x => x.GetAllTransfersAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<StockTransferDto>>.Failure("فشل في جلب التحويلات"));

        var result = await _controller.GetAll(null, null, 1, 10, false, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenTransferExists_ReturnsOkWithTransfer()
    {
        var transfer = new StockTransferDto(1, "TRF-001", 1, "من المستودع الرئيسي", 2, "إلى المستودع الفرعي", DateTime.Now, null, 1, Array.Empty<StockTransferItemDto>());

        _inventoryServiceMock
            .Setup(x => x.GetTransferByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockTransferDto>.Success(transfer));

        var result = await _controller.GetById(1, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetById_WhenTransferNotFound_ReturnsNotFound()
    {
        _inventoryServiceMock
            .Setup(x => x.GetTransferByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockTransferDto>.Failure("التحويل غير موجود", ErrorCodes.NotFound));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreateStockTransferRequest(1, 2, DateTime.Now, null, new List<CreateStockTransferItemRequest> { new(1, 10m) });
        var transfer = new StockTransferDto(1, "TRF-001", 1, "من المستودع الرئيسي", 2, "إلى المستودع الفرعي", DateTime.Now, null, 1, Array.Empty<StockTransferItemDto>());

        _inventoryServiceMock
            .Setup(x => x.CreateTransferAsync(It.IsAny<CreateStockTransferRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockTransferDto>.Success(transfer));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new CreateStockTransferRequest(1, 2, DateTime.Now, null, new List<CreateStockTransferItemRequest> { new(1, 10m) });

        _inventoryServiceMock
            .Setup(x => x.CreateTransferAsync(It.IsAny<CreateStockTransferRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockTransferDto>.Failure("فشل في إنشاء التحويل"));

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

        var request = new CreateStockTransferRequest(1, 2, DateTime.Now, null, new List<CreateStockTransferItemRequest> { new(1, 10m) });

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }
}
