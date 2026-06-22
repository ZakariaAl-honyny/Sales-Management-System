using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Roles;

public class RoleEditorViewModel : ViewModelBase
{
    private readonly IRoleApiService _roleService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private string _name = string.Empty;
    private string _description = string.Empty;
    private string? _errorMessage;
    private string _windowTitle = "إضافة دور جديد";
    private RoleDto? _roleDto;

    public RoleEditorViewModel()
    {
        _roleService = App.GetService<IRoleApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);
        InitializeCommands();
    }

    public void LoadRole(RoleDto role)
    {
        _roleDto = role;
        Name = role.Name;
        Description = role.Description ?? string.Empty;
        WindowTitle = $"تعديل دور: {role.Name}";
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ الدور...")));
        CancelCommand = new RelayCommand(() => RequestClose());
    }

    #region Properties

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Name), "اسم الدور مطلوب");
                else
                    ClearErrors(nameof(Name));
            }
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string WindowTitle
    {
        get => _windowTitle;
        set => SetProperty(ref _windowTitle, value);
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

        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "اسم الدور مطلوب — أدخل اسماً فريداً للدور");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        Result<RoleDto> result;
        if (_roleDto == null)
        {
            var request = new CreateRoleRequest(Name.Trim(), string.IsNullOrWhiteSpace(Description) ? null : Description.Trim());
            result = await _roleService.CreateAsync(request);
        }
        else
        {
            var request = new UpdateRoleRequest(Name.Trim(), string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(), _roleDto.IsActive);
            result = await _roleService.UpdateAsync(_roleDto.Id, request);
        }

        if (result.IsSuccess)
        {
            _toastService.ShowSuccess("تم حفظ الدور بنجاح");
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ الدور", "RoleEditorViewModel.SaveOperationAsync");
            await _dialogService.ShowErrorAsync("خطأ في حفظ الدور", ErrorMessage!);
        }
    }

    #endregion
}
