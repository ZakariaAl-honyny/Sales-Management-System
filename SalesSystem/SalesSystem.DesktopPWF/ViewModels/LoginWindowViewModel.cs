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

    public LoginWindowViewModel(IAuthApiService authService, ISessionService sessionService)
    {
        _authService = authService;
        _sessionService = sessionService;

        LoginCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoginOperationAsync, ex => ErrorMessage = HandleException(ex, "LoginWindowViewModel.LoginAsync"), "جاري تسجيل الدخول...")),
            () => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password));
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

    private string? _errorMessage;
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

        if (result.IsSuccess)
        {
            var response = result.Value;

            _sessionService.SetSession(
                response!.Token,
                response.UserName,
                response.UserId,
                (UserRole)response.Role);

            if (System.Windows.Application.Current != null)
            {
                await InvokeOnUIThreadAsync(async () =>
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
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "اسم المستخدم أو كلمة المرور غير صحيحة", "LoginWindowViewModel.LoginAsync");
        }
    }
}
