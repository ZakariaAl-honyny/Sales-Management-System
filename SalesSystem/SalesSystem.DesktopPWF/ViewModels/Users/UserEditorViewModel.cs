using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Users;

public class UserEditorViewModel : ViewModelBase
{
    private readonly IUserApiService _userService;
    private readonly IEventBus _eventBus;

    private int _id;
    private string _username = string.Empty;
    private string _fullName = string.Empty;
    private string _password = string.Empty;
    private UserRole _role = UserRole.Cashier;
    private bool _isActive = true;
    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }
    private bool _isLoading;
    private string? _errorMessage;


    public UserEditorViewModel()
    {
        _userService = App.GetService<IUserApiService>();
        _eventBus = App.GetService<IEventBus>();
        _isEditMode = false;
        WindowTitle = "إضافة مستخدم جديد";
        InitializeCommands();
    }

    public UserEditorViewModel(UserDto user) : this()
    {
        _id = user.Id;
        Username = user.UserName;
        FullName = user.FullName;
        Role = (UserRole)user.Role;
        IsActive = user.IsActive;
        _isEditMode = true;
        WindowTitle = $"تعديل مستخدم: {user.UserName}";
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave());
        CancelCommand = new RelayCommand(() => RequestClose());
    }

    #region Properties
    public string WindowTitle { get; }

    public string Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
                (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public string FullName
    {
        get => _fullName;
        set
        {
            if (SetProperty(ref _fullName, value))
                (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
                (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public UserRole Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ObservableCollection<object> RoleOptions { get; } = new()
    {
        new { Value = UserRole.Admin, Display = "مدير نظام" },
        new { Value = UserRole.Manager, Display = "مدير فرع" },
        new { Value = UserRole.Cashier, Display = "كاشير" }
    };
    #endregion

    #region Commands
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    #endregion

    #region Methods
    private bool CanSave()
    {
        if (string.IsNullOrWhiteSpace(Username)) return false;
        if (string.IsNullOrWhiteSpace(FullName)) return false;
        if (!IsEditMode && string.IsNullOrWhiteSpace(Password)) return false;
        return true;
    }

    private async Task SaveAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Result<UserDto> result;

            if (IsEditMode)
            {
                var request = new UpdateUserRequest(FullName, (byte)Role, IsActive, string.IsNullOrWhiteSpace(Password) ? null : Password);
                result = await _userService.UpdateAsync(_id, request);
            }
            else
            {
                var request = new CreateUserRequest(Username, Password, FullName, (byte)Role);
                result = await _userService.CreateAsync(request);
            }

            if (result.IsSuccess && result.Value != null)
            {
                _eventBus.Publish(new UserChangedMessage(result.Value.Id));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ بيانات المستخدم", "UserEditorViewModel.SaveAsync", "[UserEditorViewModel.SaveAsync] Failed to save user data.");
                System.Windows.MessageBox.Show(ErrorMessage, "خطأ في الحفظ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "UserEditorViewModel.SaveAsync", "[UserEditorViewModel.SaveAsync] Failed to save user data.");
            System.Windows.MessageBox.Show(ErrorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
    #endregion
}
