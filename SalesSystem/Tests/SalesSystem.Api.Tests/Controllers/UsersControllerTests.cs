using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers;

/// <summary>
/// Unit tests for UsersController
/// </summary>
public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _controller = new UsersController(_userServiceMock.Object);
    }

    #region GetAll Tests

    /// <summary>
    /// Given admin role, when getting all users, then returns user list
    /// </summary>
    [Fact]
    public async Task GivenAdminRole_WhenGetAll_ThenReturnsUserList()
    {
        // Arrange
        var users = new List<UserDto>
        {
            new(1, "admin", "المسؤول", 1),
            new(2, "manager", "المدير", 2),
            new(3, "cashier", "الكاشير", 3)
        };

        _userServiceMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<UserDto>>.Success(users));

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<List<UserDto>>().Subject;
        
        response.Should().HaveCount(3);
        response[0].UserName.Should().Be("admin");
    }

    /// <summary>
    /// Given no users exist, when getting all users, then returns empty list
    /// </summary>
    [Fact]
    public async Task GivenNoUsers_WhenGetAll_ThenReturnsEmptyList()
    {
        // Arrange
        var users = new List<UserDto>();

        _userServiceMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<UserDto>>.Success(users));

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<List<UserDto>>().Subject;
        
        response.Should().BeEmpty();
    }

    /// <summary>
    /// Given service throws exception, when getting all users, then handles error
    /// </summary>
    [Fact]
    public async Task GivenServiceThrowsException_WhenGetAll_ThenReturnsBadRequest()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<UserDto>>.Failure("فشل في جلب المستخدمين", "ServerError"));

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetById Tests

    /// <summary>
    /// Given valid user id, when getting user by id, then returns user
    /// </summary>
    [Fact]
    public async Task GivenValidUserId_WhenGetById_ThenReturnsUser()
    {
        // Arrange
        var user = new UserDto(1, "admin", "المسؤول", 1);

        _userServiceMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(user));

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserDto>().Subject;
        
        response.Id.Should().Be(1);
        response.UserName.Should().Be("admin");
    }

    /// <summary>
    /// Given non-existent user id, when getting user by id, then returns NotFound
    /// </summary>
    [Fact]
    public async Task GivenNonExistentUserId_WhenGetById_ThenReturnsNotFound()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("المستخدم غير موجود", "NotFound"));

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Given zero user id, when getting user by id, then returns NotFound
    /// </summary>
    [Fact]
    public async Task GivenZeroUserId_WhenGetById_ThenReturnsNotFound()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("المستخدم غير موجود", "NotFound"));

        // Act
        var result = await _controller.GetById(0, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    /// <summary>
    /// Given valid create request, when creating user, then returns created user
    /// </summary>
    [Fact]
    public async Task GivenValidCreateRequest_WhenCreate_ThenReturnsCreatedUser()
    {
        // Arrange
        var request = new CreateUserRequest("newuser", "password123", "مستخدم جديد", 2);
        var createdUser = new UserDto(4, "newuser", "مستخدم جديد", 2);

        _userServiceMock
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(createdUser));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<UserDto>().Subject;
        
        response.Id.Should().Be(4);
        response.UserName.Should().Be("newuser");
    }

    /// <summary>
    /// Given duplicate username, when creating user, then returns BadRequest
    /// </summary>
    [Fact]
    public async Task GivenDuplicateUsername_WhenCreate_ThenReturnsBadRequest()
    {
        // Arrange
        var request = new CreateUserRequest("existinguser", "password123", "مستخدم موجود", 2);

        _userServiceMock
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("اسم المستخدم موجود مسبقا", "DuplicateUserName"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given invalid role, when creating user, then returns BadRequest
    /// </summary>
    [Fact]
    public async Task GivenInvalidRole_WhenCreate_ThenReturnsBadRequest()
    {
        // Arrange
        var request = new CreateUserRequest("newuser", "password123", "مستخدم جديد", 99);

        _userServiceMock
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("الدور غير صالح", "InvalidRole"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given empty username, when creating user, then returns BadRequest
    /// </summary>
    [Fact]
    public async Task GivenEmptyUsername_WhenCreate_ThenReturnsBadRequest()
    {
        // Arrange
        var request = new CreateUserRequest("", "password123", "مستخدم جديد", 2);

        _userServiceMock
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("اسم المستخدم مطلوب", "ValidationError"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given weak password, when creating user, then returns BadRequest
    /// </summary>
    [Fact]
    public async Task GivenWeakPassword_WhenCreate_ThenReturnsBadRequest()
    {
        // Arrange
        var request = new CreateUserRequest("newuser", "123", "مستخدم جديد", 2);

        _userServiceMock
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("كلمة المرور ضعيفة جداً", "WeakPassword"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    /// <summary>
    /// Given valid update request, when updating user, then returns updated user
    /// </summary>
    [Fact]
    public async Task GivenValidUpdateRequest_WhenUpdate_ThenReturnsUpdatedUser()
    {
        // Arrange
        var request = new UpdateUserRequest("اسم محدث", 2, null);
        var updatedUser = new UserDto(1, "admin", "اسم محدث", 2);

        _userServiceMock
            .Setup(x => x.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(updatedUser));

        // Act
        var result = await _controller.Update(1, request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserDto>().Subject;
        
        response.FullName.Should().Be("اسم محدث");
    }

    /// <summary>
    /// Given non-existent user, when updating user, then returns NotFound
    /// </summary>
    [Fact]
    public async Task GivenNonExistentUser_WhenUpdate_ThenReturnsNotFound()
    {
        // Arrange
        var request = new UpdateUserRequest("اسم محدث", 2, null);

        _userServiceMock
            .Setup(x => x.UpdateAsync(999, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("المستخدم غير موجود", "NotFound"));

        // Act
        var result = await _controller.Update(999, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Given invalid id in request, when updating user, then returns BadRequest
    /// </summary>
    [Fact]
    public async Task GivenInvalidIdMismatch_WhenUpdate_ThenReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateUserRequest("اسم محدث", 2, null);

        _userServiceMock
            .Setup(x => x.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("معرف المستخدم غير صحيح", "InvalidId"));

        // Act
        var result = await _controller.Update(1, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given password update, when updating user, then returns updated user
    /// </summary>
    [Fact]
    public async Task GivenPasswordUpdate_WhenUpdate_ThenReturnsUpdatedUser()
    {
        // Arrange
        var request = new UpdateUserRequest("المسؤول", 1, "newpassword123");
        var updatedUser = new UserDto(1, "admin", "المسؤول", 1);

        _userServiceMock
            .Setup(x => x.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(updatedUser));

        // Act
        var result = await _controller.Update(1, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Delete Tests

    /// <summary>
    /// Given valid user id, when deleting user, then returns success message
    /// </summary>
    [Fact]
    public async Task GivenValidUserId_WhenDelete_ThenReturnsSuccessMessage()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Given non-existent user, when deleting user, then returns NotFound
    /// </summary>
    [Fact]
    public async Task GivenNonExistentUser_WhenDelete_ThenReturnsNotFound()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("المستخدم غير موجود", "NotFound"));

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Given user with related invoices, when deleting user, then returns BadRequest
    /// </summary>
    [Fact]
    public async Task GivenUserWithRelatedInvoices_WhenDelete_ThenReturnsBadRequest()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("لا يمكن حذف المستخدم لوجود فواتير مرتبطة", "HasRelatedInvoices"));

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given last admin user, when deleting user, then returns BadRequest
    /// </summary>
    [Fact]
    public async Task GivenLastAdminUser_WhenDelete_ThenReturnsBadRequest()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("لا يمكن حذف آخر مستخدم مدير", "LastAdmin"));

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Given zero user id, when deleting user, then returns BadRequest
    /// </summary>
    [Fact]
    public async Task GivenZeroUserId_WhenDelete_ThenReturnsBadRequest()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.DeleteAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("معرف المستخدم غير صالح", "InvalidId"));

        // Act
        var result = await _controller.Delete(0, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Authorization Tests

    /// <summary>
    /// Verify UsersController requires AdminOnly authorization
    /// </summary>
    [Fact]
    public void GivenUsersController_ThenRequiresAdminOnlyAuthorization()
    {
        // Assert
        var controllerType = typeof(UsersController);
        var authorizeAttribute = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .FirstOrDefault() as AuthorizeAttribute;
        
        authorizeAttribute.Should().NotBeNull("UsersController should have [Authorize] attribute");
        authorizeAttribute!.Policy.Should().Be("AdminOnly");
    }

    #endregion
}