using System.Windows;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Models;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.ViewModels.Users;

namespace SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// ViewModel for LoginWindow (standalone window).
/// Handles normal login, MustChangePassword flow, and RequiresPasswordSetup (first login) flow.
/// </summary>
public class LoginWindowViewModel : ViewModelBase
{
    private readonly IAuthApiService _authService;
    private readonly ISessionService _sessionService;
    private readonly IScreenWindowService _screenWindowService;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _rememberMe;

    public LoginWindowViewModel(
        IAuthApiService authService,
        ISessionService sessionService,
        IScreenWindowService screenWindowService)
    {
        _authService = authService;
        _sessionService = sessionService;
        _screenWindowService = screenWindowService;

        LoginCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoginOperationAsync, ex => ErrorMessage = HandleException(ex, "LoginWindowViewModel.LoginAsync"), "جاري تسجيل الدخول...")));
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public AsyncRelayCommand LoginCommand { get; }

    /// <summary>
    /// Main login operation. Handles three cases:
    /// 1. Normal login success → navigate to MainWindow
    /// 2. MustChangePassword = true → open PasswordChangeView, then navigate
    /// 3. RequiresPasswordSetup → open SetPasswordView, then re-attempt login
    /// </summary>
    private async Task LoginOperationAsync()
    {
        ErrorMessage = null;

        var request = new LoginRequest(Username, Password);
        var result = await _authService.LoginWithDetailsAsync(request);

        if (result.IsSuccess)
        {
            var response = result.Response!;

            // Flow B: Login succeeded but user must change password
            if (response.MustChangePassword)
            {
                await HandleMustChangePasswordFlowAsync(response);
                return;
            }

            // Normal login success
            CompleteLogin(response);
        }
        else
        {
            // Flow A: User has no password (PasswordHash == null)
            if (result.ErrorCode == ErrorCodes.RequiresPasswordSetup)
            {
                var passwordSet = await ShowSetPasswordModalAsync(result.RequiresPasswordSetupUserId, result.PasswordResetToken);
                if (passwordSet)
                {
                    // Re-attempt login — the password was just set
                    await LoginOperationAsync();
                }
                return;
            }

            ErrorMessage = HandleFailure(result.Error ?? "اسم المستخدم أو كلمة المرور غير صحيحة", "LoginWindowViewModel.LoginAsync");
        }
    }

    /// <summary>
    /// Flow B: Login succeeded but MustChangePassword is true.
    /// Store session then show PasswordChangeView modally via IScreenWindowService.
    /// The password change is MANDATORY — user CANNOT close/escape without changing.
    /// On success: navigate to MainWindow.
    /// </summary>
    private async Task HandleMustChangePasswordFlowAsync(LoginResponse response)
    {
        // Store session temporarily
        _sessionService.SetSession(
            response.Token,
            response.UserName,
            response.UserId,
            (UserRole)response.Role);

        var passwordChangeVm = new PasswordChangeViewModel(
            App.GetService<IAuthApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IToastNotificationService>(),
            isMandatory: true);

        var tcs = new TaskCompletionSource<bool>();

        _screenWindowService.OpenScreen(passwordChangeVm, new ScreenWindowOptions
        {
            Title = "تغيير كلمة المرور — مطلوب قبل المتابعة",
            Width = 420,
            Height = 380,
            IsModal = true,
            CanResize = false,
            Style = WindowStyle.None,
            PreventClose = true,
            OnClosed = (_) =>
            {
                tcs.TrySetResult(passwordChangeVm.DialogResult);
                passwordChangeVm.Cleanup();
            }
        });

        var passwordChanged = await tcs.Task;

        if (passwordChanged)
        {
            // Navigate to MainWindow (session already stored)
            NavigateToMainWindow();
        }
        else
        {
            // User somehow exited without changing — clear session and stay on login
            _sessionService.ClearSession();
        }
    }

    /// <summary>
    /// Flow A: Shows SetPasswordView modally via IScreenWindowService.
    /// Returns true if the user successfully set their initial password.
    /// </summary>
    private async Task<bool> ShowSetPasswordModalAsync(int? userId, string? passwordResetToken)
    {
        if (userId == null || userId.Value <= 0)
        {
            ErrorMessage = "لم يتم العثور على المستخدم. يرجى مراجعة المسؤول.";
            return false;
        }

        var setPasswordVm = new SetPasswordViewModel(
            App.GetService<IAuthApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IToastNotificationService>(),
            userId.Value,
            Username,
            passwordResetToken: passwordResetToken ?? string.Empty,
            isFirstLogin: true);

        var tcs = new TaskCompletionSource<bool>();

        _screenWindowService.OpenScreen(setPasswordVm, new ScreenWindowOptions
        {
            Title = "تعيين كلمة المرور",
            Width = 420,
            Height = 400,
            IsModal = true,
            CanResize = false,
            OnClosed = (_) =>
            {
                tcs.TrySetResult(setPasswordVm.DialogResult);
                setPasswordVm.Cleanup();
            }
        });

        return await tcs.Task;
    }

    /// <summary>
    /// Completes the login: stores session, opens MainWindow, closes LoginWindow.
    /// </summary>
    private void CompleteLogin(LoginResponse response)
    {
        _sessionService.SetSession(
            response.Token,
            response.UserName,
            response.UserId,
            (UserRole)response.Role);

        NavigateToMainWindow();
    }

    /// <summary>
    /// Opens MainWindow and closes the LoginWindow.
    /// </summary>
    private static void NavigateToMainWindow()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            System.Windows.Application.Current.MainWindow = mainWindow;

            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window is LoginWindow)
                {
                    window.Close();
                    break;
                }
            }
        });
    }
}
