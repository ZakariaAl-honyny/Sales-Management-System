using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

/// <summary>
/// ViewModel for Products List View
/// </summary>
public class ProductListViewModel : ViewModelBase, IDisposable
{
    private readonly IProductApiService _productService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<ProductDto> _products = new();
    private ICollectionView? _productsView;
    private ProductDto? _selectedProduct;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;
    private string _lastUpdateTime = "لم يتم التحديث بعد";

    public ProductListViewModel()
    {
        _productService = App.GetService<IProductApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();

        InitializeCommands();
    }

    /// <summary>    
    /// Constructor for dependency injection (used in tests)    
    /// </summary>
    public ProductListViewModel(
        IProductApiService productService,
        IEventBus eventBus,
        IDialogService dialogService,
        IScreenWindowService screenWindowService,
        IToastNotificationService? toastService = null)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        // Initialize commands
        RefreshCommand = new AsyncRelayCommand(LoadProductsAsync);
        AddCommand = new RelayCommand(AddProduct);
        EditCommand = new RelayCommand(EditProduct);
        DeleteCommand = new AsyncRelayCommand(DeleteProductAsync);
        RestoreCommand = new AsyncRelayCommand(RestoreProductAsync);
        SearchCommand = new RelayCommand(Search);

        // Subscribe to product changes
        _eventBus.Subscribe<ProductChangedMessage>(OnProductChanged);
    }

    #region Properties
    public ObservableCollection<ProductDto> Products
    {
        get => _products;
        set => SetProperty(ref _products, value);
    }

    public ICollectionView? ProductsView
    {
        get => _productsView;
        private set => SetProperty(ref _productsView, value);
    }

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ProductsView?.Refresh();
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
                _ = ExecuteAsync(LoadProductsOperationAsync);
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
    public async Task LoadProductsAsync()
    {
        await ExecuteAsync(LoadProductsOperationAsync);
    }

    private async Task LoadProductsOperationAsync()
    {
        ErrorMessage = null;

        var result = await _productService.GetAllAsync(IncludeInactive);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Products.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Products.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Products.Count == 0;
                LastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل المنتجات", "ProductListViewModel.LoadProductsAsync", "[ProductListViewModel.LoadProductsAsync] Failed to load products from API.");
            IsEmpty = Products.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        ProductsView = new ListCollectionView(Products);
        ProductsView.Filter = FilterProducts;
    }

    private bool FilterProducts(object obj)
    {
        if (obj is not ProductDto product) return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var searchLower = SearchText.Trim().ToLower();
        return product.Name.ToLower().Contains(searchLower) ||
               (product.CategoryName?.ToLower().Contains(searchLower) ?? false) ||
               (product.Barcode?.ToLower().Contains(searchLower) ?? false);
    }

    private void AddProduct()
    {
        var editorVm = App.GetService<ProductEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "منتج جديد",
            Width = 900,
            Height = 650,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadProductsAsync());
            }
        });
    }

    private void EditProduct()
    {
        if (SelectedProduct == null) return;

        var editorVm = new ProductEditorViewModel(SelectedProduct);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل منتج",
            Width = 900,
            Height = 650,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadProductsAsync());
            }
        });
    }

    public void EditProductFromDoubleClick()
    {
        if (SelectedProduct != null)
        {
            EditProduct();
        }
    }

public async Task DeleteProductAsync()
    {
        if (SelectedProduct == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"المنتج: {SelectedProduct.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        await ExecuteAsync(() => DeleteProductOperationAsync(strategy));
    }

    private async Task DeleteProductOperationAsync(DeleteStrategy strategy)
    {
        ErrorMessage = null;

        if (strategy == DeleteStrategy.Deactivate)
        {
            var deleteResult = await _productService.DeleteAsync(SelectedProduct!.Id);
            if (deleteResult.IsSuccess)
            {
                _eventBus.Publish(new ProductChangedMessage(SelectedProduct.Id));
                await LoadProductsAsync();
                _toastService.ShowSuccess("تم إلغاء تنشيط المنتج بنجاح");
            }
            else
            {
                ErrorMessage = HandleFailure(deleteResult.Error ?? "فشل في حذف المنتج", "ProductListViewModel.DeleteProductAsync", $"[ProductListViewModel.DeleteProductAsync] Failed to deactivate product with ID {SelectedProduct.Id}.");
            }
        }
        else if (strategy == DeleteStrategy.Permanent)
        {
            var deleteResult = await _productService.DeletePermanentlyAsync(SelectedProduct!.Id);
            if (deleteResult.IsSuccess)
            {
                _eventBus.Publish(new ProductChangedMessage(SelectedProduct.Id));
                await LoadProductsAsync();
                _toastService.ShowSuccess("تم حذف المنتج نهائياً");
            }
            else
            {
                var error = deleteResult.Error ?? "فشل في حذف المنتج";
                ErrorMessage = HandleFailure(error, "ProductListViewModel.DeleteProductAsync", $"[ProductListViewModel.DeleteProductAsync] Failed to permanently delete product with ID {SelectedProduct.Id}.");
                LogSystemError($"Hard delete failed for Product {SelectedProduct.Id}: {error}", "ProductListViewModel.DeleteProductAsync");
            }
        }
    }

    public async Task RestoreProductAsync()
    {
        if (SelectedProduct == null) return;

        await ExecuteAsync(RestoreProductOperationAsync);
    }

    private async Task RestoreProductOperationAsync()
    {
        ErrorMessage = null;

        var request = new UpdateProductRequest(
            Name: SelectedProduct!.Name,
            Barcode: SelectedProduct.Barcode,
            CategoryId: SelectedProduct.CategoryId,
            Description: SelectedProduct.Description,
            ReorderLevel: SelectedProduct.ReorderLevel,
            TrackExpiry: SelectedProduct.TrackExpiry,
            IsActive: true
        );

        var result = await _productService.UpdateAsync(SelectedProduct.Id, request);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new ProductChangedMessage(SelectedProduct.Id));
            await LoadProductsAsync();
            _toastService.ShowSuccess("تم استعادة المنتج بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في استعادة المنتج", "ProductListViewModel.RestoreProductAsync", $"[ProductListViewModel.RestoreProductAsync] Failed to restore product with ID {SelectedProduct.Id}.");
        }
}

    private void Search()
    {
        ProductsView?.Refresh();
    }

    private void OnProductChanged(ProductChangedMessage msg)
    {
        // Reload products when any change happens (from other modules or this module)
        _ = InvokeOnUIThreadAsync(async () =>
        {
            await LoadProductsAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<ProductChangedMessage>(OnProductChanged);
    }

    public void Dispose() => Cleanup();
    #endregion
}




