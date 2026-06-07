using System.Windows;
using SalesSystem.Contracts;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.ViewModels;

namespace SalesSystem.DesktopPWF.Views;

/// <summary>
/// ViewModel for Login page
/// </summary>
public class LoginViewModel : ViewModelBase
{
    private readonly IAuthApiService _authService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _rememberMe;
    private string? _errorMessage;

    public LoginViewModel(
        IAuthApiService authService, 
        ISessionService sessionService, 
        INavigationService navigationService)
    {
        _authService = authService;
        _sessionService = sessionService;
        _navigationService = navigationService;

        LoginCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoginOperationAsync)));
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

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public AsyncRelayCommand LoginCommand { get; }

    private async Task LoginOperationAsync()
    {
        ErrorMessage = null;

        var request = new LoginRequest(Username, Password);
        var result = await _authService.LoginAsync(request);

        if (result.IsSuccess && result.Value != null)
        {
            var response = result.Value;
            
            // Check MustChangePassword in login response to redirect to SetPassword screen
            if (response.MustChangePassword)
            {
                ErrorMessage = "يجب تغيير كلمة المرور. يرجى الاتصال بمسؤول النظام.";
                LogSystemError("[MustChangePassword] User needs to change password before proceeding",
                    "LoginViewModel.LoginOperationAsync");
                return;
            }

            // Store session
            _sessionService.SetSession(
                response.Token,
                response.UserName,
                response.UserId,
                (UserRole)response.Role);

            // Navigate to main window
            await InvokeOnUIThreadAsync(() =>
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                
                // Close login window
                if (System.Windows.Application.Current?.MainWindow != null)
                {
                    System.Windows.Application.Current.MainWindow.Close();
                }
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.MainWindow = mainWindow;
                }
                return Task.CompletedTask;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "اسم المستخدم أو كلمة المرور غير صحيحة", "LoginViewModel.LoginAsync");
        }
    }
}
