namespace SalesSystem.DesktopPWF.Tests.ViewModels;

using System.ComponentModel;
using System.Windows.Input;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// Tests for LoginWindowViewModel
/// </summary>
public class LoginWindowViewModelTests
{
    private readonly Mock<IAuthApiService> _mockAuthService;
    private readonly Mock<ISessionService> _mockSessionService;
    private readonly LoginWindowViewModel _viewModel;

    public LoginWindowViewModelTests()
    {
        _mockAuthService = new Mock<IAuthApiService>();
        _mockSessionService = new Mock<ISessionService>();

        _viewModel = new LoginWindowViewModel(
            _mockAuthService.Object,
            _mockSessionService.Object);
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
    public async Task LoginCommand_WhenSuccessful_SetsSession()
    {
        var loginResponse = new LoginResponse(
            UserId: 1,
            UserName: "admin",
            FullName: "Admin User",
            Role: (byte)UserRole.Admin,
            Token: "test-token-123",
            ExpiresAt: DateTime.UtcNow.AddHours(8));

        _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(Result<LoginResponse>.Success(loginResponse));

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        _mockSessionService.Verify(
            s => s.SetSession(
                "test-token-123",
                "admin",
                1,
                UserRole.Admin),
            Times.Once);
    }

    [Fact]
    public async Task LoginCommand_WhenFailed_SetsErrorMessage()
    {
        _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(Result<LoginResponse>.Failure("اسم المستخدم أو كلمة المرور غير صحيحة"));

        _viewModel.Username = "admin";
        _viewModel.Password = "wrong";

        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.ErrorMessage.Should().Be("اسم المستخدم أو كلمة المرور غير صحيحة");
    }

    [Fact]
    public async Task LoginCommand_WhenLoading_SetsIsLoadingTrue()
    {
        var tcs = new TaskCompletionSource<Result<LoginResponse>>();
        _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .Returns(tcs.Task);

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.Execute(null);
        _viewModel.IsLoading.Should().BeTrue();

        tcs.SetResult(Result<LoginResponse>.Success(new LoginResponse(
            UserId: 1,
            UserName: "admin",
            FullName: "Admin",
            Role: (byte)UserRole.Admin,
            Token: "token",
            ExpiresAt: DateTime.UtcNow.AddHours(8))));

        await tcs.Task;

        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoginCommand_ClearsErrorMessage_OnNewAttempt()
    {
        _viewModel.ErrorMessage = "خطأ سابق";

        _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(Result<LoginResponse>.Success(new LoginResponse(
                UserId: 1,
                UserName: "admin",
                FullName: "Admin",
                Role: (byte)UserRole.Admin,
                Token: "token",
                ExpiresAt: DateTime.UtcNow.AddHours(8))));

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task LoginCommand_WhenExceptionThrown_SetsErrorMessage()
    {
        _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new System.Net.Http.HttpRequestException("Network error"));

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.ErrorMessage.Should().NotBeNull();
        _viewModel.ErrorMessage.Should().Contain("خطأ");
    }

    [Fact]
    public async Task LoginCommand_WhenExceptionThrown_SetsIsLoadingFalse()
    {
        _mockAuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new Exception("Unknown error"));

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

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
}
