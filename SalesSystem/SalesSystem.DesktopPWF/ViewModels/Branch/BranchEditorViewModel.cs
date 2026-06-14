using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Branch;

public class BranchEditorViewModel : ViewModelBase
{
    private readonly IBranchApiService _branchService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int _branchId;
    private string _name = string.Empty;
    private string? _code;
    private bool _isActive = true;
    private bool _isEditMode;
    private string? _errorMessage;

    public BranchEditorViewModel()
    {
        _branchService = App.GetService<IBranchApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ الفرع...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

    public string Title => IsEditMode ? "تعديل فرع" : "إضافة فرع جديد";

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
                    AddError(nameof(Name), "اسم الفرع مطلوب");
                else
                    ClearErrors(nameof(Name));
            }
        }
    }

    public string? Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
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

    public void LoadBranch(BranchDto branch)
    {
        _branchId = branch.Id;
        _name = branch.Name;
        _code = branch.Code;
        _isActive = branch.IsActive;
        _isEditMode = true;
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "اسم الفرع مطلوب");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        if (IsEditMode)
        {
            var request = new UpdateBranchRequest(Name, Code);
            var result = await _branchService.UpdateAsync(_branchId, request);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم تحديث الفرع بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحديث الفرع", "BranchEditorViewModel.SaveAsync");
            }
        }
        else
        {
            var request = new CreateBranchRequest(Name, Code);
            var result = await _branchService.CreateAsync(request);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم إضافة الفرع بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إضافة الفرع", "BranchEditorViewModel.SaveAsync");
            }
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    #endregion
}
