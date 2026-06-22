using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

/// <summary>
/// ViewModel for Product Categories List View
/// </summary>
public class ProductCategoriesListViewModel : ViewModelBase, IDisposable
{
    private readonly IProductCategoryApiService _categoryService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IToastNotificationService? _toastService;

    private ObservableCollection<ProductCategoryDto> _categories = new();
    private ICollectionView? _categoriesView;
    private ProductCategoryDto? _selectedCategory;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;
    private string _lastUpdateTime = "لم يتم التحديث بعد";

    public ProductCategoriesListViewModel()
    {
        _categoryService = App.GetService<IProductCategoryApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        _toastService = App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    /// <summary>
    /// Constructor for dependency injection (used in tests)
    /// </summary>
    public ProductCategoriesListViewModel(
        IProductCategoryApiService categoryService,
        IEventBus eventBus,
        IDialogService dialogService,
        IScreenWindowService screenWindowService,
        IToastNotificationService? toastService = null)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadCategoriesAsync);
        AddCommand = new RelayCommand(AddCategory);
        EditCommand = new RelayCommand(EditCategory);
        DeleteCommand = new AsyncRelayCommand(DeleteCategoryAsync);
        RestoreCommand = new AsyncRelayCommand(RestoreCategoryAsync);
        SearchCommand = new RelayCommand(Search);

        _eventBus.Subscribe<ProductCategoryChangedMessage>(OnCategoryChanged);
    }

    #region Properties
    public ObservableCollection<ProductCategoryDto> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    public ICollectionView? CategoriesView
    {
        get => _categoriesView;
        private set => SetProperty(ref _categoriesView, value);
    }

    public ProductCategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                CategoriesView?.Refresh();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public bool IncludeInactive
    {
        get => _includeInactive;
        set
        {
            if (SetProperty(ref _includeInactive, value))
            {
                _ = ExecuteAsync(LoadCategoriesOperationAsync);
            }
        }
    }

    public string LastUpdateTime
    {
        get => _lastUpdateTime;
        private set => SetProperty(ref _lastUpdateTime, value);
    }
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;
    public ICommand RestoreCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;
    #endregion

    #region Methods
    public async Task LoadCategoriesAsync()
    {
        await ExecuteAsync(LoadCategoriesOperationAsync);
    }

    private async Task LoadCategoriesOperationAsync()
    {
        ErrorMessage = null;

        var result = await _categoryService.GetAllAsync(IncludeInactive);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Categories.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Categories.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Categories.Count == 0;
                LastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل مجموعات الأصناف", "ProductCategoriesListViewModel.LoadCategoriesAsync", "[ProductCategoriesListViewModel.LoadCategoriesAsync] Failed to load product categories from API.");
            IsEmpty = Categories.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        CategoriesView = new ListCollectionView(Categories);
        CategoriesView.Filter = FilterCategories;
    }

    private bool FilterCategories(object obj)
    {
        if (obj is not ProductCategoryDto category) return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var searchLower = SearchText.Trim().ToLower();
        return category.Name.ToLower().Contains(searchLower) ||
               (category.Description?.ToLower().Contains(searchLower) ?? false);
    }

    private void AddCategory()
    {
        var editorVm = App.GetService<ProductCategoryEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "مجموعة أصناف جديدة",
            Width = 550,
            Height = 400,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadCategoriesAsync());
            }
        });
    }

    private void EditCategory()
    {
        if (SelectedCategory == null) return;

        var editorVm = new ProductCategoryEditorViewModel(SelectedCategory);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل مجموعة أصناف",
            Width = 550,
            Height = 400,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadCategoriesAsync());
            }
        });
    }

    public void EditCategoryFromDoubleClick()
    {
        if (SelectedCategory != null)
        {
            EditCategory();
        }
    }

    public async Task DeleteCategoryAsync()
    {
        if (SelectedCategory == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"المجموعة: {SelectedCategory.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        await ExecuteAsync(() => DeleteCategoryOperationAsync(strategy));
    }

    private async Task DeleteCategoryOperationAsync(DeleteStrategy strategy)
    {
        ErrorMessage = null;

        if (strategy == DeleteStrategy.Deactivate)
        {
            var deleteResult = await _categoryService.DeactivateAsync(SelectedCategory!.Id);
            if (deleteResult.IsSuccess)
            {
                _eventBus.Publish(new ProductCategoryChangedMessage(SelectedCategory.Id));
                await LoadCategoriesAsync();
                _toastService?.ShowSuccess("تم إلغاء تنشيط المجموعة بنجاح");
            }
            else
            {
                ErrorMessage = HandleFailure(deleteResult.Error ?? "فشل في حذف المجموعة", "ProductCategoriesListViewModel.DeleteCategoryAsync", $"[ProductCategoriesListViewModel.DeleteCategoryAsync] Failed to deactivate category with ID {SelectedCategory.Id}.");
                await _dialogService.ShowErrorAsync("خطأ في حذف المجموعة", ErrorMessage!);
            }
        }
    }

    public async Task RestoreCategoryAsync()
    {
        if (SelectedCategory == null) return;

        await ExecuteAsync(RestoreCategoryOperationAsync);
    }

    private async Task RestoreCategoryOperationAsync()
    {
        ErrorMessage = null;

        var result = await _categoryService.ReactivateAsync(SelectedCategory!.Id);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new ProductCategoryChangedMessage(SelectedCategory.Id));
            await LoadCategoriesAsync();
            _toastService?.ShowSuccess("تم استعادة المجموعة بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في استعادة المجموعة", "ProductCategoriesListViewModel.RestoreCategoryAsync", $"[ProductCategoriesListViewModel.RestoreCategoryAsync] Failed to restore category with ID {SelectedCategory.Id}.");
            await _dialogService.ShowErrorAsync("خطأ في استعادة المجموعة", ErrorMessage!);
        }
    }

    private void Search()
    {
        CategoriesView?.Refresh();
    }

    private void OnCategoryChanged(ProductCategoryChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () =>
        {
            await LoadCategoriesAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<ProductCategoryChangedMessage>(OnCategoryChanged);
    }

    public void Dispose() => Cleanup();
    #endregion
}
