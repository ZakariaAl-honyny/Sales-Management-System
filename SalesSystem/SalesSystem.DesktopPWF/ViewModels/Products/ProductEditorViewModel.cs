using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

/// <summary>
/// ViewModel for Product Editor Dialog — supports tabbed UI with Basic Info, Pricing, Images, and Batches tabs.
/// Phase 25: Removed legacy unit/price fields — units managed via ProductUnits, pricing via ProductPrices.
/// </summary>
public class ProductEditorViewModel : ViewModelBase
{
    private readonly IProductApiService _productService;
    private readonly IProductCategoryApiService _categoryService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IProductPriceApiService? _priceService;
    private readonly IInventoryBatchApiService? _batchService;
    private readonly IScreenWindowService? _screenWindowService;
    private readonly IToastNotificationService? _toastService;

    private int _productId;
    private string _barcode = string.Empty;
    private string _name = string.Empty;
    private int? _categoryId;
    private decimal _reorderLevel;
    private string _description = string.Empty;
    private bool _isActive = true;
    private bool _isEditMode;
    private bool _trackExpiry;
    private decimal? _openingQuantity;
    private decimal? _openingUnitCost;
    private DateTime? _openingExpiryDate;
    private string? _errorMessage;

    private ProductCategoryDto? _selectedCategory;

    // Tab-specific fields
    private int _selectedTabIndex;
    private int _productUnitId;
    private ObservableCollection<ProductPriceDto> _prices = new();
    private ProductPriceDto? _selectedPrice;
    private ObservableCollection<InventoryBatchDto> _batches = new();
    private InventoryBatchDto? _selectedBatch;


    public ProductEditorViewModel()
    {
        _productService = App.GetService<IProductApiService>();
        _categoryService = App.GetService<IProductCategoryApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _priceService = App.GetService<IProductPriceApiService>();
        _batchService = App.GetService<IInventoryBatchApiService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _ = LoadLookupDataAsync();
    }

    public ProductEditorViewModel(
        IProductApiService productService,
        IProductCategoryApiService categoryService,
        IEventBus eventBus,
        IDialogService dialogService,
        IProductPriceApiService? priceService = null,
        IInventoryBatchApiService? batchService = null,
        IScreenWindowService? screenWindowService = null,
        IToastNotificationService? toastService = null)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _priceService = priceService ?? App.GetService<IProductPriceApiService>();
        _batchService = batchService ?? App.GetService<IInventoryBatchApiService>();
        _screenWindowService = screenWindowService ?? App.GetService<IScreenWindowService>();
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _ = LoadLookupDataAsync();
    }

    public ProductEditorViewModel(ProductDto product)
        : this(
            App.GetService<IProductApiService>(),
            App.GetService<IProductCategoryApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<IDialogService>(),
            priceService: App.GetService<IProductPriceApiService>(),
            batchService: App.GetService<IInventoryBatchApiService>(),
            screenWindowService: App.GetService<IScreenWindowService>(),
            toastService: App.GetService<IToastNotificationService>())
    {
        _productId = product.Id;
        _barcode = product.Barcode ?? string.Empty;
        _name = product.Name;
        _categoryId = product.CategoryId;
        _reorderLevel = product.ReorderLevel;
        _description = product.Description ?? string.Empty;
        _isActive = product.IsActive;
        _isEditMode = true;
        _trackExpiry = product.TrackExpiry;
    }

    public ProductEditorViewModel(
        ProductDto product,
        IProductApiService productService,
        IProductCategoryApiService categoryService,
        IEventBus eventBus,
        IDialogService dialogService,
        IProductPriceApiService? priceService = null,
        IInventoryBatchApiService? batchService = null,
        IScreenWindowService? screenWindowService = null,
        IToastNotificationService? toastService = null)
        : this(productService, categoryService, eventBus, dialogService,
              priceService, batchService, screenWindowService, toastService)
    {
        _productId = product.Id;
        _barcode = product.Barcode ?? string.Empty;
        _name = product.Name;
        _categoryId = product.CategoryId;
        _reorderLevel = product.ReorderLevel;
        _description = product.Description ?? string.Empty;
        _isActive = product.IsActive;
        _isEditMode = true;
        _trackExpiry = product.TrackExpiry;
    }

    #region Properties
    public string Title => IsEditMode ? "تعديل منتج" : "إضافة منتج جديد";

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public string Barcode
    {
        get => _barcode;
        set => SetProperty(ref _barcode, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Name), "اسم المنتج مطلوب");
                else
                    ClearErrors(nameof(Name));
            }
        }
    }

    public int? CategoryId
    {
        get => _categoryId;
        set
        {
            if (SetProperty(ref _categoryId, value))
            {
                if (!value.HasValue || value.Value <= 0)
                    AddError(nameof(CategoryId), "يجب اختيار فئة");
                else
                    ClearErrors(nameof(CategoryId));
            }
        }
    }

    public decimal ReorderLevel
    {
        get => _reorderLevel;
        set => SetProperty(ref _reorderLevel, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
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

    public ObservableCollection<ProductCategoryDto> Categories { get; } = new();

    public ProductCategoryDto? SelectedCategory
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

    // ── Opening Stock Fields ──

    /// <summary>
    /// True when the product tracks expiry dates — shown as a checkbox in the editor.
    /// </summary>
    public bool TrackExpiry
    {
        get => _trackExpiry;
        set => SetProperty(ref _trackExpiry, value);
    }

    /// <summary>
    /// Optional opening quantity when creating a new product.
    /// </summary>
    public decimal? OpeningQuantity
    {
        get => _openingQuantity;
        set => SetProperty(ref _openingQuantity, value);
    }

    /// <summary>
    /// Optional unit cost for the opening stock.
    /// </summary>
    public decimal? OpeningUnitCost
    {
        get => _openingUnitCost;
        set => SetProperty(ref _openingUnitCost, value);
    }

    /// <summary>
    /// Optional expiry date for the opening stock batch.
    /// Required if TrackExpiry is true and OpeningQuantity > 0.
    /// </summary>
    public DateTime? OpeningExpiryDate
    {
        get => _openingExpiryDate;
        set => SetProperty(ref _openingExpiryDate, value);
    }

    /// <summary>
    /// True when opening stock fields should be visible (create mode only).
    /// </summary>
    public bool ShowOpeningFields => !IsEditMode;

    // ── Tab-specific Properties ──

    /// <summary>
    /// Product ID for API calls — 0 for new products.
    /// </summary>
    public int ProductId => _productId;

    /// <summary>
    /// True when editing an existing product (needed for tab enablement).
    /// </summary>
    public bool IsExistingProduct => IsEditMode && _productId > 0;

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                _ = OnTabChangedAsync(value);
            }
        }
    }

    public int ProductUnitId
    {
        get => _productUnitId;
        set => SetProperty(ref _productUnitId, value);
    }

    // Pricing
    public ObservableCollection<ProductPriceDto> Prices
    {
        get => _prices;
        set => SetProperty(ref _prices, value);
    }

    public ProductPriceDto? SelectedPrice
    {
        get => _selectedPrice;
        set => SetProperty(ref _selectedPrice, value);
    }

    // Batches
    public ObservableCollection<InventoryBatchDto> Batches
    {
        get => _batches;
        set => SetProperty(ref _batches, value);
    }

    public InventoryBatchDto? SelectedBatch
    {
        get => _selectedBatch;
        set => SetProperty(ref _selectedBatch, value);
    }

    #endregion

    #region Commands
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    // Tab commands
    public ICommand AddPriceCommand { get; private set; } = null!;
    public ICommand EditPriceCommand { get; private set; } = null!;
    public ICommand RefreshPricesCommand { get; private set; } = null!;
    public ICommand RefreshBatchesCommand { get; private set; } = null!;
    #endregion

    #region Methods

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ المنتج...")));
        CancelCommand = new RelayCommand(Cancel);

        // Pricing tab commands
        AddPriceCommand = new RelayCommand(AddPrice);
        EditPriceCommand = new RelayCommand(EditPrice);
        RefreshPricesCommand = new AsyncRelayCommand(LoadPricesAsync);

        // Batches tab commands
        RefreshBatchesCommand = new AsyncRelayCommand(LoadBatchesAsync);
    }

    private async Task LoadLookupDataAsync()
    {
        try
        {
            var categoriesResult = await _categoryService.GetAllAsync();
            if (categoriesResult.IsSuccess && categoriesResult.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories.Clear();
                    foreach (var cat in categoriesResult.Value)
                    {
                        Categories.Add(cat);
                    }

                    if (CategoryId.HasValue)
                    {
                        SelectedCategory = Categories.FirstOrDefault(c => c.Id == CategoryId.Value);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "ProductEditorViewModel.LoadLookupDataAsync", "[ProductEditorViewModel.LoadLookupDataAsync] Failed to load categories for lookup.");
        }
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        // Use INotifyDataErrorInfo to add errors
        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "اسم المنتج مطلوب");
        if (!CategoryId.HasValue || CategoryId.Value <= 0)
            AddError(nameof(CategoryId), "يجب اختيار فئة");

        // Opening stock validation (create mode only)
        if (ShowOpeningFields)
        {
            if (OpeningQuantity.HasValue && OpeningQuantity.Value <= 0)
                AddError(nameof(OpeningQuantity), "الكمية الافتتاحية يجب أن تكون أكبر من صفر");
            if (OpeningUnitCost.HasValue && OpeningUnitCost.Value < 0)
                AddError(nameof(OpeningUnitCost), "تكلفة الوحدة لا يمكن أن تكون سالبة");
            if (TrackExpiry && OpeningQuantity.HasValue && OpeningQuantity.Value > 0 && !OpeningExpiryDate.HasValue)
                AddError(nameof(OpeningExpiryDate), "تاريخ الانتهاء مطلوب عند تفعيل تتبع الصلاحية وإدخال كمية افتتاحية");
        }

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync())
        {
            return;
        }

        ErrorMessage = null;

        Result<ProductDto> result;

        if (IsEditMode)
        {
            var updateRequest = new UpdateProductRequest(
                Name: Name,
                Barcode: string.IsNullOrWhiteSpace(Barcode) ? null : Barcode,
                CategoryId: CategoryId ?? 0,
                Description: string.IsNullOrWhiteSpace(Description) ? null : Description,
                ReorderLevel: ReorderLevel,
                IsActive: IsActive);

            result = await _productService.UpdateAsync(_productId, updateRequest);
        }
        else
        {
            var createRequest = new CreateProductRequest(
                Name: Name,
                Barcode: string.IsNullOrWhiteSpace(Barcode) ? null : Barcode,
                CategoryId: CategoryId ?? 0,
                Description: string.IsNullOrWhiteSpace(Description) ? null : Description,
                ReorderLevel: ReorderLevel,
                TrackExpiry: TrackExpiry,
                OpeningQuantity: OpeningQuantity,
                OpeningUnitCost: OpeningUnitCost,
                OpeningExpiryDate: OpeningExpiryDate);

            result = await _productService.CreateAsync(createRequest);
        }

        if (result.IsSuccess && result.Value != null)
        {
            // Publish event to notify other modules
            _eventBus.Publish(new ProductChangedMessage(result.Value.Id));

            await _dialogService.ShowSuccessAsync(
                IsEditMode ? "تحديث المنتج" : "إضافة منتج",
                IsEditMode ? "تم تحديث المنتج بنجاح" : "تم إضافة المنتج بنجاح");

            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ المنتج", "ProductEditorViewModel.SaveAsync", "[ProductEditorViewModel.SaveAsync] Failed to save product data.");
            await _dialogService.ShowErrorAsync("خطأ في حفظ المنتج", ErrorMessage);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    // ── Tab Lazy-Load Methods ──

    private async Task OnTabChangedAsync(int tabIndex)
    {
        switch (tabIndex)
        {
            case 1:
                await LoadPricesAsync();
                break;
            case 2:
                await LoadBatchesAsync();
                break;
        }
    }

    /// <summary>
    /// Loads prices for the current product (available for existing products only).
    /// Triggered when the Pricing tab is selected.
    /// </summary>
    public async Task LoadPricesAsync()
    {
        if (!IsExistingProduct || ProductId <= 0) return;
        await ExecuteAsync(LoadPricesOperationAsync);
    }

    private async Task LoadPricesOperationAsync()
    {
        ErrorMessage = null;
        var result = await _priceService!.GetByProductUnitAsync(ProductUnitId > 0 ? ProductUnitId : ProductId);
        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Prices.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Prices.Add(item);
                }
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الأسعار", "ProductEditorViewModel.LoadPricesOperationAsync", "[ProductEditorViewModel.LoadPricesOperationAsync] Failed to load product prices from API.");
        }
    }

    /// <summary>
    /// Loads inventory batches for the current product (available for existing products only).
    /// Triggered when the Batches tab is selected.
    /// </summary>
    public async Task LoadBatchesAsync()
    {
        if (!IsExistingProduct || ProductId <= 0) return;
        await ExecuteAsync(LoadBatchesOperationAsync);
    }

    private async Task LoadBatchesOperationAsync()
    {
        ErrorMessage = null;
        var result = await _batchService!.GetByProductAsync(ProductId, null);
        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Batches.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Batches.Add(item);
                }
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الدفعات", "ProductEditorViewModel.LoadBatchesOperationAsync", "[ProductEditorViewModel.LoadBatchesOperationAsync] Failed to load inventory batches from API.");
        }
    }

    // ── Pricing Tab Commands ──

    private void AddPrice()
    {
        if (_screenWindowService == null) return;

        var editorVm = new ProductPriceEditorViewModel(ProductUnitId > 0 ? ProductUnitId : ProductId);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "إضافة سعر جديد",
            Width = 550,
            Height = 500,
            OnClosed = (vm) =>
            {
                if (vm is ProductPriceEditorViewModel editor && editor.PriceId.HasValue)
                {
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadPricesAsync());
                }
            }
        });
    }

    private void EditPrice()
    {
        if (_screenWindowService == null || SelectedPrice == null) return;

        var editorVm = new ProductPriceEditorViewModel(ProductUnitId > 0 ? ProductUnitId : ProductId, SelectedPrice);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل السعر",
            Width = 550,
            Height = 500,
            OnClosed = (vm) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadPricesAsync());
            }
        });
    }

    #endregion
}
