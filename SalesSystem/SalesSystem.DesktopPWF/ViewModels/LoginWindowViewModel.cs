using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// ViewModel for LoginWindow (standalone window)
/// </summary>
public class LoginWindowViewModel : ViewModelBase
{
    private readonly IAuthApiService _authService;
    private readonly ISessionService _sessionService;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _rememberMe;
    private bool _isLoading;
    private string? _errorMessage;

    public LoginWindowViewModel(IAuthApiService authService, ISessionService sessionService)
    {
        _authService = authService;
        _sessionService = sessionService;

        LoginCommand = new AsyncRelayCommand(LoginAsync, () => !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password));
    }

    public string Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
            {
                ((AsyncRelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ((AsyncRelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                ((AsyncRelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public AsyncRelayCommand LoginCommand { get; }

    private async Task LoginAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var request = new LoginRequest(Username, Password);
            var result = await _authService.LoginAsync(request);

            if (result.IsSuccess)
            {
                var response = result.Value;

                // Store session
                _sessionService.SetSession(
                    response!.Token,
                    response.UserName,
                    response.UserId,
                    (UserRole)response.Role);

                // Open main window and close login
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    System.Windows.Application.Current.MainWindow = mainWindow;

                    // Close this login window
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
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "اسم المستخدم أو كلمة المرور غير صحيحة", "LoginWindowViewModel.LoginAsync");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "LoginWindowViewModel.LoginAsync", $"[LoginWindowViewModel.LoginAsync] Unexpected error during login process for user {Username}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
