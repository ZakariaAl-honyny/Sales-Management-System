namespace SalesSystem.DesktopPWF.Tests.ViewModels;

using System.ComponentModel;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.ViewModels;
using SalesSystem.DesktopPWF.Views;

/// <summary>
/// Tests for LoginViewModel
/// </summary>
public class LoginViewModelTests
{
    private readonly Mock<IAuthApiService> _mockAuthService;
    private readonly Mock<ISessionService> _mockSessionService;
    private readonly Mock<INavigationService> _mockNavigationService;
    private readonly LoginViewModel _viewModel;

    public LoginViewModelTests()
    {
        _mockAuthService = new Mock<IAuthApiService>();
        _mockSessionService = new Mock<ISessionService>();
        _mockNavigationService = new Mock<INavigationService>();

        _viewModel = new LoginViewModel(
            _mockAuthService.Object,
            _mockSessionService.Object,
            _mockNavigationService.Object);
    }

    #region Property Tests

    [Fact]
    public void Username_DefaultValue_IsEmpty()
    {
        _viewModel.Username.Should().BeEmpty();
    }

    [Fact]
    public void Password_DefaultValue_IsEmpty()
    {
        _viewModel.Password.Should().BeEmpty();
    }

    [Fact]
    public void IsLoading_DefaultValue_IsFalse()
    {
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void ErrorMessage_DefaultValue_IsNull()
    {
        _viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void RememberMe_DefaultValue_IsFalse()
    {
        _viewModel.RememberMe.Should().BeFalse();
    }

    #endregion

    #region PropertyChangeNotification Tests

    [Fact]
    public void Username_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.Username = "admin";

        propertyChangedEvents.Should().Contain("Username");
    }

    [Fact]
    public void Password_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.Password = "password123";

        propertyChangedEvents.Should().Contain("Password");
    }

    [Fact]
    public void IsLoading_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.IsLoading = true;

        propertyChangedEvents.Should().Contain("IsLoading");
    }

    [Fact]
    public void ErrorMessage_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.ErrorMessage = "خطأ";

        propertyChangedEvents.Should().Contain("ErrorMessage");
    }

    [Fact]
    public void RememberMe_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.RememberMe = true;

        propertyChangedEvents.Should().Contain("RememberMe");
    }

    #endregion

    #region LoginCommand CanExecute Tests

    [Fact]
    public void LoginCommand_CannotExecute_WhenUsernameEmpty()
    {
        _viewModel.Username = "";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void LoginCommand_CannotExecute_WhenPasswordEmpty()
    {
        _viewModel.Username = "admin";
        _viewModel.Password = "";

        _viewModel.LoginCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void LoginCommand_CannotExecute_WhenLoading()
    {
        _viewModel.Username = "admin";
        _viewModel.Password = "password";
        _viewModel.IsLoading = true;

        _viewModel.LoginCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void LoginCommand_CanExecute_WhenValidCredentials()
    {
        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void LoginCommand_CannotExecute_WhenUsernameWhitespace()
    {
        _viewModel.Username = "   ";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void LoginCommand_CannotExecute_WhenPasswordWhitespace()
    {
        _viewModel.Username = "admin";
        _viewModel.Password = "   ";

        _viewModel.LoginCommand.CanExecute(null).Should().BeFalse();
    }

    #endregion

    #region LoginCommand Tests

    [Fact]
    public async Task LoginCommand_WhenValidCredentials_CallsService()
    {
        // Arrange
        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        var loginResponse = CreateLoginResponse();
        _mockAuthService
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(Result<LoginResponse>.Success(loginResponse));

        // Act
        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        // Assert
        _mockAuthService.Verify(
            x => x.LoginAsync(It.Is<LoginRequest>(r => r.UserName == "admin" && r.Password == "password")),
            Times.Once);
    }

    [Fact]
    public async Task LoginCommand_WhenSuccessful_SetsSession()
    {
        // Arrange
        var loginResponse = CreateLoginResponse();
        _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(Result<LoginResponse>.Success(loginResponse));

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        // Act
        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        // Assert
        _mockSessionService.Verify(
            s => s.SetSession(
                loginResponse.Token,
                loginResponse.UserName,
                loginResponse.UserId,
                UserRole.Admin),
            Times.Once);
    }

    [Fact]
    public async Task LoginCommand_WhenFailed_SetsErrorMessage()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(Result<LoginResponse>.Failure("اسم المستخدم أو كلمة المرور غير صحيحة"));

        _viewModel.Username = "admin";
        _viewModel.Password = "wrong";

        // Act
        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        // Assert
        _viewModel.ErrorMessage.Should().Be("اسم المستخدم أو كلمة المرور غير صحيحة");
    }

    [Fact]
    public async Task LoginCommand_WhenLoading_SetsIsLoadingTrue()
    {
        // Arrange
        var tcs = new TaskCompletionSource<Result<LoginResponse>>();
        _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .Returns(tcs.Task);

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        // Act
        _viewModel.LoginCommand.Execute(null);

        // Assert - IsLoading should be true during execution
        _viewModel.IsLoading.Should().BeTrue();

        // Complete the task
        tcs.SetResult(Result<LoginResponse>.Success(CreateLoginResponse()));
        await Task.Delay(100);

        // Assert - IsLoading should be false after completion
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoginCommand_ClearsErrorMessage_OnNewAttempt()
    {
        // Arrange
        _viewModel.ErrorMessage = "خطأ سابق";

        var tcs = new TaskCompletionSource<Result<LoginResponse>>();
        _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .Returns(tcs.Task);

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        // Act
        _viewModel.LoginCommand.Execute(null);
        
        // Give it a tiny bit of time to reach the first await
        await Task.Delay(10);

        // Assert
        _viewModel.ErrorMessage.Should().BeNull();
        _viewModel.IsLoading.Should().BeTrue();

        // Cleanup
        tcs.SetResult(Result<LoginResponse>.Success(CreateLoginResponse()));
        await Task.Delay(50);
    }

    [Fact]
    public async Task LoginCommand_WhenExceptionThrown_SetsErrorMessage()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new Exception("Network error"));

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        // Act
        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        // Assert
        _viewModel.ErrorMessage.Should().NotBeNull();
        _viewModel.ErrorMessage.Should().Contain("خطأ");
    }

    [Fact]
    public async Task LoginCommand_WhenExceptionThrown_SetsIsLoadingFalse()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new Exception("Unknown error"));

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        // Act
        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        // Assert
        _viewModel.IsLoading.Should().BeFalse();
    }

    #endregion

    #region Command CanExecute Notification Tests

    [Fact]
    public void LoginCommand_RaisesCanExecuteChanged_WhenUsernameChanges()
    {
        var canExecuteChanged = false;
        _viewModel.LoginCommand.CanExecuteChanged += (s, e) => canExecuteChanged = true;

        _viewModel.Username = "admin";

        canExecuteChanged.Should().BeTrue();
    }

    [Fact]
    public void LoginCommand_RaisesCanExecuteChanged_WhenPasswordChanges()
    {
        var canExecuteChanged = false;
        _viewModel.LoginCommand.CanExecuteChanged += (s, e) => canExecuteChanged = true;

        _viewModel.Password = "password";

        canExecuteChanged.Should().BeTrue();
    }

    [Fact]
    public void LoginCommand_RaisesCanExecuteChanged_WhenIsLoadingChanges()
    {
        var canExecuteChanged = false;
        _viewModel.LoginCommand.CanExecuteChanged += (s, e) => canExecuteChanged = true;

        _viewModel.IsLoading = true;

        canExecuteChanged.Should().BeTrue();
    }

    #endregion

    #region Request Creation Tests

    [Fact]
    public async Task LoginCommand_SendsCorrectRequest_ToService()
    {
        // Arrange
        _viewModel.Username = "testuser";
        _viewModel.Password = "testpass123";

        LoginRequest? capturedRequest = null;
        _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .Callback<LoginRequest>(r => capturedRequest = r)
            .ReturnsAsync(Result<LoginResponse>.Success(CreateLoginResponse()));

        // Act
        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserName.Should().Be("testuser");
        capturedRequest.Password.Should().Be("testpass123");
    }

    #endregion

    private static LoginResponse CreateLoginResponse()
    {
        return new LoginResponse(
            UserId: 1,
            UserName: "admin",
            FullName: "المدير العام",
            Role: (byte)UserRole.Admin,
            Token: "test-token-123",
            ExpiresAt: DateTime.UtcNow.AddHours(8));
    }
}