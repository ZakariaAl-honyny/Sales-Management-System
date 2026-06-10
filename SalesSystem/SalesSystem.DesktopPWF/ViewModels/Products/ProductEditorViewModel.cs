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
    private readonly ICategoryApiService _categoryService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IProductPriceApiService? _priceService;
    private readonly IProductImageApiService? _imageService;
    private readonly IInventoryBatchApiService? _batchService;
    private readonly IScreenWindowService? _screenWindowService;
    private readonly IToastNotificationService? _toastService;

    private int _productId;
    private string _barcode = string.Empty;
    private string _name = string.Empty;
    private int? _categoryId;
    private decimal _minStock;
    private string _description = string.Empty;
    private bool _isActive = true;
    private bool _isEditMode;
    private string? _errorMessage;

    private CategoryDto? _selectedCategory;

    // Tab-specific fields
    private int _selectedTabIndex;
    private int _productUnitId;
    private ObservableCollection<ProductPriceDto> _prices = new();
    private ProductPriceDto? _selectedPrice;
    private ObservableCollection<ProductImageDto> _images = new();
    private ProductImageDto? _selectedImage;
    private ObservableCollection<InventoryBatchDto> _batches = new();
    private InventoryBatchDto? _selectedBatch;


    public ProductEditorViewModel()
    {
        _productService = App.GetService<IProductApiService>();
        _categoryService = App.GetService<ICategoryApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _priceService = App.GetService<IProductPriceApiService>();
        _imageService = App.GetService<IProductImageApiService>();
        _batchService = App.GetService<IInventoryBatchApiService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _ = LoadLookupDataAsync();
    }

    public ProductEditorViewModel(
        IProductApiService productService,
        ICategoryApiService categoryService,
        IEventBus eventBus,
        IDialogService dialogService,
        IProductPriceApiService? priceService = null,
        IProductImageApiService? imageService = null,
        IInventoryBatchApiService? batchService = null,
        IScreenWindowService? screenWindowService = null,
        IToastNotificationService? toastService = null)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _priceService = priceService ?? App.GetService<IProductPriceApiService>();
        _imageService = imageService ?? App.GetService<IProductImageApiService>();
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
            App.GetService<ICategoryApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<IDialogService>(),
            priceService: App.GetService<IProductPriceApiService>(),
            imageService: App.GetService<IProductImageApiService>(),
            batchService: App.GetService<IInventoryBatchApiService>(),
            screenWindowService: App.GetService<IScreenWindowService>(),
            toastService: App.GetService<IToastNotificationService>())
    {
        _productId = product.Id;
        _barcode = product.Barcode ?? string.Empty;
        _name = product.Name;
        _categoryId = product.CategoryId;
        _minStock = product.MinStock;
        _description = product.Description ?? string.Empty;
        _isActive = product.IsActive;
        _isEditMode = true;
    }

    public ProductEditorViewModel(
        ProductDto product,
        IProductApiService productService,
        ICategoryApiService categoryService,
        IEventBus eventBus,
        IDialogService dialogService,
        IProductPriceApiService? priceService = null,
        IProductImageApiService? imageService = null,
        IInventoryBatchApiService? batchService = null,
        IScreenWindowService? screenWindowService = null,
        IToastNotificationService? toastService = null)
        : this(productService, categoryService, eventBus, dialogService,
              priceService, imageService, batchService, screenWindowService, toastService)
    {
        _productId = product.Id;
        _barcode = product.Barcode ?? string.Empty;
        _name = product.Name;
        _categoryId = product.CategoryId;
        _minStock = product.MinStock;
        _description = product.Description ?? string.Empty;
        _isActive = product.IsActive;
        _isEditMode = true;
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

    public decimal MinStock
    {
        get => _minStock;
        set => SetProperty(ref _minStock, value);
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

    public ObservableCollection<CategoryDto> Categories { get; } = new();

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

    // Images
    public ObservableCollection<ProductImageDto> Images
    {
        get => _images;
        set => SetProperty(ref _images, value);
    }

    public ProductImageDto? SelectedImage
    {
        get => _selectedImage;
        set => SetProperty(ref _selectedImage, value);
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
    public ICommand AddImageCommand { get; private set; } = null!;
    public ICommand SetPrimaryImageCommand { get; private set; } = null!;
    public ICommand DeleteImageCommand { get; private set; } = null!;
    public ICommand RefreshImagesCommand { get; private set; } = null!;
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

        // Images tab commands
        AddImageCommand = new AsyncRelayCommand(AddImageAsync);
        SetPrimaryImageCommand = new AsyncRelayCommand(SetPrimaryImageAsync);
        DeleteImageCommand = new AsyncRelayCommand(DeleteImageAsync);
        RefreshImagesCommand = new AsyncRelayCommand(LoadImagesAsync);

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
                CategoryId: CategoryId,
                MinStock: MinStock,
                Description: string.IsNullOrWhiteSpace(Description) ? null : Description,
                IsActive: IsActive);

            result = await _productService.UpdateAsync(_productId, updateRequest);
        }
        else
        {
            var createRequest = new CreateProductRequest(
                Name: Name,
                Barcode: string.IsNullOrWhiteSpace(Barcode) ? null : Barcode,
                CategoryId: CategoryId,
                MinStock: MinStock,
                Description: string.IsNullOrWhiteSpace(Description) ? null : Description);

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
                await LoadImagesAsync();
                break;
            case 3:
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
    /// Loads images for the current product (available for existing products only).
    /// Triggered when the Images tab is selected.
    /// </summary>
    public async Task LoadImagesAsync()
    {
        if (!IsExistingProduct || ProductId <= 0) return;
        await ExecuteAsync(LoadImagesOperationAsync);
    }

    private async Task LoadImagesOperationAsync()
    {
        ErrorMessage = null;
        var result = await _imageService!.GetByProductAsync(ProductId);
        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Images.Clear();
                foreach (var item in result.Value.OrderBy(x => x.SortOrder))
                {
                    Images.Add(item);
                }
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الصور", "ProductEditorViewModel.LoadImagesOperationAsync", "[ProductEditorViewModel.LoadImagesOperationAsync] Failed to load product images from API.");
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

    // ── Images Tab Commands ──

    private async Task AddImageAsync()
    {
        if (_imageService == null || _toastService == null) return;
        await ExecuteAsync(AddImageOperationAsync);
    }

    private async Task AddImageOperationAsync()
    {
        if (!IsExistingProduct || ProductId <= 0) return;
        ErrorMessage = null;

        string? filePath = null;
        InvokeOnUIThread(() =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "اختيار صورة للمنتج",
                Filter = "صور (PNG, JPG, JPEG, GIF, BMP)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|كل الملفات|*.*",
                Multiselect = false,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
                filePath = dialog.FileName;
        });

        if (string.IsNullOrEmpty(filePath))
            return;

        var request = new CreateProductImageRequest(
            ProductId: ProductId,
            ImagePath: filePath,
            IsPrimary: false,
            SortOrder: 0);

        var result = await _imageService!.CreateAsync(request);

        if (result.IsSuccess)
        {
            _toastService!.ShowSuccess("تمت إضافة الصورة بنجاح");
            await LoadImagesAsync();
        }
        else
        {
            var error = result.Error ?? "فشل في إضافة الصورة";
            ErrorMessage = HandleFailure(error, "ProductEditorViewModel.AddImageOperationAsync", "[ProductEditorViewModel.AddImageOperationAsync] Failed to create product image.");
            await _dialogService.ShowErrorAsync("خطأ في إضافة الصورة", ErrorMessage);
        }
    }

    private async Task SetPrimaryImageAsync()
    {
        if (_imageService == null || _toastService == null || SelectedImage == null) return;

        var imageId = SelectedImage.Id;
        await ExecuteAsync(() => SetPrimaryImageOperationAsync(imageId));
    }

    private async Task SetPrimaryImageOperationAsync(int imageId)
    {
        ErrorMessage = null;
        var result = await _imageService!.SetPrimaryAsync(ProductId, imageId);

        if (result.IsSuccess)
        {
            _toastService!.ShowSuccess("تم تعيين الصورة كصورة رئيسية");
            await LoadImagesAsync();
        }
        else
        {
            var error = result.Error ?? "فشل في تعيين الصورة الرئيسية";
            ErrorMessage = HandleFailure(error, "ProductEditorViewModel.SetPrimaryImageOperationAsync", "[ProductEditorViewModel.SetPrimaryImageOperationAsync] Failed to set primary image.");
            await _dialogService.ShowErrorAsync("خطأ في تعيين الصورة", ErrorMessage);
        }
    }

    private async Task DeleteImageAsync()
    {
        if (_imageService == null || _toastService == null || SelectedImage == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync("حذف الصورة");
        if (strategy == DeleteStrategy.Cancel) return;

        var imageId = SelectedImage.Id;
        await ExecuteAsync(() => DeleteImageOperationAsync(imageId));
    }

    private async Task DeleteImageOperationAsync(int imageId)
    {
        ErrorMessage = null;
        var result = await _imageService!.DeactivateAsync(imageId);

        if (result.IsSuccess)
        {
            _toastService!.ShowSuccess("تم حذف الصورة بنجاح");
            await LoadImagesAsync();
        }
        else
        {
            var error = result.Error ?? "فشل في حذف الصورة";
            ErrorMessage = HandleFailure(error, "ProductEditorViewModel.DeleteImageOperationAsync", "[ProductEditorViewModel.DeleteImageOperationAsync] Failed to delete product image.");
            await _dialogService.ShowErrorAsync("خطأ في حذف الصورة", ErrorMessage);
        }
    }

    #endregion
}
