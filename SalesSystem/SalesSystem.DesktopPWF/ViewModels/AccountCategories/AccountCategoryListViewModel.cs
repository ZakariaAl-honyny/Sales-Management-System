using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.AccountCategories;

/// <summary>
/// ViewModel for managing account categories.
/// Supports full CRUD with delete confirmation.
/// RULE-059: All buttons always enabled — validates/confirms on click.
/// RULE-220: Newest-first sorting.
/// </summary>
public class AccountCategoryListViewModel : ViewModelBase, IDisposable
{
    private readonly IAccountCategoryApiService _categoryApi;
    private readonly IDialogService _dialogService;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IEventBus _eventBus;

    private ObservableCollection<AccountCategoryDto> _categories = new();
    private AccountCategoryDto? _selectedCategory;
    private bool _isEmpty;
    private string? _errorMessage;

    public AccountCategoryListViewModel()
        : this(
            App.GetService<IAccountCategoryApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IScreenWindowService>(),
            App.GetService<IEventBus>())
    {
    }

    public AccountCategoryListViewModel(
        IAccountCategoryApiService categoryApi,
        IDialogService dialogService,
        IScreenWindowService screenWindowService,
        IEventBus eventBus)
    {
        _categoryApi = categoryApi ?? throw new ArgumentNullException(nameof(categoryApi));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        SetDialogService(dialogService);

        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadCategoriesOperationAsync)));
        AddCommand = new RelayCommand(AddCategory);
        EditCommand = new RelayCommand(EditCategory);
        DeleteCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(DeleteCategoryOperationAsync)));
        CloseCommand = new RelayCommand(RequestClose);

        _ = ExecuteAsync(LoadCategoriesOperationAsync);
    }

    // ═══════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════

    public ObservableCollection<AccountCategoryDto> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    public AccountCategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
                OnPropertyChanged(nameof(HasSelection));
        }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool HasSelection => SelectedCategory != null;
    public bool HasNoCategories => Categories.Count == 0;

    // ═══════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;
    public ICommand CloseCommand { get; private set; } = null!;

    // ═══════════════════════════════════════════════════════════════
    // Operations
    // ═══════════════════════════════════════════════════════════════

    private async Task LoadCategoriesOperationAsync()
    {
        ErrorMessage = null;
        var result = await _categoryApi.GetAllAsync();

        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Categories.Clear();
                foreach (var dto in result.Value.OrderByDescending(x => x.Id))
                {
                    Categories.Add(dto);
                }
                IsEmpty = Categories.Count == 0;
                OnPropertyChanged(nameof(HasNoCategories));
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل التصنيفات المحاسبية", "AccountCategoryListViewModel.Load");
            IsEmpty = Categories.Count == 0;
            OnPropertyChanged(nameof(HasNoCategories));
        }
    }

    private void AddCategory()
    {
        var editorVm = new AccountCategoryEditorViewModel(_categoryApi, _dialogService);
        editorVm.OnSaved += () =>
        {
            _ = ExecuteAsync(LoadCategoriesOperationAsync);
        };
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "إضافة تصنيف محاسبي جديد",
            Width = 500,
            Height = 350
        });
    }

    private void EditCategory()
    {
        if (SelectedCategory == null)
        {
            _ = _dialogService.ShowWarningAsync("تنبيه", "يرجى تحديد تصنيف محاسبي من القائمة.");
            return;
        }

        var editorVm = new AccountCategoryEditorViewModel(_categoryApi, _dialogService, SelectedCategory);
        editorVm.OnSaved += () =>
        {
            _ = ExecuteAsync(LoadCategoriesOperationAsync);
        };
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل التصنيف المحاسبي",
            Width = 500,
            Height = 350
        });
    }

    private async Task DeleteCategoryOperationAsync()
    {
        if (SelectedCategory == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "يرجى تحديد تصنيف محاسبي من القائمة.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync("تأكيد الحذف",
            $"سيتم حذف التصنيف المحاسبي '{SelectedCategory.Name}'.\nهل تريد المتابعة؟");

        if (!confirmed) return;

        var result = await _categoryApi.DeleteAsync(SelectedCategory.Id);
        if (result.IsSuccess)
        {
            await _dialogService.ShowSuccessAsync("تم", $"تم حذف التصنيف '{SelectedCategory.Name}' بنجاح.");
            _ = ExecuteAsync(LoadCategoriesOperationAsync);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حذف التصنيف المحاسبي", "AccountCategoryListViewModel.Delete");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Cleanup
    // ═══════════════════════════════════════════════════════════════

    public override void Cleanup()
    {
        Categories.Clear();
        base.Cleanup();
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }
}
