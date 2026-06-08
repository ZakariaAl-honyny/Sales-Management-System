using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.CashBoxes;

public class CashBoxEditorViewModel : ViewModelBase
{
    private readonly ICashBoxApiService _cashBoxService;
    private readonly ICategoryApiService _categoryService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private string _boxName = string.Empty;
    private int? _accountId;
    private bool _isAccountAutoCreated = true;
    private string? _accountInfo;
    private int? _categoryId;
    private string? _phoneNumber;
    private string? _taxNumber;
    private string? _address;
    private bool _isEditMode;
    private string? _errorMessage;
    private ObservableCollection<CategoryDto> _categories = new();
    private CategoryDto? _selectedCategory;

    public CashBoxEditorViewModel()
        : this(
            App.GetService<ICashBoxApiService>(),
            App.GetService<ICategoryApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public CashBoxEditorViewModel(
        ICashBoxApiService cashBoxService,
        ICategoryApiService categoryService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService? toastService = null)
    {
        _cashBoxService = cashBoxService ?? throw new ArgumentNullException(nameof(cashBoxService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _ = LoadCategoriesAsync();
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ الصندوق...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

    public string BoxName
    {
        get => _boxName;
        set
        {
            if (SetProperty(ref _boxName, value))
            {
                ValidateField(() => !string.IsNullOrWhiteSpace(value), nameof(BoxName), "اسم الصندوق مطلوب");
            }
        }
    }

    public int? AccountId
    {
        get => _accountId;
        set => SetProperty(ref _accountId, value);
    }

    public bool IsAccountAutoCreated
    {
        get => _isAccountAutoCreated;
        set => SetProperty(ref _isAccountAutoCreated, value);
    }

    public string? AccountInfo
    {
        get => _accountInfo;
        set => SetProperty(ref _accountInfo, value);
    }

    public ObservableCollection<CategoryDto> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    public CategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                CategoryId = value?.Id;
            }
        }
    }

    public int? CategoryId
    {
        get => _categoryId;
        set => SetProperty(ref _categoryId, value);
    }

    public string? PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    public string? TaxNumber
    {
        get => _taxNumber;
        set => SetProperty(ref _taxNumber, value);
    }

    public string? Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        private set => SetProperty(ref _isEditMode, value);
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

    public void LoadForEdit(string boxName, int? accountId, string? accountName, string? phoneNumber, string? taxNumber, string? address, int? categoryId, string? categoryName)
    {
        BoxName = boxName;
        AccountId = accountId;
        IsAccountAutoCreated = accountId == null;
        AccountInfo = accountName ?? "سيتم إنشاء حساب تلقائي";
        PhoneNumber = phoneNumber;
        TaxNumber = taxNumber;
        Address = address;
        CategoryId = categoryId;
        IsEditMode = true;

        // Pre-select category if available
        if (categoryId.HasValue && Categories.Any(c => c.Id == categoryId.Value))
        {
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == categoryId.Value);
        }
    }

    public async Task LoadCategoriesAsync()
    {
        await ExecuteAsync(LoadCategoriesOperationAsync);
    }

    private async Task LoadCategoriesOperationAsync()
    {
        var result = await _categoryService.GetAllAsync();
        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
        {
            Categories.Clear();
            foreach (var cat in result.Value)
            {
                Categories.Add(cat);
            }
        });
        }
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(BoxName))
            AddError(nameof(BoxName), "اسم الصندوق مطلوب");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        // Determine AccountId: null = auto-create
        int? requestAccountId = IsAccountAutoCreated ? null : AccountId;
        int? requestCategoryId = SelectedCategory?.Id ?? CategoryId;

        var request = new CreateCashBoxRequest(
            BoxName.Trim(),
            requestAccountId,
            requestCategoryId,
            null,   // BranchId
            null,   // AssignedUserId
            null,   // CurrencyId
            string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
            string.IsNullOrWhiteSpace(TaxNumber) ? null : TaxNumber.Trim(),
            string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
            null);  // Notes

        var result = await _cashBoxService.CreateAsync(request);

        if (result.IsSuccess)
        {
            var id = result.Value?.Id ?? 0;
            _eventBus.Publish(new CashBoxChangedMessage(id));
            _toastService.ShowSuccess("تم إضافة الصندوق بنجاح");
            RequestClose();
        }
        else
        {
            var error = HandleFailure(result.Error ?? "فشل في إضافة الصندوق", "CashBoxEditorViewModel.SaveOperationAsync", "[CashBoxEditorViewModel.SaveOperationAsync] Failed to create cash box.");
            ErrorMessage = error;
            await _dialogService.ShowErrorAsync("خطأ في حفظ الصندوق", error);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    public override void Cleanup()
    {
        Categories.Clear();
        base.Cleanup();
    }

    #endregion
}
