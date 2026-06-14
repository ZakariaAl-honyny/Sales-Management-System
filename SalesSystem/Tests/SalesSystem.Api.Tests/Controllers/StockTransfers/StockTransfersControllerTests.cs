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

public class WarehouseTransfersControllerTests
{
    private readonly Mock<IWarehouseTransferService> _warehouseTransferServiceMock;
    private readonly WarehouseTransfersController _controller;

    public WarehouseTransfersControllerTests()
    {
        _warehouseTransferServiceMock = new Mock<IWarehouseTransferService>();
        _controller = new WarehouseTransfersController(_warehouseTransferServiceMock.Object);

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
        var transfers = new List<WarehouseTransferDto>
        {
            new(1, 1, (short)1, "من المستودع الرئيسي", (short)2, "إلى المستودع الفرعي", DateTime.Now, null, 1, Array.Empty<WarehouseTransferLineDto>())
        };
        var pagedResult = PagedResult<WarehouseTransferDto>.Create(transfers, 1, 1, 10);

        _warehouseTransferServiceMock
            .Setup(x => x.GetAllAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<WarehouseTransferDto>>.Success(pagedResult));

        var result = await _controller.GetAll(null, null, 1, 10, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        _warehouseTransferServiceMock
            .Setup(x => x.GetAllAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<WarehouseTransferDto>>.Failure("فشل في جلب التحويلات"));

        var result = await _controller.GetAll(null, null, 1, 10, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenTransferExists_ReturnsOkWithTransfer()
    {
        var transfer = new WarehouseTransferDto(1, 1, (short)1, "من المستودع الرئيسي", (short)2, "إلى المستودع الفرعي", DateTime.Now, null, 1, Array.Empty<WarehouseTransferLineDto>());

        _warehouseTransferServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseTransferDto>.Success(transfer));

        var result = await _controller.GetById(1, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetById_WhenTransferNotFound_ReturnsNotFound()
    {
        _warehouseTransferServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseTransferDto>.Failure("التحويل غير موجود", ErrorCodes.NotFound));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreateWarehouseTransferRequest(1, (short)1, (short)2, DateTime.Now, null, new List<CreateWarehouseTransferLineRequest> { new(1, 1, 10m, 0m) });
        var transfer = new WarehouseTransferDto(1, 1, (short)1, "من المستودع الرئيسي", (short)2, "إلى المستودع الفرعي", DateTime.Now, null, 1, Array.Empty<WarehouseTransferLineDto>());

        _warehouseTransferServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateWarehouseTransferRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseTransferDto>.Success(transfer));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new CreateWarehouseTransferRequest(1, (short)1, (short)2, DateTime.Now, null, new List<CreateWarehouseTransferLineRequest> { new(1, 1, 10m, 0m) });

        _warehouseTransferServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateWarehouseTransferRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WarehouseTransferDto>.Failure("فشل في إنشاء التحويل"));

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

        var request = new CreateWarehouseTransferRequest(1, (short)1, (short)2, DateTime.Now, null, new List<CreateWarehouseTransferLineRequest> { new(1, 1, 10m, 0m) });

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }
}
