using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
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

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithPagedResult()
    {
        // Arrange
        var categories = new PagedResult<CategoryDto>
        {
            Items = new List<CategoryDto> { CreateCategoryDto(1), CreateCategoryDto(2) },
            Page = 1, PageSize = 10, TotalCount = 2
        };

        CategoryServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(categories));

        // Act
        var result = await _controller.GetAll(null, 1, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        CategoryServiceMock.Setup(x => x.GetAllAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<PagedResult<CategoryDto>>("فشل في استرجاع التصنيفات"));

        // Act
        var result = await _controller.GetAll(null, 1, 10);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenCategoryExists_ReturnsOkWithCategory()
    {
        // Arrange
        var category = CreateCategoryDto(1);
        CategoryServiceMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(category));

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenCategoryNotFound_ReturnsNotFound()
    {
        // Arrange
        CategoryServiceMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<CategoryDto>("التصنيف غير موجود"));

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
        var request = new CreateCategoryRequest("إلكترونيات", "أجهزة إلكترونية");
        var createdCategory = CreateCategoryDto(1);
        CategoryServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(createdCategory));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCategoryRequest("إلكترونيات", "أجهزة إلكترونية");
        CategoryServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<CategoryDto>("اسم التصنيف موجود مسبقاً"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedCategory()
    {
        // Arrange
        var request = new UpdateCategoryRequest("إلكترونيات محدث", "وصف محدث", true);
        var updatedCategory = CreateCategoryDto(1);
        CategoryServiceMock.Setup(x => x.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult(updatedCategory));

        // Act
        var result = await _controller.Update(1, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenCategoryNotFound_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateCategoryRequest("إلكترونيات", "وصف", true);
        CategoryServiceMock.Setup(x => x.UpdateAsync(999, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult<CategoryDto>("التصنيف غير موجود"));

        // Act
        var result = await _controller.Update(999, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WhenCategoryExists_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        CategoryServiceMock.Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult());

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenCategoryNotFound_ReturnsBadRequest()
    {
        // Arrange
        CategoryServiceMock.Setup(x => x.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailureResult("التصنيف غير موجود"));

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static CategoryDto CreateCategoryDto(int id) => new(
        Id: id,
        Name: $"تصنيف {id}",
        Description: $"وصف التصنيف {id}",
        IsActive: true);

    #endregion
}