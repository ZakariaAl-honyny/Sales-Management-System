using System.Collections.Generic;
using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Linq;

namespace SalesSystem.DesktopPWF.ViewModels.Users;

public class UserEditorViewModel : ViewModelBase
{
    private readonly IUserApiService _userService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private int _id;
    private string _username = string.Empty;
    private string _fullName = string.Empty;
    private UserRole _role = UserRole.Cashier;
    private bool _isActive = true;
    private bool _isEditMode;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _avatarUrl = string.Empty;
    private int? _defaultCashBoxId;
    private ObservableCollection<CashBoxOptionItem> _cashBoxOptions = new();
    private bool _isLocked;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }
    private string? _errorMessage;

    public UserEditorViewModel()
    {
        _userService = App.GetService<IUserApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        SetDialogService(_dialogService);
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
        IsActive = user.Status == 1;
        Phone = user.Phone ?? string.Empty;
        Email = user.Email ?? string.Empty;
        AvatarUrl = user.AvatarPath ?? string.Empty;
        DefaultCashBoxId = user.DefaultCashBoxId;
        _isEditMode = true;
        _isLocked = user.Status == 2; // Locked status
        WindowTitle = $"تعديل مستخدم: {user.UserName}";
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ المستخدم...")));
        CancelCommand = new RelayCommand(() => RequestClose());
        UploadAvatarCommand = new RelayCommand(UploadAvatar);
        RemoveAvatarCommand = new RelayCommand(RemoveAvatar, () => HasAvatar);
        ChangePasswordCommand = new RelayCommand(OpenChangePassword);
    }

    #region Properties
    public string WindowTitle { get; }

    public string Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Username), "اسم المستخدم مطلوب");
                else
                    ClearErrors(nameof(Username));
            }
        }
    }

    public string FullName
    {
        get => _fullName;
        set
        {
            if (SetProperty(ref _fullName, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(FullName), "الاسم بالكامل مطلوب");
                else
                    ClearErrors(nameof(FullName));
            }
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

    public string Phone
    {
        get => _phone;
        set
        {
            if (SetProperty(ref _phone, value))
            {
                if (!string.IsNullOrWhiteSpace(value) && value.Length > 20)
                    AddError(nameof(Phone), "رقم الهاتف لا يتجاوز 20 رقم");
                else
                    ClearErrors(nameof(Phone));
            }
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                if (!string.IsNullOrWhiteSpace(value) && !IsValidEmail(value))
                    AddError(nameof(Email), "البريد الإلكتروني غير صالح — مثال: user@example.com");
                else
                    ClearErrors(nameof(Email));
            }
        }
    }

    public string AvatarUrl
    {
        get => _avatarUrl;
        set
        {
            if (SetProperty(ref _avatarUrl, value))
            {
                OnPropertyChanged(nameof(HasAvatar));
                (RemoveAvatarCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);

    public bool IsLocked
    {
        get => _isLocked;
        private set => SetProperty(ref _isLocked, value);
    }

    public int? DefaultCashBoxId
    {
        get => _defaultCashBoxId;
        set => SetProperty(ref _defaultCashBoxId, value);
    }

    public ObservableCollection<CashBoxOptionItem> CashBoxOptions
    {
        get => _cashBoxOptions;
        set => SetProperty(ref _cashBoxOptions, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ObservableCollection<RoleOption> RoleOptions { get; } = new()
    {
        new RoleOption { Value = UserRole.Admin, Display = "مدير نظام" },
        new RoleOption { Value = UserRole.Manager, Display = "مدير فرع" },
        new RoleOption { Value = UserRole.Cashier, Display = "كاشير" }
    };
    #endregion

    #region Commands
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand UploadAvatarCommand { get; private set; } = null!;
    public ICommand RemoveAvatarCommand { get; private set; } = null!;
    public ICommand ChangePasswordCommand { get; private set; } = null!;
    #endregion

    #region Methods

    public void LoadCashBoxOptions(List<CashBoxOptionItem> options)
    {
        CashBoxOptions = new ObservableCollection<CashBoxOptionItem>(options ?? new List<CashBoxOptionItem>());
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private void UploadAvatar()
    {
        // Placeholder — will integrate file picker in future phase
        _ = _dialogService.ShowInfoAsync("رفع صورة", "سيتم تفعيل رفع الصور في التحديث القادم");
    }

    private void RemoveAvatar()
    {
        AvatarUrl = string.Empty;
        _toastService.ShowSuccess("تم إزالة الصورة الشخصية");
    }

    private async void OpenChangePassword()
    {
        if (_id <= 0) return;

        var confirm = await _dialogService.ShowConfirmationAsync(
            "إعادة تعيين كلمة المرور",
            $"هل أنت متأكد من إعادة تعيين كلمة المرور للمستخدم: {FullName}؟\n\n" +
            $"سيتم تعيين كلمة المرور إلى: 12345678\n" +
            $"وسيُطلب من المستخدم تغييرها عند أول تسجيل دخول.");

        if (!confirm) return;

        var result = await _userService.ResetPasswordAsync(_id);
        if (result.IsSuccess)
        {
            _toastService.ShowSuccess($"تم إعادة تعيين كلمة المرور للمستخدم {FullName} إلى 12345678");
        }
        else
        {
            await _dialogService.ShowErrorAsync("خطأ في إعادة تعيين كلمة المرور",
                HandleFailure(result.Error ?? "حدث خطأ أثناء إعادة تعيين كلمة المرور", "UserEditorViewModel.OpenChangePassword"));
        }
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(Username))
            AddError(nameof(Username), "اسم المستخدم مطلوب — تأكد من إدخال اسم فريد للدخول إلى النظام");

        if (string.IsNullOrWhiteSpace(FullName))
            AddError(nameof(FullName), "الاسم بالكامل مطلوب — سيظهر هذا الاسم في الفواتير والتقارير");

        if (!string.IsNullOrWhiteSpace(Phone) && Phone.Length > 20)
            AddError(nameof(Phone), "رقم الهاتف لا يتجاوز 20 رقم");

        if (!string.IsNullOrWhiteSpace(Email) && !IsValidEmail(Email))
            AddError(nameof(Email), "البريد الإلكتروني غير صالح — مثال: user@example.com");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync())
            return;

        ErrorMessage = null;

        Result<UserDto> result;

        if (IsEditMode)
        {
            var request = new UpdateUserRequest(
                FullName: FullName,
                Role: (byte)Role,
                Status: (byte)(IsActive ? SalesSystem.Domain.Enums.UserStatus.Active : SalesSystem.Domain.Enums.UserStatus.Inactive),
                Password: null,
                Phone: string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                Email: string.IsNullOrWhiteSpace(Email) ? null : Email,
                DefaultCashBoxId: DefaultCashBoxId);
            result = await _userService.UpdateAsync(_id, request);
        }
        else
        {
            var request = new CreateUserRequest(
                UserName: Username,
                FullName: FullName,
                Role: (byte)Role,
                Phone: string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                Email: string.IsNullOrWhiteSpace(Email) ? null : Email,
                DefaultCashBoxId: DefaultCashBoxId);
            result = await _userService.CreateAsync(request);
        }

        if (result.IsSuccess && result.Value != null)
        {
            _eventBus.Publish(new UserChangedMessage(result.Value.Id));
            _toastService.ShowSuccess(IsEditMode ? "تم تحديث بيانات المستخدم بنجاح" : "تم إنشاء المستخدم بنجاح");
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ بيانات المستخدم", "UserEditorViewModel.SaveOperationAsync");
            await _dialogService.ShowErrorAsync("خطأ في حفظ المستخدم", ErrorMessage!);
        }
    }
    #endregion
}

public record CashBoxOptionItem(int Id, string Name);

public class RoleOption
{
    public UserRole Value { get; set; }
    public string Display { get; set; } = string.Empty;
}
