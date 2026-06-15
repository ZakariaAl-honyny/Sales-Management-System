using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SalesSystem.Api.Controllers;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _uowMock = new Mock<IUnitOfWork>();
        _controller = new UsersController(_userServiceMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task GivenAdminRole_WhenGetAll_ThenReturnsUserList()
    {
        var users = new List<UserDto>
        {
            new(1, "admin", "المسؤول", 1, 1, false, null, null, null, null, null, 0, null),
            new(2, "manager", "المدير", 2, 1, false, null, null, null, null, null, 0, null),
            new(3, "cashier", "الكاشير", 3, 1, false, null, null, null, null, null, 0, null)
        };

        _userServiceMock
            .Setup(x => x.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<UserDto>>.Success(users));

        var result = await _controller.GetAll(false, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<List<UserDto>>().Subject;

        response.Should().HaveCount(3);
        response[0].UserName.Should().Be("admin");
    }

    [Fact]
    public async Task GivenNoUsers_WhenGetAll_ThenReturnsEmptyList()
    {
        var users = new List<UserDto>();

        _userServiceMock
            .Setup(x => x.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<UserDto>>.Success(users));

        var result = await _controller.GetAll(false, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<List<UserDto>>().Subject;

        response.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenServiceThrowsException_WhenGetAll_ThenReturnsBadRequest()
    {
        _userServiceMock
            .Setup(x => x.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<UserDto>>.Failure("فشل في جلب المستخدمين", "ServerError"));

        var result = await _controller.GetAll(false, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GivenValidUserId_WhenGetById_ThenReturnsUser()
    {
        var user = new UserDto(1, "admin", "المسؤول", 1, 1, false, null, null, null, null, null, 0, null);

        _userServiceMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(user));

        var result = await _controller.GetById(1, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserDto>().Subject;

        response.Id.Should().Be(1);
        response.UserName.Should().Be("admin");
    }

    [Fact]
    public async Task GivenNonExistentUserId_WhenGetById_ThenReturnsNotFound()
    {
        _userServiceMock
            .Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("المستخدم غير موجود", "NotFound"));

        var result = await _controller.GetById(999, CancellationToken.None);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GivenZeroUserId_WhenGetById_ThenReturnsNotFound()
    {
        _userServiceMock
            .Setup(x => x.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("المستخدم غير موجود", "NotFound"));

        var result = await _controller.GetById(0, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GivenValidCreateRequest_WhenCreate_ThenReturnsCreatedUser()
    {
        var request = new CreateUserRequest("newuser", "مستخدم جديد", 2);
        var createdUser = new UserDto(4, "newuser", "مستخدم جديد", 2, 1, false, null, null, null, null, null, 0, null);

        _userServiceMock
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(createdUser));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<UserDto>().Subject;

        response.Id.Should().Be(4);
        response.UserName.Should().Be("newuser");
    }

    [Fact]
    public async Task GivenDuplicateUsername_WhenCreate_ThenReturnsBadRequest()
    {
        var request = new CreateUserRequest("existinguser", "مستخدم موجود", 2);

        _userServiceMock
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("اسم المستخدم موجود مسبقا", "DuplicateUserName"));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GivenInvalidRole_WhenCreate_ThenReturnsBadRequest()
    {
        var request = new CreateUserRequest("newuser", "مستخدم جديد", 99);

        _userServiceMock
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("الدور غير صالح", "InvalidRole"));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GivenEmptyUsername_WhenCreate_ThenReturnsBadRequest()
    {
        var request = new CreateUserRequest("", "مستخدم جديد", 2);

        _userServiceMock
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("اسم المستخدم مطلوب", "ValidationError"));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GivenWeakPassword_WhenCreate_ThenReturnsBadRequest()
    {
        var request = new CreateUserRequest("newuser", "مستخدم جديد", 2);

        _userServiceMock
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("كلمة المرور ضعيفة جداً", "WeakPassword"));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GivenValidUpdateRequest_WhenUpdate_ThenReturnsUpdatedUser()
    {
        var request = new UpdateUserRequest("اسم محدث", 2, 1, null);
        var updatedUser = new UserDto(1, "admin", "اسم محدث", 2, 1, false, null, null, null, null, null, 0, null);

        _userServiceMock
            .Setup(x => x.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(updatedUser));

        var result = await _controller.Update(1, request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserDto>().Subject;

        response.FullName.Should().Be("اسم محدث");
    }

    [Fact]
    public async Task GivenNonExistentUser_WhenUpdate_ThenReturnsNotFound()
    {
        var request = new UpdateUserRequest("اسم محدث", 2, 1, null);

        _userServiceMock
            .Setup(x => x.UpdateAsync(999, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure("المستخدم غير موجود", "NotFound"));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GivenPasswordUpdate_WhenUpdate_ThenReturnsUpdatedUser()
    {
        var request = new UpdateUserRequest("المسؤول", 1, 1, "newpassword123");
        var updatedUser = new UserDto(1, "admin", "المسؤول", 1, 1, false, null, null, null, null, null, 0, null);

        _userServiceMock
            .Setup(x => x.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(updatedUser));

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GivenValidUserId_WhenDelete_ThenReturnsSuccessMessage()
    {
        _userServiceMock
            .Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Delete(1, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GivenNonExistentUser_WhenDelete_ThenReturnsBadRequest()
    {
        _userServiceMock
            .Setup(x => x.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("المستخدم غير موجود", "NotFound"));

        var result = await _controller.Delete(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GivenUserWithRelatedInvoices_WhenDelete_ThenReturnsBadRequest()
    {
        _userServiceMock
            .Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("لا يمكن حذف المستخدم لوجود فواتير مرتبطة", "HasRelatedInvoices"));

        var result = await _controller.Delete(1, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GivenLastAdminUser_WhenDelete_ThenReturnsBadRequest()
    {
        _userServiceMock
            .Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("لا يمكن حذف آخر مستخدم مدير", "LastAdmin"));

        var result = await _controller.Delete(1, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GivenZeroUserId_WhenDelete_ThenReturnsBadRequest()
    {
        _userServiceMock
            .Setup(x => x.DeleteAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("معرف المستخدم غير صالح", "InvalidId"));

        var result = await _controller.Delete(0, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GivenValidUserId_WhenPermanentDelete_ThenReturnsSuccessMessage()
    {
        _userServiceMock
            .Setup(x => x.PermanentDeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.PermanentDelete(1, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GivenNonExistentUser_WhenPermanentDelete_ThenReturnsBadRequest()
    {
        _userServiceMock
            .Setup(x => x.PermanentDeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("المستخدم غير موجود", "NotFound"));

        var result = await _controller.PermanentDelete(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void GivenUsersController_ThenRequiresAdminOnlyAuthorization()
    {
        var controllerType = typeof(UsersController);
        var authorizeAttribute = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .FirstOrDefault() as AuthorizeAttribute;

        authorizeAttribute.Should().NotBeNull("UsersController should have [Authorize] attribute");
        authorizeAttribute!.Policy.Should().Be("AdminOnly");
    }
}
