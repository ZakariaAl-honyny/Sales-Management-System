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
    private bool _isLoading;
    private string? _errorMessage;

    public LoginViewModel(
        IAuthApiService authService, 
        ISessionService sessionService, 
        INavigationService navigationService)
    {
        _authService = authService;
        _sessionService = sessionService;
        _navigationService = navigationService;

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

    public AsyncRelayCommand LoginCommand {
get;
}

    private async Task LoginAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var request = new LoginRequest(Username, Password);
            var result = await _authService.LoginAsync(request);

            if (result.IsSuccess && result.Value != null)
            {
                var response = result.Value;
                
                // Store session
                _sessionService.SetSession(
                    response.Token,
                    response.UserName,
                    response.UserId,
                    (UserRole)response.Role);

                // Navigate to main window
                InvokeOnUIThread(() =>
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
                });
            }
            else
            {
                ErrorMessage = result.Error ?? "اسم المستخدم أو كلمة المرور غير صحيحة";
            }
        }
        catch (System.Net.Http.HttpRequestException)
        {
            ErrorMessage = "فشل الاتصال بالخادم. تأكد من تشغيل خدمة الـ API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ غير متوقع: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

