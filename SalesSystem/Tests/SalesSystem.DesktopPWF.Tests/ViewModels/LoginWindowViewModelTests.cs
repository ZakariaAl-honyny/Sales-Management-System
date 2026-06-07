namespace SalesSystem.DesktopPWF.Tests.ViewModels;

using System.ComponentModel;
using System.Windows.Input;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Models;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// Tests for LoginWindowViewModel
/// </summary>
public class LoginWindowViewModelTests
{
    private readonly Mock<IAuthApiService> _mockAuthService;
    private readonly Mock<ISessionService> _mockSessionService;
    private readonly Mock<IScreenWindowService> _mockScreenWindowService;
    private readonly LoginWindowViewModel _viewModel;

    public LoginWindowViewModelTests()
    {
        _mockAuthService = new Mock<IAuthApiService>();
        _mockSessionService = new Mock<ISessionService>();
        _mockScreenWindowService = new Mock<IScreenWindowService>();

        _viewModel = new LoginWindowViewModel(
            _mockAuthService.Object,
            _mockSessionService.Object,
            _mockScreenWindowService.Object);
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
    public void IsBusy_DefaultValue_IsFalse()
    {
        _viewModel.IsBusy.Should().BeFalse();
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
    public void IsBusy_IsReadOnly_FromViewModelBase()
    {
        // IsBusy has protected set in ViewModelBase, managed by ExecuteAsync
        // Verify it's false by default (no async operation running)
        _viewModel.IsBusy.Should().BeFalse();
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
    public void LoginCommand_AlwaysEnabled_WhenUsernameEmpty()
    {
        // Per RULE-059: buttons are NEVER disabled via CanExecute predicate
        _viewModel.Username = "";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void LoginCommand_AlwaysEnabled_WhenPasswordEmpty()
    {
        // Per RULE-059: buttons are NEVER disabled via CanExecute predicate
        _viewModel.Username = "admin";
        _viewModel.Password = "";

        _viewModel.LoginCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task LoginCommand_CannotExecute_WhileExecuting()
    {
        var tcs = new TaskCompletionSource<LoginResult>();
        _mockAuthService
            .Setup(s => s.LoginWithDetailsAsync(It.IsAny<LoginRequest>()))
            .Returns(tcs.Task);

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(50);

        _viewModel.LoginCommand.CanExecute(null).Should().BeFalse();

        tcs.SetResult(new LoginResult
        {
            IsSuccess = true,
            Response = new LoginResponse(
                UserId: 1,
                UserName: "admin",
                FullName: "Admin",
                Role: (byte)UserRole.Admin,
                Token: "token",
                ExpiresAt: DateTime.UtcNow.AddHours(8))
        });

        await tcs.Task;
    }

    [Fact]
    public void LoginCommand_CanExecute_WhenValidCredentials()
    {
        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void LoginCommand_AlwaysEnabled_WhenUsernameWhitespace()
    {
        // Per RULE-059: buttons are NEVER disabled via CanExecute predicate
        _viewModel.Username = "   ";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void LoginCommand_AlwaysEnabled_WhenPasswordWhitespace()
    {
        // Per RULE-059: buttons are NEVER disabled via CanExecute predicate
        _viewModel.Username = "admin";
        _viewModel.Password = "   ";

        _viewModel.LoginCommand.CanExecute(null).Should().BeTrue();
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
            .Setup(s => s.LoginWithDetailsAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(new LoginResult
            {
                IsSuccess = true,
                Response = loginResponse
            });

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
            .Setup(s => s.LoginWithDetailsAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(new LoginResult
            {
                IsSuccess = false,
                Error = "اسم المستخدم أو كلمة المرور غير صحيحة",
                ErrorCode = "Unknown"
            });

        _viewModel.Username = "admin";
        _viewModel.Password = "wrong";

        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.ErrorMessage.Should().Be("اسم المستخدم أو كلمة المرور غير صحيحة");
    }

    [Fact]
    public async Task LoginCommand_WhenExecuting_SetsIsBusyTrue()
    {
        var tcs = new TaskCompletionSource<LoginResult>();
        _mockAuthService
            .Setup(s => s.LoginWithDetailsAsync(It.IsAny<LoginRequest>()))
            .Returns(tcs.Task);

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.Execute(null);
        _viewModel.IsBusy.Should().BeTrue();

        tcs.SetResult(new LoginResult
        {
            IsSuccess = true,
            Response = new LoginResponse(
                UserId: 1,
                UserName: "admin",
                FullName: "Admin",
                Role: (byte)UserRole.Admin,
                Token: "token",
                ExpiresAt: DateTime.UtcNow.AddHours(8))
        });

        await tcs.Task;

        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoginCommand_ClearsErrorMessage_OnNewAttempt()
    {
        // Arrange: Set a previous error message and delay the service response
        _viewModel.ErrorMessage = "خطأ سابق";

        var tcs = new TaskCompletionSource<LoginResult>();
        _mockAuthService
            .Setup(s => s.LoginWithDetailsAsync(It.IsAny<LoginRequest>()))
            .Returns(tcs.Task);

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        // Act: Execute the command (ErrorMessage should be cleared at start of LoginOperationAsync)
        _viewModel.LoginCommand.Execute(null);

        // Give time for the operation to start and clear ErrorMessage
        await Task.Delay(10);

        // Assert: ErrorMessage was cleared by LoginOperationAsync before awaiting the service
        _viewModel.ErrorMessage.Should().BeNull();
        _viewModel.IsBusy.Should().BeTrue();

        // Cleanup: complete the pending operation
        tcs.SetResult(new LoginResult
        {
            IsSuccess = true,
            Response = new LoginResponse(
                UserId: 1,
                UserName: "admin",
                FullName: "Admin",
                Role: (byte)UserRole.Admin,
                Token: "token",
                ExpiresAt: DateTime.UtcNow.AddHours(8))
        });

        await Task.Delay(50);
    }

    [Fact]
    public async Task LoginCommand_WhenExceptionThrown_SetsErrorMessage()
    {
        _mockAuthService
            .Setup(s => s.LoginWithDetailsAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new System.Net.Http.HttpRequestException("Network error"));

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.ErrorMessage.Should().NotBeNull();
        _viewModel.ErrorMessage.Should().Contain("فشل");
    }

    [Fact]
    public async Task LoginCommand_WhenExceptionThrown_SetsIsBusyFalse()
    {
        _mockAuthService
            .Setup(s => s.LoginWithDetailsAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new Exception("Unknown error"));

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoginCommand_WithMustChangePassword_StoresSessionAndShowsModal()
    {
        // When MustChangePassword is true, session is stored but LoginWindow stays open.
        // MainWindow navigation and LoginWindow close happen only after password change completes.
        var loginResponse = new LoginResponse(
            UserId: 1,
            UserName: "user1",
            FullName: "User One",
            Role: (byte)UserRole.Cashier,
            Token: "change-password-token",
            ExpiresAt: DateTime.UtcNow.AddHours(8),
            MustChangePassword: true);

        _mockAuthService
            .Setup(s => s.LoginWithDetailsAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(new LoginResult
            {
                IsSuccess = true,
                Response = loginResponse
            });

        _viewModel.Username = "user1";
        _viewModel.Password = "oldpass";

        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        // Session must be stored immediately when MustChangePassword is true
        _mockSessionService.Verify(
            s => s.SetSession(
                "change-password-token",
                "user1",
                1,
                UserRole.Cashier),
            Times.Once);
    }

    [Fact]
    public async Task LoginCommand_WithRequiresPasswordSetup_ShowsErrorWhenNoUserId()
    {
        // When RequiresPasswordSetup is returned without a userId, show an error
        _mockAuthService
            .Setup(s => s.LoginWithDetailsAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(new LoginResult
            {
                IsSuccess = false,
                Error = "يجب تعيين كلمة المرور أولاً",
                ErrorCode = ErrorCodes.RequiresPasswordSetup,
                RequiresPasswordSetupUserId = null
            });

        _viewModel.Username = "newuser";
        _viewModel.Password = "";

        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.ErrorMessage.Should().Contain("لم يتم العثور على المستخدم");
    }

    #endregion

    #region Command CanExecute Notification Tests

    [Fact]
    public void Username_Set_RaisesPropertyChanged()
    {
        // Per RULE-059: No CanExecute predicate — commands are always enabled.
        // PropertyChanged fires via SetProperty in the Username setter.
        var propertyChanged = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Username))
                propertyChanged = true;
        };

        _viewModel.Username = "admin";

        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void Password_Set_RaisesPropertyChanged()
    {
        // Per RULE-059: No CanExecute predicate — commands are always enabled.
        // PropertyChanged fires via SetProperty in the Password setter.
        var propertyChanged = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Password))
                propertyChanged = true;
        };

        _viewModel.Password = "password";

        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public async Task LoginCommand_RaisesCanExecuteChanged_WhenExecuting()
    {
        var canExecuteChangedCount = 0;
        _viewModel.LoginCommand.CanExecuteChanged += (s, e) => canExecuteChangedCount++;

        var tcs = new TaskCompletionSource<LoginResult>();
        _mockAuthService
            .Setup(s => s.LoginWithDetailsAsync(It.IsAny<LoginRequest>()))
            .Returns(tcs.Task);

        _viewModel.Username = "admin";
        _viewModel.Password = "password";

        _viewModel.LoginCommand.Execute(null);
        await Task.Delay(50);

        canExecuteChangedCount.Should().BeGreaterThan(0, "CanExecuteChanged should fire when command starts executing");

        tcs.SetResult(new LoginResult
        {
            IsSuccess = true,
            Response = new LoginResponse(
                UserId: 1,
                UserName: "admin",
                FullName: "Admin",
                Role: (byte)UserRole.Admin,
                Token: "token",
                ExpiresAt: DateTime.UtcNow.AddHours(8))
        });

        await tcs.Task;
    }

    #endregion
}
