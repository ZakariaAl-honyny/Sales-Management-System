using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

/// <summary>
/// ViewModel for Product Category Editor Dialog
/// </summary>
public class ProductCategoryEditorViewModel : ViewModelBase
{
    private readonly IProductCategoryApiService _categoryService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService? _toastService;

    private int _categoryId;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private bool _isEditMode;
    private string? _errorMessage;

    public ProductCategoryEditorViewModel()
    {
        _categoryService = App.GetService<IProductCategoryApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
    }

    /// <summary>
    /// Constructor for dependency injection
    /// </summary>
    public ProductCategoryEditorViewModel(
        IProductCategoryApiService categoryService,
        IEventBus eventBus,
        IDialogService dialogService,
        IToastNotificationService? toastService = null)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
    }

    /// <summary>
    /// Constructor for edit mode — loads existing category data
    /// </summary>
    public ProductCategoryEditorViewModel(ProductCategoryDto category)
        : this(
            App.GetService<IProductCategoryApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<IDialogService>(),
            App.GetService<IToastNotificationService>())
    {
        _categoryId = category.Id;
        _name = category.Name;
        _description = category.Description ?? string.Empty;
        _isEditMode = true;
    }

    /// <summary>
    /// Constructor for edit mode with DI (for testing)
    /// </summary>
    public ProductCategoryEditorViewModel(
        ProductCategoryDto category,
        IProductCategoryApiService categoryService,
        IEventBus eventBus,
        IDialogService dialogService,
        IToastNotificationService? toastService = null)
        : this(categoryService, eventBus, dialogService, toastService)
    {
        _categoryId = category.Id;
        _name = category.Name;
        _description = category.Description ?? string.Empty;
        _isEditMode = true;
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ مجموعة الأصناف...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties
    public string Title => IsEditMode ? "تعديل مجموعة أصناف" : "إضافة مجموعة أصناف جديدة";

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
                    AddError(nameof(Name), "اسم المجموعة مطلوب");
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
            AddError(nameof(Name), "اسم المجموعة مطلوب");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync())
        {
            return;
        }

        ErrorMessage = null;

        if (IsEditMode)
        {
            var updateRequest = new UpdateProductCategoryRequest(
                Name: Name.Trim()
            );

            var result = await _categoryService.UpdateAsync(_categoryId, updateRequest);
            await HandleSaveResult(result, "تم تحديث المجموعة بنجاح");
        }
        else
        {
            var createRequest = new CreateProductCategoryRequest(
                Name: Name.Trim()
            );

            var result = await _categoryService.CreateAsync(createRequest);
            await HandleSaveResult(result, "تم إضافة المجموعة بنجاح");
        }
    }

    private async Task HandleSaveResult(Result<ProductCategoryDto> result, string successMessage)
    {
        if (result.IsSuccess && result.Value != null)
        {
            _eventBus.Publish(new ProductCategoryChangedMessage(result.Value.Id));
            _toastService?.ShowSuccess(successMessage);
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ مجموعة الأصناف", "ProductCategoryEditorViewModel.SaveAsync", "[ProductCategoryEditorViewModel.SaveAsync] Failed to save product category data.");
            await _dialogService.ShowErrorAsync("خطأ في حفظ مجموعة الأصناف", ErrorMessage);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }
    #endregion
}
