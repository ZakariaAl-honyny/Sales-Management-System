using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Api.Controllers;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers.Products;

public class ProductsControllerTests : ControllerTestBase
{
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _controller = new ProductsController(ProductServiceMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        // Arrange
        var products = new PagedResult<ProductDto>
        {
            Items = new List<ProductDto> { CreateProductDto(1), CreateProductDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        ProductServiceMock.Setup(x => x.GetAllAsync(null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(products));

        // Act
        var result = await _controller.GetAll(null, null, 1, 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        ProductServiceMock.Setup(x => x.GetAllAsync(null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<ProductDto>>("فشل في استرجاع المنتجات"));

        // Act
        var result = await _controller.GetAll(null, null, 1, 10);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenProductExists_ReturnsOkWithProduct()
    {
        // Arrange
        var product = CreateProductDto(1);
        ProductServiceMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(product));

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenProductNotFound_ReturnsNotFound()
    {
        // Arrange
        ProductServiceMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<ProductDto>("المنتج غير موجود"));

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = CreateValidRequest();
        var createdProduct = CreateProductDto(1);
        ProductServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdProduct));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(ProductsController.GetById));
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();
        ProductServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<ProductDto>("رمز المنتج موجود مسبقاً"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedProduct()
    {
        // Arrange
        var request = UpdateValidRequest();
        var updatedProduct = CreateProductDto(1);
        ProductServiceMock.Setup(x => x.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(updatedProduct));

        // Act
        var result = await _controller.Update(1, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenProductNotFound_ReturnsBadRequest()
    {
        // Arrange
        var request = UpdateValidRequest();
        ProductServiceMock.Setup(x => x.UpdateAsync(999, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<ProductDto>("المنتج غير موجود"));

        // Act
        var result = await _controller.Update(999, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WhenProductExists_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        ProductServiceMock.Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenProductNotFound_ReturnsBadRequest()
    {
        // Arrange
        ProductServiceMock.Setup(x => x.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("المنتج غير موجود"));

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static ProductDto CreateProductDto(int id) => new(
        Id: id,
        Code: $"P{id:D3}",
        Barcode: null,
        Name: $"منتج {id}",
        CategoryId: 1,
        CategoryName: "تصنيف",
        UnitId: 1,
        UnitName: "قطعة",
        PurchasePrice: 50.00m,
        SalePrice: 100.00m,
        MinStock: 10.00m,
        Description: null,
        IsActive: true);

    private static CreateProductRequest CreateValidRequest() => new(
        Code: "P001",
        Barcode: null,
        Name: "منتج جديد",
        CategoryId: 1,
        UnitId: 1,
        PurchasePrice: 50.00m,
        SalePrice: 100.00m,
        MinStock: 10.00m,
        Description: null);

    private static UpdateProductRequest UpdateValidRequest() => new(
        Code: "P001",
        Barcode: null,
        Name: "منتج محدث",
        CategoryId: 1,
        UnitId: 1,
        PurchasePrice: 55.00m,
        SalePrice: 110.00m,
        MinStock: 15.00m,
        Description: "وصف محدث",
        IsActive: true);

    #endregion
}