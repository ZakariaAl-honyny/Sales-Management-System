using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

public class ProductUnitsListViewModel : ViewModelBase, IDisposable
{
    private readonly IProductApiService _productService;
    private readonly IProductUnitApiService _unitService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<ProductUnitDto> _units = new();
    private ProductUnitDto? _selectedUnit;
    private string? _errorMessage;
    private bool _isEmpty;
    private int _productId;
    private bool _includeInactive;

    // Search/selection fields
    private string _searchText = string.Empty;
    private ObservableCollection<ProductDto> _searchResults = new();
    private ProductDto? _selectedProduct;
    private bool _hasSelectedProduct;
    private string _selectedProductName = string.Empty;
    private bool _isSearching;

    public ProductUnitsListViewModel()
        : this(
            App.GetService<IProductApiService>(),
            App.GetService<IProductUnitApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IScreenWindowService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public ProductUnitsListViewModel(
        IProductApiService productService,
        IProductUnitApiService unitService,
        IDialogService dialogService,
        IEventBus eventBus,
        IScreenWindowService screenWindowService,
        IToastNotificationService? toastService = null)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadUnitsAsync);
        AddCommand = new RelayCommand(AddUnit);
        EditCommand = new RelayCommand(EditUnit);
        DeleteCommand = new AsyncRelayCommand(DeleteUnitAsync);
        PricesCommand = new RelayCommand(ViewPrices);

        SearchCommand = new AsyncRelayCommand(SearchProductsAsync);
        SelectProductCommand = new RelayCommand<ProductDto>(SelectProduct);
        ClearProductCommand = new RelayCommand(ClearProductSelection);
    }

    public void OnNavigatedTo()
    {
        _eventBus.Subscribe<ProductChangedMessage>(OnProductChanged);
        // Do NOT auto-load units — user must search and select a product first
    }

    #region Properties

    public ObservableCollection<ProductUnitDto> Units
    {
        get => _units;
        set => SetProperty(ref _units, value);
    }

    public ProductUnitDto? SelectedUnit
    {
        get => _selectedUnit;
        set => SetProperty(ref _selectedUnit, value);
    }

    public int ProductId
    {
        get => _productId;
        set => SetProperty(ref _productId, value);
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
                _ = LoadUnitsAsync();
            }
        }
    }

    #endregion

    #region Search/Selection Properties

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public ObservableCollection<ProductDto> SearchResults
    {
        get => _searchResults;
        set => SetProperty(ref _searchResults, value);
    }

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public bool HasSelectedProduct
    {
        get => _hasSelectedProduct;
        private set => SetProperty(ref _hasSelectedProduct, value);
    }

    public string SelectedProductName
    {
        get => _selectedProductName;
        private set => SetProperty(ref _selectedProductName, value);
    }

    public bool IsSearching
    {
        get => _isSearching;
        set => SetProperty(ref _isSearching, value);
    }

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;
    public ICommand PricesCommand { get; private set; } = null!;

    public ICommand SearchCommand { get; private set; } = null!;
    public ICommand SelectProductCommand { get; private set; } = null!;
    public ICommand ClearProductCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadUnitsAsync()
    {
        if (ProductId <= 0) return;
        await ExecuteAsync(LoadUnitsOperationAsync);
    }

    private async Task LoadUnitsOperationAsync()
    {
        ErrorMessage = null;
        var result = await _unitService.GetByProductIdAsync(ProductId);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Units.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Units.Add(item);
                }
                IsEmpty = Units.Count == 0;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل وحدات القياس", "ProductUnitsListViewModel.LoadUnitsOperationAsync", "[ProductUnitsListViewModel.LoadUnitsOperationAsync] Failed to load product units from API.");
            IsEmpty = Units.Count == 0;
        }
    }

    /// <summary>
    /// Searches for products matching the current SearchText via the API.
    /// </summary>
    public async Task SearchProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            InvokeOnUIThread(() => SearchResults.Clear());
            return;
        }

        await ExecuteAsync(SearchProductsOperationAsync);
    }

    private async Task SearchProductsOperationAsync()
    {
        IsSearching = true;
        ErrorMessage = null;
        try
        {
            var result = await _productService.SearchAsync(SearchText.Trim());
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    SearchResults.Clear();
                    foreach (var product in result.Value.OrderBy(p => p.Name))
                    {
                        SearchResults.Add(product);
                    }
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في البحث عن المنتجات", "ProductUnitsListViewModel.SearchProductsOperationAsync", "[ProductUnitsListViewModel.SearchProductsOperationAsync] Failed to search products.");
            }
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>
    /// Selects a product from the search results and loads its units.
    /// </summary>
    private void SelectProduct(ProductDto? product)
    {
        if (product == null) return;

        ProductId = product.Id;
        SelectedProduct = product;
        SelectedProductName = product.Name;
        HasSelectedProduct = true;
        SearchResults.Clear();
        SearchText = string.Empty;
        ErrorMessage = null;

        _ = LoadUnitsAsync();
    }

    /// <summary>
    /// Clears the product selection and returns to search mode.
    /// </summary>
    private void ClearProductSelection()
    {
        ProductId = 0;
        SelectedProduct = null;
        SelectedProductName = string.Empty;
        HasSelectedProduct = false;
        Units.Clear();
        IsEmpty = false;
        ErrorMessage = null;
    }

    private void AddUnit()
    {
        if (ProductId <= 0) return;

        var editorVm = new ProductUnitEditorViewModel(ProductId);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "إضافة وحدة قياس جديدة",
            OnClosed = (vm) =>
            {
                if (vm is ProductUnitEditorViewModel editor && editor.UnitId.HasValue)
                {
                    _eventBus.Publish(new ProductChangedMessage(ProductId));
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadUnitsAsync());
                }
            }
        });
    }

    private void EditUnit()
    {
        if (ProductId <= 0) return;
        if (SelectedUnit == null) return;

        var editorVm = new ProductUnitEditorViewModel(ProductId, SelectedUnit.Id);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل وحدة القياس",
            OnClosed = (vm) =>
            {
                _eventBus.Publish(new ProductChangedMessage(ProductId));
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadUnitsAsync());
            }
        });
    }

    public async Task DeleteUnitAsync()
    {
        if (ProductId <= 0) return;
        if (SelectedUnit == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"الوحدة: {SelectedUnit.UnitName}");
        if (strategy == DeleteStrategy.Cancel) return;

        var unitId = SelectedUnit.Id;
        await ExecuteAsync(() => DeleteUnitOperationAsync(unitId, strategy));
    }

    private async Task DeleteUnitOperationAsync(int unitId, DeleteStrategy strategy)
    {
        ErrorMessage = null;
        var deleteResult = await _unitService.DeleteUnitAsync(ProductId, unitId, strategy);

        if (deleteResult.IsSuccess)
        {
            _eventBus.Publish(new ProductChangedMessage(ProductId));
            await LoadUnitsAsync();
            _toastService.ShowSuccess("تم حذف الوحدة بنجاح");
        }
        else
        {
            var error = deleteResult.Error ?? "فشل في حذف الوحدة";
            ErrorMessage = error;
            await _dialogService.ShowErrorAsync("خطأ في حذف الوحدة", error);
        }
    }

    private void ViewPrices()
    {
        if (ProductId <= 0) return;
        if (SelectedUnit == null) return;

        var vm = new ProductPricesListViewModel(
            App.GetService<IProductPriceApiService>(),
            App.GetService<IProductUnitApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IScreenWindowService>(),
            App.GetService<IToastNotificationService>())
        {
            ProductId = ProductId,
            ProductUnitId = SelectedUnit.Id,
            ProductUnitName = SelectedUnit.UnitName ?? ""
        };

        _screenWindowService.OpenScreen(vm, new ScreenWindowOptions
        {
            Title = $"أسعار الوحدة: {SelectedUnit.UnitName}",
            Width = 800,
            Height = 600,
            OnClosed = (_) =>
            {
                _eventBus.Publish(new ProductChangedMessage(ProductId));
            }
        });
    }

    private void OnProductChanged(ProductChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () =>
        {
            if (ProductId > 0)
            {
                await LoadUnitsAsync();
            }
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<ProductChangedMessage>(OnProductChanged);
    }

    public void Dispose() => Cleanup();

    #endregion
}
