using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
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

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        var products = new PagedResult<ProductDto>
        {
            Items = new List<ProductDto> { CreateProductDto(1), CreateProductDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        ProductServiceMock.Setup(x => x.GetAllAsync(null, null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(products));

        var result = await _controller.GetAll(null, null, 1, 10, false, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        ProductServiceMock.Setup(x => x.GetAllAsync(null, null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<ProductDto>>("فشل في استرجاع المنتجات"));

        var result = await _controller.GetAll(null, null, 1, 10, false, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenProductExists_ReturnsOkWithProduct()
    {
        var product = CreateProductDto(1);
        ProductServiceMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(product));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenProductNotFound_ReturnsNotFound()
    {
        ProductServiceMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<ProductDto>("المنتج غير موجود"));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
    {
        var request = CreateValidRequest();
        var createdProduct = CreateProductDto(1);
        ProductServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdProduct));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(ProductsController.GetById));
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var request = CreateValidRequest();
        ProductServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<ProductDto>("رمز المنتج موجود مسبقاً"));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedProduct()
    {
        var request = UpdateValidRequest();
        var updatedProduct = CreateProductDto(1);
        ProductServiceMock.Setup(x => x.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(updatedProduct));

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenProductNotFound_ReturnsBadRequest()
    {
        var request = UpdateValidRequest();
        ProductServiceMock.Setup(x => x.UpdateAsync(999, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<ProductDto>("المنتج غير موجود"));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenProductExists_ReturnsOkWithSuccessMessage()
    {
        ProductServiceMock.Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        var result = await _controller.Delete(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenProductNotFound_ReturnsBadRequest()
    {
        ProductServiceMock.Setup(x => x.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("المنتج غير موجود"));

        var result = await _controller.Delete(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PermanentDelete_WhenProductExists_ReturnsOkWithSuccessMessage()
    {
        ProductServiceMock.Setup(x => x.PermanentDeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        var result = await _controller.PermanentDelete(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PermanentDelete_WhenProductNotFound_ReturnsBadRequest()
    {
        ProductServiceMock.Setup(x => x.PermanentDeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("المنتج غير موجود"));

        var result = await _controller.PermanentDelete(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static ProductDto CreateProductDto(int id) => new(
        Id: id,
        Name: $"منتج {id}",
        CategoryId: 1,
        CategoryName: "تصنيف",
        Barcode: null,
        Description: null,
        ReorderLevel: 10.00m,
        TrackExpiry: false,
        ImagePath: null,
        Notes: null,
        IsActive: true);

    private static CreateProductRequest CreateValidRequest() => new(
        Name: "منتج جديد",
        CategoryId: 1,
        Description: null,
        Barcode: null!,
        ReorderLevel: 10.00m);

    private static UpdateProductRequest UpdateValidRequest() => new(
        Name: "منتج محدث",
        CategoryId: 1,
        Description: "وصف محدث",
        Barcode: null!,
        ReorderLevel: 15.00m,
        IsActive: true);
}
