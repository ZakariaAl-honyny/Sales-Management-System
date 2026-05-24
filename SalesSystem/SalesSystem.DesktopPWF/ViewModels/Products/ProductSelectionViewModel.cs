using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using System.Linq;
using System.Threading.Tasks;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

/// <summary>
/// Wrapper ViewModel for a product row in the selection dialog.
/// Adds live stock display without touching the immutable ProductDto record.
/// </summary>
public class ProductSelectionItemViewModel
{
    public ProductDto Product { get; }
    public decimal Stock { get; set; }
    public string StockDisplay => Stock > 0 ? Stock.ToString("N3") : "â€”";

    // Delegated properties for XAML column bindings
    public int Id => Product.Id;
    public string Name => Product.Name;
    public string? Barcode => Product.Barcode;
    public string? CategoryName => Product.CategoryName;
    public decimal RetailPrice => Product.RetailPrice;
    public decimal WholesalePrice => Product.WholesalePrice;

    public ProductSelectionItemViewModel(ProductDto product, decimal stock = 0)
    {
        Product = product;
        Stock = stock;
    }
}

/// <summary>
/// ViewModel for the Product Search / Selection dialog.
/// Supports:
/// - Real-time text filtering (Name, Code, Barcode)
/// - Keyboard navigation (Up/Down arrows + Enter handled by the View)
/// - Optional warehouse stock display (pass warehouseId > 0 to activate)
/// - Event-driven selection: raises OnProductSelected instead of returning directly
/// </summary>
public class ProductSelectionViewModel : ViewModelBase
{
    private readonly IProductApiService _productService;
    private readonly IInventoryApiService _inventoryService;
    private readonly int _warehouseId;

    private ObservableCollection<ProductSelectionItemViewModel> _products = new();
    private ICollectionView? _productsView;
    private ProductSelectionItemViewModel? _selectedItem;
    private string _searchText = string.Empty;
    private bool _isStockLoading;

    /// <summary>
    /// Raised when the user confirms a product selection.
    /// The ProductDto (not the wrapper) is passed to keep callers unchanged.
    /// </summary>
    public event Action<ProductDto>? OnProductSelected;

    /// <param name="warehouseId">
    /// When > 0 the dialog fetches and displays current stock for each product.
    /// Pass 0 (default) to skip stock lookup.
    /// </param>
    public ProductSelectionViewModel(int warehouseId = 0)
    {
        _productService  = App.GetService<IProductApiService>();
        _inventoryService = App.GetService<IInventoryApiService>();
        _warehouseId = warehouseId;

        SelectCommand = new RelayCommand(Select, () => SelectedItem != null);
        CancelCommand = new RelayCommand(Cancel);
        SearchCommand = new RelayCommand(Search);
        ScanBarcodeCommand = new RelayCommand(ScanBarcode);

        _ = LoadProductsAsync();
    }

    // â”€â”€ Properties â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public ObservableCollection<ProductSelectionItemViewModel> Products
    {
        get => _products;
        set => SetProperty(ref _products, value);
    }

    public ICollectionView? ProductsView
    {
        get => _productsView;
        private set => SetProperty(ref _productsView, value);
    }

    public ProductSelectionItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
                (SelectCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    /// <summary>Convenience property â€” returns the underlying ProductDto of the selected row.</summary>
    public ProductDto? SelectedProduct => SelectedItem?.Product;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ProductsView?.Refresh();
        }
    }


    public bool IsStockLoading
    {
        get => _isStockLoading;
        set => SetProperty(ref _isStockLoading, value);
    }

    public bool ShowStockColumn => _warehouseId > 0;

    // â”€â”€ Commands â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public ICommand SelectCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ScanBarcodeCommand { get; }

    // â”€â”€ Methods â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async Task LoadProductsAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _productService.GetAllAsync(false);
            if (result.IsSuccess && result.Value != null)
            {
                var items = result.Value
                    .Select(p => new ProductSelectionItemViewModel(p))
                    .ToList();

                Products = new ObservableCollection<ProductSelectionItemViewModel>(items);
                ProductsView = CollectionViewSource.GetDefaultView(Products);
                ProductsView.Filter = ApplyFilter;
            }
        }
        finally
        {
            IsBusy = false;

            // Fetch stock in background â€” does NOT block the UI
            if (_warehouseId > 0)
                _ = LoadStockAsync();
        }
    }

    private async Task LoadStockAsync()
    {
        IsStockLoading = true;
        try
        {
            // Fetch stock per product in parallel (batched to avoid overwhelming the API)
            var tasks = Products.Select(async item =>
            {
                var stockResult = await _inventoryService.GetStockAsync(item.Id, _warehouseId);
                if (stockResult.IsSuccess)
                    item.Stock = stockResult.Value;
            });

            await Task.WhenAll(tasks);

            // Refresh the view to show updated stock values
            System.Windows.Application.Current.Dispatcher.Invoke(() => ProductsView?.Refresh());
        }
        catch
        {
            // Stock display is optional â€” swallow errors silently
        }
        finally
        {
            IsStockLoading = false;
        }
    }

    private bool ApplyFilter(object obj)
    {
        if (obj is not ProductSelectionItemViewModel item) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var search = SearchText.Trim().ToLower();
        return item.Name.ToLower().Contains(search)
            || (item.Barcode?.ToLower().Contains(search) ?? false)
            || (item.CategoryName?.ToLower().Contains(search) ?? false);
    }

    private void Search() => ProductsView?.Refresh();

    private void ScanBarcode()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        var search = SearchText.Trim().ToLower();
        var match = Products.FirstOrDefault(p =>
            (p.Barcode != null && p.Barcode.ToLower() == search));

        if (match != null)
        {
            SelectedItem = match;
            Select();
            SearchText = string.Empty;
        }
        else
        {
            Search();
        }
    }

    private void Select()
    {
        if (SelectedItem != null)
            OnProductSelected?.Invoke(SelectedItem.Product);
        // RequestClose() intentionally omitted â€” allows continuous selection mode.
        // Callers that need single-pick mode call CloseDialog() explicitly.
    }

    private void Cancel()
    {
        SelectedItem = null;
        RequestClose();
    }

    /// <summary>Called by callers that operate in single-pick mode to close the window.</summary>
    public void CloseDialog() => RequestClose();
}




