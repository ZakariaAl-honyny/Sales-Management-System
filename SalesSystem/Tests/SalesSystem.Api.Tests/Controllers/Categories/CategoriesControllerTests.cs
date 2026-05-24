using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers.Categories;

public class CategoriesControllerTests : ControllerTestBase
{
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _controller = new CategoriesController(CategoryServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        var categories = new PagedResult<CategoryDto>
        {
            Items = new List<CategoryDto> { CreateCategoryDto(1), CreateCategoryDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        CategoryServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(categories));

        var result = await _controller.GetAll(null, 1, 10, false, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        CategoryServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<CategoryDto>>("فشل في استرجاع التصنيفات"));

        var result = await _controller.GetAll(null, 1, 10, false, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenCategoryExists_ReturnsOkWithCategory()
    {
        var category = CreateCategoryDto(1);
        CategoryServiceMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(category));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenCategoryNotFound_ReturnsNotFound()
    {
        CategoryServiceMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<CategoryDto>("التصنيف غير موجود"));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreateCategoryRequest("إلكترونيات", "أجهزة إلكترونية");
        var createdCategory = CreateCategoryDto(1);
        CategoryServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdCategory));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        var request = new CreateCategoryRequest("إلكترونيات", "أجهزة إلكترونية");
        CategoryServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<CategoryDto>("اسم التصنيف موجود مسبقاً"));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedCategory()
    {
        var request = new UpdateCategoryRequest("إلكترونيات محدث", "وصف محدث", true);
        var updatedCategory = CreateCategoryDto(1);
        CategoryServiceMock.Setup(x => x.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(updatedCategory));

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenCategoryNotFound_ReturnsBadRequest()
    {
        var request = new UpdateCategoryRequest("إلكترونيات", "وصف", true);
        CategoryServiceMock.Setup(x => x.UpdateAsync(999, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<CategoryDto>("التصنيف غير موجود"));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenCategoryExists_ReturnsOkWithSuccessMessage()
    {
        CategoryServiceMock.Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        var result = await _controller.Delete(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenCategoryNotFound_ReturnsBadRequest()
    {
        CategoryServiceMock.Setup(x => x.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("التصنيف غير موجود"));

        var result = await _controller.Delete(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PermanentDelete_WhenCategoryExists_ReturnsOkWithSuccessMessage()
    {
        CategoryServiceMock.Setup(x => x.PermanentDeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        var result = await _controller.PermanentDelete(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PermanentDelete_WhenCategoryNotFound_ReturnsBadRequest()
    {
        CategoryServiceMock.Setup(x => x.PermanentDeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("التصنيف غير موجود"));

        var result = await _controller.PermanentDelete(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static CategoryDto CreateCategoryDto(int id) => new(
        Id: id,
        Name: $"تصنيف {id}",
        Description: $"وصف التصنيف {id}",
        IsActive: true);
}
