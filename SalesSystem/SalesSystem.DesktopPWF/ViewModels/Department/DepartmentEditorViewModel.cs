using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Department;

public class DepartmentEditorViewModel : ViewModelBase
{
    private readonly IDepartmentApiService _departmentService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int _departmentId;
    private string _name = string.Empty;
    private bool _isActive = true;
    private bool _isEditMode;
    private string? _errorMessage;

    public DepartmentEditorViewModel()
    {
        _departmentService = App.GetService<IDepartmentApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ القسم...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

    public string Title => IsEditMode ? "تعديل قسم" : "إضافة قسم جديد";

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Name), "اسم القسم مطلوب");
                else
                    ClearErrors(nameof(Name));
            }
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public void LoadDepartment(DepartmentDto department)
    {
        _departmentId = department.Id;
        _name = department.Name;
        _isActive = department.IsActive;
        _isEditMode = true;
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "اسم القسم مطلوب");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        if (IsEditMode)
        {
            var request = new UpdateDepartmentRequest(Name);
            var result = await _departmentService.UpdateAsync(_departmentId, request);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم تحديث القسم بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحديث القسم", "DepartmentEditorViewModel.SaveAsync");
                await _dialogService.ShowErrorAsync("خطأ في تحديث القسم", ErrorMessage!);
            }
        }
        else
        {
            var request = new CreateDepartmentRequest(Name);
            var result = await _departmentService.CreateAsync(request);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم إضافة القسم بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إضافة القسم", "DepartmentEditorViewModel.SaveAsync");
                await _dialogService.ShowErrorAsync("خطأ في إضافة القسم", ErrorMessage!);
            }
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    #endregion
}
