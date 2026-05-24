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
public class ProductListViewModel : ViewModelBase
{
    private readonly IProductApiService _productService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<ProductDto> _products = new();
    private ICollectionView? _productsView;
    private ProductDto? _selectedProduct;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;
    private string _lastUpdateTime = "ظ„ظ… ظٹطھظ… ط§ظ„طھط­ط¯ظٹط« ط¨ط¹ط¯";

    public ProductListViewModel()
    {
        _productService = App.GetService<IProductApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    /// <summary>    
    /// Constructor for dependency injection (used in tests)    
    /// </summary>
    public ProductListViewModel(
        IProductApiService productService,
        IEventBus eventBus,
        IDialogService dialogService,
        IToastNotificationService? toastService = null)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        // Initialize commands
        RefreshCommand = new AsyncRelayCommand(LoadProductsAsync);
        AddCommand = new RelayCommand(AddProduct);
        EditCommand = new RelayCommand(EditProduct, () => SelectedProduct != null);
        DeleteCommand = new AsyncRelayCommand(DeleteProductAsync, () => SelectedProduct != null && SelectedProduct.IsActive);
        RestoreCommand = new AsyncRelayCommand(RestoreProductAsync, () => SelectedProduct != null && !SelectedProduct.IsActive);
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
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                // Update command's CanExecute
                (EditCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (RestoreCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
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
                _ = LoadProductsAsync();
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
        IsBusy = true;
        ErrorMessage = null;

        try
        {
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
                ErrorMessage = HandleFailure(result.Error ?? "ظپط´ظ„ ظپظٹ طھط­ظ…ظٹظ„ ط§ظ„ظ…ظ†طھط¬ط§طھ", "ProductListViewModel.LoadProductsAsync", "[ProductListViewModel.LoadProductsAsync] Failed to load products from API.");
                IsEmpty = Products.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "ProductListViewModel.LoadProductsAsync", "[ProductListViewModel.LoadProductsAsync] Failed to load products from API.");
        }
        finally
        {
            IsBusy = false;
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
        var editorVm = new ProductEditorViewModel();
        if (_dialogService.ShowDialog(editorVm))
        {
            // Product was saved successfully - refresh list
            _ = LoadProductsAsync();
        }
    }

    private void EditProduct()
    {
        if (SelectedProduct == null) return;

        var editorVm = new ProductEditorViewModel(SelectedProduct);
        if (_dialogService.ShowDialog(editorVm))
        {
            _ = LoadProductsAsync();
        }
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

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"ط§ظ„ظ…ظ†طھط¬: {SelectedProduct.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (strategy == DeleteStrategy.Deactivate)
            {
                var deleteResult = await _productService.DeleteAsync(SelectedProduct.Id);
                if (deleteResult.IsSuccess)
                {
                    _eventBus.Publish(new ProductChangedMessage(SelectedProduct.Id));
                    await LoadProductsAsync();
                    _toastService.ShowSuccess("طھظ… ط¥ظ„ط؛ط§ط، طھظ†ط´ظٹط· ط§ظ„ظ…ظ†طھط¬ ط¨ظ†ط¬ط§ط­");
                }
                else
                {
                    var error = deleteResult.Error ?? "ظپط´ظ„ ظپظٹ ط­ط°ظپ ط§ظ„ظ…ظ†طھط¬";
                    ErrorMessage = error;
                    _toastService.ShowError(error);
                }
            }
            else if (strategy == DeleteStrategy.Permanent)
            {
                var deleteResult = await _productService.DeletePermanentlyAsync(SelectedProduct.Id);
                if (deleteResult.IsSuccess)
                {
                    _eventBus.Publish(new ProductChangedMessage(SelectedProduct.Id));
                    await LoadProductsAsync();
                    _toastService.ShowSuccess("طھظ… ط­ط°ظپ ط§ظ„ظ…ظ†طھط¬ ظ†ظ‡ط§ط¦ظٹط§ظ‹");
                }
                else
                {
                    var error = deleteResult.Error ?? "ظپط´ظ„ ظپظٹ ط­ط°ظپ ط§ظ„ظ…ظ†طھط¬";
                    ErrorMessage = error;
                    _toastService.ShowError(error);
                    LogSystemError($"Hard delete failed for Product {SelectedProduct.Id}: {error}", "ProductListViewModel.DeleteProductAsync");
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "ProductListViewModel.DeleteProductAsync", $"[ProductListViewModel.DeleteProductAsync] Failed to delete product with ID {SelectedProduct.Id}.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RestoreProductAsync()
    {
        if (SelectedProduct == null) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var request = new UpdateProductRequest(
                Barcode: SelectedProduct.Barcode ?? string.Empty,
                Name: SelectedProduct.Name,
                CategoryId: SelectedProduct.CategoryId,
                UnitId: SelectedProduct.UnitId,
                RetailUnitId: SelectedProduct.RetailUnitId,
                WholesaleUnitId: SelectedProduct.WholesaleUnitId,
                ConversionFactor: SelectedProduct.ConversionFactor,
                PurchasePrice: SelectedProduct.PurchasePrice,
                SalePrice: SelectedProduct.SalePrice,
                RetailPrice: SelectedProduct.RetailPrice,
                WholesalePrice: SelectedProduct.WholesalePrice,
                MinStock: SelectedProduct.MinStock,
                Description: SelectedProduct.Description,
                IsActive: true
            );

            var result = await _productService.UpdateAsync(SelectedProduct.Id, request);

            if (result.IsSuccess)
            {
                _eventBus.Publish(new ProductChangedMessage(SelectedProduct.Id));
                await LoadProductsAsync();
                await _dialogService.ShowSuccessAsync("ظ†ط¬ط§ط­", "طھظ… ط§ط³طھط¹ط§ط¯ط© ط§ظ„ظ…ظ†طھط¬ ط¨ظ†ط¬ط§ط­");
            }
            else
            {
                ErrorMessage = result.Error ?? "ظپط´ظ„ ظپظٹ ط§ط³طھط¹ط§ط¯ط© ط§ظ„ظ…ظ†طھط¬";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "ProductListViewModel.RestoreProductAsync", $"[ProductListViewModel.RestoreProductAsync] Failed to restore product with ID {SelectedProduct.Id}.");
        }
        finally
        {
            IsBusy = false;
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
    #endregion
}




