using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using System.Linq;
using System.Threading.Tasks;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

/// <summary>
/// Wrapper ViewModel for a product row in the selection dialog.
/// Adds live stock display (per-unit converted) without touching the immutable ProductDto record.
/// </summary>
public class ProductSelectionItemViewModel : INotifyPropertyChanged
{
    public ProductDto Product { get; }
    public decimal Stock { get; set; }

    /// <summary>Stock converted to the currently selected unit (set by parent VM).</summary>
    public decimal StockInSelectedUnit { get; set; }

    /// <summary>Display name of the selected unit (e.g., "كرتون", "حبة").</summary>
    public string? SelectedUnitName { get; set; }

    /// <summary>
    /// Fallback: shows base-unit stock (N3) when no unit selected.
    /// When a unit is selected, shows converted stock + unit name.
    /// </summary>
    public string StockDisplay
    {
        get
        {
            if (StockInSelectedUnit > 0 && !string.IsNullOrWhiteSpace(SelectedUnitName))
            {
                var val = StockInSelectedUnit < 1
                    ? StockInSelectedUnit.ToString("N1")
                    : $"{StockInSelectedUnit:N0}";
                return $"{val} {SelectedUnitName}";
            }
            return Stock > 0 ? Stock.ToString("N3") : (Product.CurrentStock > 0 ? Product.CurrentStock.ToString("N3") : "—");
        }
    }

    // Delegated properties for XAML column bindings
    public int Id => Product.Id;
    public string Name => Product.Name;
    public string? CategoryName => Product.CategoryName;
    public string? Barcode => Product.Barcode;
    public decimal Cost => 0m;

    public ProductSelectionItemViewModel(ProductDto product, decimal stock = 0)
    {
        Product = product;
        Stock = stock;
    }

    /// <summary>Recalculates StockInSelectedUnit from base stock and a product unit factor.</summary>
    public void ApplyUnit(decimal conversionFactor, string unitName)
    {
        StockInSelectedUnit = conversionFactor > 0 ? Stock / conversionFactor : Stock;
        SelectedUnitName = unitName;
        OnPropertyChanged(nameof(StockDisplay));
        OnPropertyChanged(nameof(StockInSelectedUnit));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
    private readonly IProductUnitApiService _productUnitService;
    private readonly int _warehouseId;

    private ObservableCollection<ProductSelectionItemViewModel> _products = new();
    private ICollectionView? _productsView;
    private ProductSelectionItemViewModel? _selectedItem;
    private string _searchText = string.Empty;
    private bool _isStockLoading;

    // ── Unit selection ──────────────────────────────────────────────────────────
    private ObservableCollection<ProductUnitDto> _availableUnits = new();
    private ProductUnitDto? _selectedUnit;
    private bool _hasUnits;
    private string _convertedStockDisplay = string.Empty;
    private string _selectedProductName = string.Empty;

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
        _productService      = App.GetService<IProductApiService>();
        _inventoryService    = App.GetService<IInventoryApiService>();
        _productUnitService  = App.GetService<IProductUnitApiService>();
        _warehouseId = warehouseId;

        AvailableUnits = new ObservableCollection<ProductUnitDto>();
        SelectedUnit = null;

        SelectCommand = new RelayCommand(Select);
        CancelCommand = new RelayCommand(Cancel);
        SearchCommand = new RelayCommand(Search);
        ScanBarcodeCommand = new RelayCommand(ScanBarcode);

        _ = LoadProductsAsync();
    }

    // ── Properties ────────────────────────────────────────────────────────────────

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
            {
                _ = OnSelectedItemChangedAsync();
            }
        }
    }

    /// <summary>Convenience property — returns the underlying ProductDto of the selected row.</summary>
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

    // ── Unit Selection Properties ──────────────────────────────────────────────

    public ObservableCollection<ProductUnitDto> AvailableUnits
    {
        get => _availableUnits;
        set => SetProperty(ref _availableUnits, value);
    }

    public ProductUnitDto? SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            if (SetProperty(ref _selectedUnit, value))
            {
                OnSelectedUnitChanged();
            }
        }
    }

    public bool HasUnits
    {
        get => _hasUnits;
        set => SetProperty(ref _hasUnits, value);
    }

    /// <summary>Converted stock display: "{N} {UnitName}" for the selected product + unit.</summary>
    public string ConvertedStockDisplay
    {
        get => _convertedStockDisplay;
        set => SetProperty(ref _convertedStockDisplay, value);
    }

    /// <summary>Name of the currently selected product (shown in details panel).</summary>
    public string SelectedProductName
    {
        get => _selectedProductName;
        set => SetProperty(ref _selectedProductName, value);
    }

    // ── Commands ───────────────────────────────────────────────────────────────

    public ICommand SelectCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ScanBarcodeCommand { get; }

    // ── Methods ────────────────────────────────────────────────────────────────

    private async Task LoadProductsAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _productService.GetAllAsync(false);
            if (result.IsSuccess && result.Value != null)
            {
                var items = result.Value
                    .OrderByDescending(p => p.Id)
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

            // Fetch stock in background — does NOT block the UI
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

            // Re-apply unit conversion for the selected item if a unit is active
            var selItem = SelectedItem;
            var selUnit = SelectedUnit;
            if (selItem != null && selUnit != null)
            {
                selItem.ApplyUnit(selUnit.ConversionFactor, selUnit.UnitName ?? "");
                var val = selItem.StockInSelectedUnit < 1
                    ? selItem.StockInSelectedUnit.ToString("N1")
                    : $"{selItem.StockInSelectedUnit:N0}";
                ConvertedStockDisplay = $"{val} {selUnit.UnitName}";
            }

            // Refresh the view to show updated stock values
            System.Windows.Application.Current.Dispatcher.Invoke(() => ProductsView?.Refresh());
        }
        catch
        {
            // Stock display is optional — swallow errors silently
        }
        finally
        {
            IsStockLoading = false;
        }
    }

    /// <summary>
    /// Called when the selected product row changes.
    /// Loads the product's available units for the unit selector panel.
    /// </summary>
    private async Task OnSelectedItemChangedAsync()
    {
        var item = SelectedItem;
        if (item == null)
        {
            HasUnits = false;
            SelectedProductName = string.Empty;
            ConvertedStockDisplay = string.Empty;
            return;
        }

        SelectedProductName = item.Name;
        HasUnits = false;
        AvailableUnits.Clear();
        SelectedUnit = null;

        if (_productUnitService == null) return;

        try
        {
            var result = await _productUnitService.GetByProductIdAsync(item.Id);
            if (result.IsSuccess && result.Value != null && result.Value.Count > 0)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableUnits = new ObservableCollection<ProductUnitDto>(result.Value);
                    HasUnits = true;

                    // Auto-select base unit (or first available)
                    SelectedUnit = result.Value.FirstOrDefault(u => u.IsBaseUnit)
                        ?? result.Value.First();
                });
            }
        }
        catch
        {
            // Unit loading is optional — swallow errors silently
        }
    }

    /// <summary>
    /// Called when the user picks a different unit from the dropdown.
    /// Converts the selected product's stock from base units to the chosen unit.
    /// </summary>
    private void OnSelectedUnitChanged()
    {
        var item = SelectedItem;
        var unit = SelectedUnit;

        if (item == null || unit == null)
        {
            ConvertedStockDisplay = string.Empty;
            return;
        }

        // Convert base-unit stock to selected unit
        var baseStock = item.Stock > 0 ? item.Stock : item.Product.CurrentStock;
        item.ApplyUnit(unit.ConversionFactor, unit.UnitName ?? "");

        // Build display string
        var val = item.StockInSelectedUnit < 1
            ? item.StockInSelectedUnit.ToString("N1")
            : $"{item.StockInSelectedUnit:N0}";
        ConvertedStockDisplay = $"{val} {unit.UnitName}";

        // Refresh DataGrid stock column
        ProductsView?.Refresh();
    }

    private bool ApplyFilter(object obj)
    {
        if (obj is not ProductSelectionItemViewModel item) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var search = SearchText.Trim().ToLower();
        return item.Name.ToLower().Contains(search)
            || (item.CategoryName?.ToLower().Contains(search) ?? false)
            || (item.Barcode?.ToLower().Contains(search) ?? false);
    }

    private void Search() => ProductsView?.Refresh();

    private void ScanBarcode()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        var search = SearchText.Trim().ToLower();
        var match = Products.FirstOrDefault(p =>
            p.Name.ToLower().Contains(search) ||
            (p.Barcode != null && p.Barcode.ToLower().Contains(search)));

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
        // RequestClose() intentionally omitted — allows continuous selection mode.
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




