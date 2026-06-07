using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Users;

/// <summary>
/// ViewModel for setting an initial password (first login or admin-reset flow).
/// The user has no password (PasswordHash == null) and must create one.
/// </summary>
public class SetPasswordViewModel : ViewModelBase
{
    private readonly IAuthApiService _authService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string? _errorMessage;

    public SetPasswordViewModel(
        IAuthApiService authService,
        IDialogService dialogService,
        IToastNotificationService toastService,
        int userId,
        string userName,
        string passwordResetToken,
        bool isFirstLogin = true)
    {
        _authService = authService;
        _dialogService = dialogService;
        _toastService = toastService;
        SetDialogService(dialogService);

        UserId = userId;
        UserName = userName;
        PasswordResetToken = passwordResetToken;
        IsFirstLogin = isFirstLogin;

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync)));
        CancelCommand = new RelayCommand(() => RequestClose());
    }

    #region Properties

    /// <summary>
    /// The user ID to set the password for (passed to the API as a query parameter).
    /// </summary>
    public int UserId { get; }

    /// <summary>
    /// The username displayed to the user for confirmation.
    /// </summary>
    public string UserName { get; }

    /// <summary>
    /// The one-time password reset token (from admin reset or creation flow).
    /// Sent to the API to authorize the password set operation.
    /// </summary>
    public string PasswordResetToken { get; }

    /// <summary>
    /// True when this is the very first login after passwordless user creation.
    /// False when an admin reset the user's password.
    /// Affects the UI title text.
    /// </summary>
    public bool IsFirstLogin { get; }

    /// <summary>
    /// Dynamic header title based on IsFirstLogin flag.
    /// </summary>
    public string HeaderTitle => IsFirstLogin
        ? "تعيين كلمة المرور لأول مرة"
        : "تعيين كلمة مرور جديدة";

    /// <summary>
    /// Set to true when saving succeeds so the caller knows to navigate forward.
    /// </summary>
    public bool DialogResult { get; private set; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (SetProperty(ref _newPassword, value))
            {
                if (!string.IsNullOrWhiteSpace(value) && value.Length < 8)
                    AddError(nameof(NewPassword), "كلمة المرور يجب أن تكون 8 أحرف على الأقل");
                else
                    ClearErrors(nameof(NewPassword));

                // Re-validate confirm password when new password changes
                if (!string.IsNullOrWhiteSpace(ConfirmPassword) && value != ConfirmPassword)
                    AddError(nameof(ConfirmPassword), "كلمة المرور وتأكيدها غير متطابقتين");
                else if (!string.IsNullOrWhiteSpace(ConfirmPassword))
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
                    AddError(nameof(ConfirmPassword), "كلمة المرور وتأكيدها غير متطابقتين");
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

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 8)
            AddError(nameof(NewPassword), "كلمة المرور يجب أن تكون 8 أحرف على الأقل");

        if (NewPassword != ConfirmPassword)
            AddError(nameof(ConfirmPassword), "كلمة المرور وتأكيدها غير متطابقتين");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        var request = new SetPasswordRequest(NewPassword, ConfirmPassword, PasswordResetToken);
        var result = await _authService.SetPasswordAsync(request);

        if (result.IsSuccess)
        {
            _toastService.ShowSuccess(IsFirstLogin
                ? "تم تعيين كلمة المرور بنجاح — يمكنك الآن تسجيل الدخول"
                : "تم تعيين كلمة المرور الجديدة بنجاح");
            DialogResult = true;
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تعيين كلمة المرور", "SetPasswordViewModel.SaveOperationAsync");
            await _dialogService.ShowErrorAsync(IsFirstLogin
                ? "خطأ في تعيين كلمة المرور"
                : "خطأ في تعيين كلمة المرور الجديدة", ErrorMessage!);
        }
    }

    /// <summary>
    /// Public setter for DialogResult so the DialogService can read it after window close.
    /// </summary>
    public void SetDialogResult(bool value) => DialogResult = value;

    public void Dispose() => Cleanup();

    #endregion
}
