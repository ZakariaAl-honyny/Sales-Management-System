using System.Windows.Input;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.AccountCategories;

/// <summary>
/// Editor ViewModel for creating or editing an account category.
/// RULE-059: Save button always enabled — validates on click with warning dialog.
/// Uses INotifyDataErrorInfo for real-time validation.
/// </summary>
public class AccountCategoryEditorViewModel : ViewModelBase
{
    private readonly IAccountCategoryApiService _categoryApi;
    private readonly IDialogService _dialogService;
    private readonly int? _editId;

    /// <summary>
    /// Raised when the entity is saved successfully.
    /// </summary>
    public event Action? OnSaved;

    /// <summary>
    /// Parameterless constructor for design-time / DI.
    /// </summary>
    public AccountCategoryEditorViewModel()
        : this(
            App.GetService<IAccountCategoryApiService>(),
            App.GetService<IDialogService>())
    {
    }

    /// <summary>
    /// Constructor for creating a new category.
    /// </summary>
    public AccountCategoryEditorViewModel(
        IAccountCategoryApiService categoryApi,
        IDialogService dialogService)
        : this(categoryApi, dialogService, null)
    {
    }

    /// <summary>
    /// Constructor for editing an existing category.
    /// </summary>
    public AccountCategoryEditorViewModel(
        IAccountCategoryApiService categoryApi,
        IDialogService dialogService,
        AccountCategoryDto? existing)
    {
        _categoryApi = categoryApi ?? throw new ArgumentNullException(nameof(categoryApi));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        SetDialogService(dialogService);

        if (existing != null)
        {
            _editId = existing.Id;
            _name = existing.Name;
            _description = existing.Description;
            IsEditMode = true;
        }

        SaveCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync)));
        CancelCommand = new RelayCommand(RequestClose);
    }

    // ═══════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                ClearErrors(nameof(Name));
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Name), "اسم التصنيف مطلوب");
            }
        }
    }

    private string? _description;
    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string Title => IsEditMode ? "تعديل التصنيف المحاسبي" : "إضافة تصنيف محاسبي جديد";

    // ═══════════════════════════════════════════════════════════════
    // Validation
    // ═══════════════════════════════════════════════════════════════

    private bool Validate()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "اسم التصنيف مطلوب");
        if (Name?.Trim().Length > 100)
            AddError(nameof(Name), "اسم التصنيف لا يمكن أن يتجاوز 100 حرف");
        if (Description?.Length > 500)
            AddError(nameof(Description), "الوصف لا يمكن أن يتجاوز 500 حرف");

        return !HasErrors;
    }

    // ═══════════════════════════════════════════════════════════════
    // Operations
    // ═══════════════════════════════════════════════════════════════

    private async Task SaveOperationAsync()
    {
        ErrorMessage = null;

        if (!Validate())
        {
            await ValidateAllAsync();
            return;
        }

        if (IsEditMode && _editId.HasValue)
        {
            var request = new UpdateAccountCategoryRequest(Name.Trim(), Description?.Trim());
            var result = await _categoryApi.UpdateAsync(_editId.Value, request);

            if (result.IsSuccess)
            {
                await _dialogService.ShowSuccessAsync("تم", "تم تحديث التصنيف المحاسبي بنجاح");
                OnSaved?.Invoke();
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحديث التصنيف", "AccountCategoryEditorViewModel.Update");
                await _dialogService.ShowErrorAsync("خطأ في حفظ التصنيف المحاسبي", ErrorMessage!);
            }
        }
        else
        {
            var request = new CreateAccountCategoryRequest(Name.Trim(), Description?.Trim());
            var result = await _categoryApi.CreateAsync(request);

            if (result.IsSuccess)
            {
                await _dialogService.ShowSuccessAsync("تم", "تم إنشاء التصنيف المحاسبي بنجاح");
                OnSaved?.Invoke();
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إنشاء التصنيف", "AccountCategoryEditorViewModel.Create");
                await _dialogService.ShowErrorAsync("خطأ في حفظ التصنيف المحاسبي", ErrorMessage!);
            }
        }
    }
}
