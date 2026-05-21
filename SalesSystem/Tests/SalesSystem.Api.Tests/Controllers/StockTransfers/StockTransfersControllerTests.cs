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

/// <summary>
/// Unit tests for StockTransfersController HTTP status codes
/// </summary>
public class StockTransfersControllerTests
{
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly StockTransfersController _controller;

    public StockTransfersControllerTests()
    {
        _inventoryServiceMock = new Mock<IInventoryService>();
        _controller = new StockTransfersController(_inventoryServiceMock.Object);
        
        // Setup controller context with user claims
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };
    }

    #region GetAll Tests

    /// <summary>
    /// Given transfers exist, when getting all transfers, then returns 200 OK with paged result
    /// </summary>
    [Fact]
    public async Task GetAll_WhenTransfersExist_ReturnsOkWithPagedResult()
    {
        // Arrange
        var transfers = new List<StockTransferDto>
        {
            new(1, DateTime.Now, 1, "من المستودع الرئيسي", 2, "إلى المستودع الفرعي", 10, "مكتمل", 1)
        };
        var pagedResult = new PagedResult<StockTransferDto>(transfers, 1, 1, 10);

        _inventoryServiceMock
            .Setup(x => x.GetAllTransfersAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<StockTransferDto>>.Success(pagedResult));

        // Act
        var result = await _controller.GetAll(null, null, 1, 10, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given service fails, when getting all transfers, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _inventoryServiceMock
            .Setup(x => x.GetAllTransfersAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<StockTransferDto>>.Failure("فشل في جلب التحويلات"));

        // Act
        var result = await _controller.GetAll(null, null, 1, 10, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetById Tests

    /// <summary>
    /// Given transfer exists, when getting by id, then returns 200 OK with transfer
    /// </summary>
    [Fact]
    public async Task GetById_WhenTransferExists_ReturnsOkWithTransfer()
    {
        // Arrange
        var transfer = new StockTransferDto(1, DateTime.Now, 1, "من المستودع الرئيسي", 2, "إلى المستودع الفرعي", 10, "مكتمل", 1);

        _inventoryServiceMock
            .Setup(x => x.GetTransferByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockTransferDto>.Success(transfer));

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Given transfer not found, when getting by id, then returns 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetById_WhenTransferNotFound_ReturnsNotFound()
    {
        // Arrange
        _inventoryServiceMock
            .Setup(x => x.GetTransferByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockTransferDto>.Failure("التحويل غير موجود", ErrorCodes.NotFound));

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    /// <summary>
    /// Given valid request, when creating transfer, then returns 201 Created
    /// </summary>
    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateStockTransferRequest(1, 2, 1, 10);
        var transfer = new StockTransferDto(1, DateTime.Now, 1, "من المستودع الرئيسي", 2, "إلى المستودع الفرعي", 10, "مكتمل", 1);

        _inventoryServiceMock
            .Setup(x => x.CreateTransferAsync(It.IsAny<CreateStockTransferRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockTransferDto>.Success(transfer));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    /// <summary>
    /// Given service fails, when creating transfer, then returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateStockTransferRequest(1, 2, 1, 10);

        _inventoryServiceMock
            .Setup(x => x.CreateTransferAsync(It.IsAny<CreateStockTransferRequest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockTransferDto>.Failure("فشل في إنشاء التحويل"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given no user id, when creating transfer, then returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task Create_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new CreateStockTransferRequest(1, 2, 1, 10);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion
}