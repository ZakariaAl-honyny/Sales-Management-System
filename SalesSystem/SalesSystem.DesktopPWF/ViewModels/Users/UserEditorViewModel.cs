using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Users;

public class UserEditorViewModel : ViewModelBase
{
    private readonly IUserApiService _userService;
    private readonly IRoleApiService _roleService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private int _id;
    private string _username = string.Empty;
    private string _fullName = string.Empty;
    private int _selectedRoleId = 3; // Cashier (default)
    private bool _isActive = true;
    private bool _isEditMode;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _avatarUrl = string.Empty;
    private int? _defaultCashBoxId;
    private ObservableCollection<CashBoxOptionItem> _cashBoxOptions = new();
    private bool _isLocked;

    // Role assignment fields
    private ObservableCollection<RoleDto> _availableRoles = new();
    private ObservableCollection<RoleDto> _assignedRoles = new();
    private RoleDto? _selectedAvailableRole;
    private RoleDto? _selectedAssignedRole;

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }
    private string? _errorMessage;

    public UserEditorViewModel()
    {
        _userService = App.GetService<IUserApiService>();
        _roleService = App.GetService<IRoleApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        SetDialogService(_dialogService);
        _isEditMode = false;
        WindowTitle = "إضافة مستخدم جديد";
        InitializeCommands();
        _ = LoadAuxiliaryDataAsync();
    }

    public UserEditorViewModel(UserDto user) : this()
    {
        _id = user.Id;
        Username = user.UserName;
        // FullName removed per schema v4.10
        SelectedRoleId = user.Role;
        IsActive = !user.IsLocked;
        // Phone removed per schema v4.10
        // Email removed per schema v4.10
        AvatarUrl = user.AvatarPath ?? string.Empty;
        DefaultCashBoxId = user.DefaultCashBoxId;
        _isEditMode = true;
        _isLocked = user.IsLocked;
        WindowTitle = $"تعديل مستخدم: {user.UserName}";
        _ = LoadUserRolesAsync(user.Id);
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ المستخدم...")));
        CancelCommand = new RelayCommand(() => RequestClose());
        UploadAvatarCommand = new RelayCommand(UploadAvatar);
        RemoveAvatarCommand = new RelayCommand(RemoveAvatar, () => HasAvatar);
        ChangePasswordCommand = new RelayCommand(OpenChangePassword);

        // Role assignment commands
        AddRoleCommand = new RelayCommand(AddRole, () => SelectedAvailableRole != null);
        RemoveRoleCommand = new RelayCommand(RemoveRole, () => SelectedAssignedRole != null);

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

    public int SelectedRoleId
    {
        get => _selectedRoleId;
        set => SetProperty(ref _selectedRoleId, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
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
        new RoleOption { Id = 1, Display = "مدير النظام" },
        new RoleOption { Id = 2, Display = "مدير" },
        new RoleOption { Id = 3, Display = "كاشير" },
        new RoleOption { Id = 4, Display = "مراقب" },
        new RoleOption { Id = 5, Display = "مدير فرع" },
    };

    // ─── Role Assignment Properties ─────────────────────────────────

    public ObservableCollection<RoleDto> AvailableRoles
    {
        get => _availableRoles;
        set => SetProperty(ref _availableRoles, value);
    }

    public ObservableCollection<RoleDto> AssignedRoles
    {
        get => _assignedRoles;
        set => SetProperty(ref _assignedRoles, value);
    }

    public RoleDto? SelectedAvailableRole
    {
        get => _selectedAvailableRole;
        set
        {
            if (SetProperty(ref _selectedAvailableRole, value))
            {
                (AddRoleCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public RoleDto? SelectedAssignedRole
    {
        get => _selectedAssignedRole;
        set
        {
            if (SetProperty(ref _selectedAssignedRole, value))
            {
                (RemoveRoleCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region Commands
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand UploadAvatarCommand { get; private set; } = null!;
    public ICommand RemoveAvatarCommand { get; private set; } = null!;
    public ICommand ChangePasswordCommand { get; private set; } = null!;

    // Role assignment commands
    public ICommand AddRoleCommand { get; private set; } = null!;
    public ICommand RemoveRoleCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public void LoadCashBoxOptions(List<CashBoxOptionItem> options)
    {
        CashBoxOptions = new ObservableCollection<CashBoxOptionItem>(options ?? new List<CashBoxOptionItem>());
    }

    private async Task LoadAuxiliaryDataAsync()
    {
        // Load available roles
        var rolesResult = await _roleService.GetAllAsync();
        if (rolesResult.IsSuccess && rolesResult.Value != null)
        {
            await InvokeOnUIThreadAsync(() =>
            {
                AvailableRoles.Clear();
                foreach (var role in rolesResult.Value.Where(r => r.IsActive))
                {
                    AvailableRoles.Add(role);
                }
                return Task.CompletedTask;
            });
        }
    }

    private async Task LoadUserRolesAsync(int userId)
    {
        // Load user roles from API
        try
        {
            var httpClient = App.GetService<System.Net.Http.HttpClient>();
            var session = App.GetService<ISessionService>();
            var token = session.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var rolesResponse = await httpClient.GetAsync($"api/v1/users/{userId}/roles");
            if (rolesResponse.IsSuccessStatusCode)
            {
                var userRoles = System.Text.Json.JsonSerializer.Deserialize<List<UserRoleDto>>(
                    await rolesResponse.Content.ReadAsStringAsync(),
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userRoles != null)
                {
                    await InvokeOnUIThreadAsync(() =>
                    {
                        foreach (var ur in userRoles)
                        {
                            var role = AvailableRoles.FirstOrDefault(r => r.Id == ur.RoleId);
                            if (role != null)
                            {
                                AssignedRoles.Add(role);
                                AvailableRoles.Remove(role);
                            }
                        }
                        return Task.CompletedTask;
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to load user roles for user {UserId}", userId);
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
            $"هل أنت متأكد من إعادة تعيين كلمة المرور للمستخدم: {Username}؟\n\n" +
            $"سيتم تعيين كلمة المرور إلى: 12345678\n" +
            $"وسيُطلب من المستخدم تغييرها عند أول تسجيل دخول.");

        if (!confirm) return;

        var result = await _userService.ResetPasswordAsync(_id);
        if (result.IsSuccess)
        {
            _toastService.ShowSuccess($"تم إعادة تعيين كلمة المرور للمستخدم {Username} إلى 12345678");
        }
        else
        {
            await _dialogService.ShowErrorAsync("خطأ في إعادة تعيين كلمة المرور",
                HandleFailure(result.Error ?? "حدث خطأ أثناء إعادة تعيين كلمة المرور", "UserEditorViewModel.OpenChangePassword"));
        }
    }

    #region Role Assignment

    private void AddRole()
    {
        if (SelectedAvailableRole == null) return;

        var role = SelectedAvailableRole;
        AvailableRoles.Remove(role);
        AssignedRoles.Add(role);
        SelectedAvailableRole = AvailableRoles.FirstOrDefault();
    }

    private void RemoveRole()
    {
        if (SelectedAssignedRole == null) return;

        var role = SelectedAssignedRole;
        AssignedRoles.Remove(role);
        AvailableRoles.Add(role);
        SelectedAssignedRole = AssignedRoles.FirstOrDefault();
    }

    #endregion

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(Username))
            AddError(nameof(Username), "اسم المستخدم مطلوب — تأكد من إدخال اسم فريد للدخول إلى النظام");

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
                Role: (byte)SelectedRoleId,
                IsLocked: IsLocked,
                Password: null,
                DefaultCashBoxId: DefaultCashBoxId);
            result = await _userService.UpdateAsync(_id, request);
        }
        else
        {
            var request = new CreateUserRequest(
                UserName: Username,
                Role: (byte)SelectedRoleId,
                Password: null,
                DefaultCashBoxId: DefaultCashBoxId);
            result = await _userService.CreateAsync(request);
        }

        if (result.IsSuccess && result.Value != null)
        {
            // Save role assignments
            var roleIds = AssignedRoles.Select(r => r.Id).ToList();
            if (roleIds.Any())
            {
                // Ensure primary role is included
                if (!roleIds.Contains(SelectedRoleId))
                    roleIds.Add(SelectedRoleId);

                await SaveUserRolesAsync(result.Value.Id, roleIds);
            }

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

    private async Task SaveUserRolesAsync(int userId, List<int> roleIds)
    {
        try
        {
            var httpClient = App.GetService<System.Net.Http.HttpClient>();
            var session = App.GetService<ISessionService>();
            var token = session.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var json = System.Text.Json.JsonSerializer.Serialize(roleIds);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PutAsync($"api/v1/users/{userId}/roles", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Serilog.Log.Warning("Failed to save roles for user {UserId}: {Error}", userId, errorBody);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to save roles for user {UserId}", userId);
        }
    }

    #endregion
}

public record CashBoxOptionItem(int Id, string Name);

public class RoleOption
{
    public int Id { get; set; }
    public string Display { get; set; } = string.Empty;
}
