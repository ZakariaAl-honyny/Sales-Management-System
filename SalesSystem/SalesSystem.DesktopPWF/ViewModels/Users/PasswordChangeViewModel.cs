using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Users;

public class PasswordChangeViewModel : ViewModelBase
{
    private readonly IAuthApiService _authService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string? _errorMessage;

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public PasswordChangeViewModel(IAuthApiService authService, IDialogService dialogService,
        IToastNotificationService toastService)
    {
        _authService = authService;
        _dialogService = dialogService;
        _toastService = toastService;
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync)));
        CancelCommand = new RelayCommand(() => RequestClose());
    }

    #region Properties

    public string CurrentPassword
    {
        get => _currentPassword;
        set => SetProperty(ref _currentPassword, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (SetProperty(ref _newPassword, value))
            {
                if (!string.IsNullOrWhiteSpace(value) && value.Length < 8)
                    AddError(nameof(NewPassword), "كلمة المرور الجديدة يجب أن تكون 8 أحرف على الأقل");
                else
                    ClearErrors(nameof(NewPassword));

                if (!string.IsNullOrWhiteSpace(ConfirmPassword) && value != ConfirmPassword)
                    AddError(nameof(ConfirmPassword), "كلمة المرور الجديدة وتأكيدها غير متطابقتين");
                else
                    ClearErrors(nameof(ConfirmPassword));
            }
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (SetProperty(ref _confirmPassword, value))
            {
                if (!string.IsNullOrWhiteSpace(value) && value != NewPassword)
                    AddError(nameof(ConfirmPassword), "كلمة المرور الجديدة وتأكيدها غير متطابقتين");
                else
                    ClearErrors(nameof(ConfirmPassword));
            }
        }
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    #endregion

    #region Methods

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(CurrentPassword))
            AddError(nameof(CurrentPassword), "كلمة المرور الحالية مطلوبة");

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 8)
            AddError(nameof(NewPassword), "كلمة المرور الجديدة يجب أن تكون 8 أحرف على الأقل");

        if (NewPassword != ConfirmPassword)
            AddError(nameof(ConfirmPassword), "كلمة المرور الجديدة وتأكيدها غير متطابقتين");

        if (NewPassword == CurrentPassword)
            AddError(nameof(NewPassword), "كلمة المرور الجديدة يجب أن تختلف عن الحالية");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        var request = new ChangePasswordRequest(CurrentPassword, NewPassword, ConfirmPassword);
        var result = await _authService.ChangePasswordAsync(request);

        if (result.IsSuccess)
        {
            _toastService.ShowSuccess("تم تغيير كلمة المرور بنجاح");
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تغيير كلمة المرور", "PasswordChangeViewModel.SaveOperationAsync");
            await _dialogService.ShowErrorAsync("خطأ في تغيير كلمة المرور", ErrorMessage!);
        }
    }

    public void Dispose() => Cleanup();

    #endregion
}
